using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

using GetBalanceResp = ThirdPartySeamlessWallet.Service.Model.MaxGame.GetBalanceResp;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class MaxGameBLL
    {
        private readonly Tools _tools;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private Dictionary<int, TpBetLimit> _tpBetLimitData;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;
        private const string _testCurrency = "UUS";

        public MaxGameBLL(IServiceProvider provider)
        {
            _thisTp = Tp.MaxGame;
            _gameType = (int)GameType.MaxGame_Poker;
            _tools = provider.GetRequiredService<Tools>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpBetLimitData = provider.GetRequiredService<IEnumerable<TpBetLimit>>().Where(i => i.GameType == _gameType).ToDictionary(item => item.CurrencyId, item => item);
            _log2File = provider.GetRequiredService<Log2File>();
        }


        #region Api Verify & Process

        public dynamic GetPlayerInfo(string request)
        {
            var getReq = JsonConvert.DeserializeObject<GetPlayerInfoReq_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.WalletSideToken))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideTokenInvalid);
            }

            if (!_authBLL.Authenticate(getReq.Payload.WalletSideToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideTokenExpired);
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            if (tpCheck == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideLoginFailed);
            }
            if (tpCheck.Locked)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountLocked);
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
            // 取出對方支援的Min/Max預設設定
            var limit = JsonConvert.DeserializeObject<Limit>(_tpBetLimitData[tpCustInfo.CurrencyId].LimitGroup);

            GetPlayerInfoResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
            {
                Payload = new Payload_GetPlayerInfoResp()
                {
                    OperatorCustId = tpCustInfoAuth.TpUserName,
                    UserName = tpCustInfoAuth.TpUserName,
                    CurrencyISO = tpCustInfoAuth.IsTest ? _testCurrency : tpCustInfoAuth.TpCurrencyId,
                    Currency = tpCustInfoAuth.IsTest ? _testCurrency : tpCustInfoAuth.TpCurrencyId,
                    Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    MinBet = limit.Min,
                    MaxBet = limit.Max,
                },
            };
            return _tools.ToJson(response);
        }

        public dynamic GetBalance(string request)
        {
            var getReq = JsonConvert.DeserializeObject<GetBalanceReq_MaxGame>(request);

            // check player account exist
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.Payload.OperatorCustId);
            if (tpCustInfo == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountNotExist);
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
            GetBalanceResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
            {
                Payload = new Payload_GetBalanceResp()
                {
                    Balance = _tools.Floor(tpCustBalance.Balance, 2),
                },
            };
            return _tools.ToJson(response);
        }

        public dynamic PlaceBet(string request)
        {
            string logName = $"{_thisTp}_PlaceBet";
            var getReq = JsonConvert.DeserializeObject<BettingData_MaxGame>(request);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.Payload.OperatorCustId);
            if (tpCheck == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideLoginFailed);
            }
            if (tpCheck.Locked)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountLocked);
            }

            int result = PlaceBetPK_MaxGame(getReq, out decimal tpAffterAmount, out List<BetDataSetList_MaxGame> refData);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                PlaceBetResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
                {
                    Payload = new Payload_PlaceBetResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpAffterAmount, 2),
                    },
                };
                response.Payload.BetDataSet = refData;

                return _tools.ToJson(response);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                PlaceBetResp response = new(ErrorCodeOfMaxGame.OpSideDuplicateRequest, getReq.Seq)
                {
                    Payload = new Payload_PlaceBetResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                        //BetDataSet 
                    },
                };
                return _tools.ToJson(response);
            }
            else if (tpAffterAmount < 0)
            {
                PlaceBetResp response = new(ErrorCodeOfMaxGame.OpSidePlayerHasInsufficientFunds, getReq.Seq)
                {
                    Payload = new Payload_PlaceBetResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpAffterAmount, 2),
                        //BetDataSet 
                    },
                };
                return _tools.ToJson(response);
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError));
        }

        public dynamic EndGame(string request)
        {
            string logName = $"{_thisTp}_EndGame";
            var getReq = JsonConvert.DeserializeObject<PayoutData_MaxGame>(request);

            int result = PayoutPK_MaxGame(getReq, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                EndGameResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
                {
                    Payload = new Payload_EndGameResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpAffterAmount, 2),
                    },
                };
                return _tools.ToJson(response);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                EndGameResp response = new(ErrorCodeOfMaxGame.OpSideDuplicateRequest, getReq.Seq)
                {
                    Payload = new Payload_EndGameResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    },
                };
                return _tools.ToJson(response);
            }
            else if (result == -2)
            {
                return _tools.ToJson(new BaseResp(ErrorCodeOfMaxGame.OpSideTransactionFailed));
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError));
        }

        public dynamic VoidGame(string request)
        {
            string logName = $"{_thisTp}_VoidGame";
            var getReq = JsonConvert.DeserializeObject<CancelBetData_MaxGame>(request);

            int result = CancelBetPK_MaxGame(getReq, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                VoidGameResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
                {
                    Payload = new Payload_EndGameResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpAffterAmount, 2),
                    },
                };
                return _tools.ToJson(response);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                VoidGameResp response = new(ErrorCodeOfMaxGame.OpSideDuplicateRequest, getReq.Seq)
                {
                    Payload = new Payload_EndGameResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    },
                };
                return _tools.ToJson(response);
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError));
        }

        public dynamic Bonus(string request)
        {
            string logName = $"{_thisTp}_Bonus";
            var getReq = JsonConvert.DeserializeObject<BonusData_MaxGame>(request);

            int result = BonusPK_MaxGame(getReq, out decimal tpAffterAmount, out string refNo);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                BonusResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
                {
                    Payload = new Payload_BonusResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpAffterAmount, 2),
                        RefNo = refNo,
                    },
                };
                return _tools.ToJson(response);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                BonusResp response = new(ErrorCodeOfMaxGame.OpSideDuplicateRequest, getReq.Seq)
                {
                    Payload = new Payload_BonusResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    },
                };
                return _tools.ToJson(response);
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(new BaseResp(ErrorCodeOfMaxGame.OpSideUnspecifiedError));
        }

        #endregion

        #region ExchangeRate & Service

        private TpBetFailureRecord GetTpBetFailureParam_MaxGame(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, BettingData_MaxGame bettingData_MaxGame, TpCheckRecordCode Code, decimal AfterAmount)
        {
            // BLL會經過匯率換算 下列還原為原始資料
            var InfoData = new
            {
                CurrentBalance = custBalance.Balance / tpCurrency.ExchangeRate,
                BetAmount = bettingData_MaxGame.Payload.BetDataSet.Sum(i => i.ActualStake) / tpCurrency.ExchangeRate,
                InsufficientBalance = AfterAmount,
                ExchangeRate = tpCurrency.ExchangeRate,
            };

            return new TpBetFailureRecord
            {
                TransId = bettingData_MaxGame.Seq,
                TpId = (int)_thisTp,
                TpGameType = _gameType,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }

        private int PlaceBetPK_MaxGame(BettingData_MaxGame bettingData_MaxGame, out decimal afterAmount, out List<BetDataSetList_MaxGame> refData)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingData_MaxGame.Payload.OperatorCustId, _gameType);
            refData = new List<BetDataSetList_MaxGame>();

            if (tpCurrency.ExchangeRate != 0)
            {
                custBalance.Balance *= tpCurrency.ExchangeRate;
                foreach (var betDataSet in bettingData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Stake *= tpCurrency.ExchangeRate;
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                }
            }
            var totalActualStake = bettingData_MaxGame.Payload.BetDataSet.Sum(i => i.ActualStake);

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = GetTpBetFailureParam_MaxGame(tpCustInfo, custBalance, tpCurrency, bettingData_MaxGame, TpCheckRecordCode.InsufficientBalance, afterAmount);
                _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(totalActualStake);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = GetTpBetFailureParam_MaxGame(tpCustInfo, custBalance, tpCurrency, bettingData_MaxGame, TpCheckRecordCode.InsufficientBalance, afterAmount);
                _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var result = _bettingService.PlaceBetPK_MaxGame(tpCustInfo, bettingData_MaxGame, out afterAmount, out refData);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int PayoutPK_MaxGame(PayoutData_MaxGame payoutData_MaxGame, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, payoutData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in payoutData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                    betDataSet.NetStake *= tpCurrency.ExchangeRate;
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var result = _settledService.PayoutPK_MaxGame(tpCustInfo, payoutData_MaxGame, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelBetPK_MaxGame(CancelBetData_MaxGame cancelBetData_MaxGame, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, cancelBetData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in cancelBetData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Stake *= tpCurrency.ExchangeRate;
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                }
            }

            var result = _settledService.CancelBetPK_MaxGame(tpCustInfo, cancelBetData_MaxGame, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int BonusPK_MaxGame(BonusData_MaxGame bonusData_MaxGame, out decimal afterAmount, out string refNo)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bonusData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in bonusData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var result = _settledService.BonusPK_MaxGame(tpCustInfo, bonusData_MaxGame, out afterAmount, out refNo);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion
    }
}
