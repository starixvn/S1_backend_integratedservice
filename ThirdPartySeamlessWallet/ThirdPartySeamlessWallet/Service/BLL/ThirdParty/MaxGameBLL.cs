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
        private IEnumerable<TpBetLimit> _tpBetLimitData;
        private readonly Log2File _log2File;
        private readonly Tp _commonTp;
        private const string _testCurrency = "UUS";
        private readonly Dictionary<string, Tp> _gameOfTpId = new()
        {
            { "maxgame", Tp.MaxGame },
            { "sabaplay", Tp.SabaPlay },
            { "uuslots", Tp.UuSlots },
            { "live22", Tp.Live22 },
            { "fastspin", Tp.FastSpin },
            { "jili", Tp.JILI }
        };
        private readonly Dictionary<int, WalletKind> _gameOfWalletKind = new()
        {
            { (int)GameType.MaxGame_Poker, WalletKind.BalanceWallet },
            { (int)GameType.SabaPlay_Poker, WalletKind.BalanceWallet },
            { (int)GameType.UuSlots_RNG, WalletKind.BonusWallet },
            { (int)GameType.Live22_RNG, WalletKind.BonusWallet },
            { (int)GameType.FastSpin_RNG, WalletKind.BonusWallet },
            { (int)GameType.JILI_RNG, WalletKind.BalanceWallet }
        };
        private readonly Dictionary<int, TpCategory> _gameOfTpCategory = new()
        {
            { (int)GameType.MaxGame_Poker, TpCategory.Games },
            { (int)GameType.SabaPlay_Poker, TpCategory.Games },
            { (int)GameType.UuSlots_RNG, TpCategory.Slots },
            { (int)GameType.Live22_RNG, TpCategory.Slots },
            { (int)GameType.FastSpin_RNG, TpCategory.Slots },
            { (int)GameType.JILI_RNG, TpCategory.Slots },
        };

        public MaxGameBLL(IServiceProvider provider)
        {
            _commonTp = Tp.MaxGame;
            _tools = provider.GetRequiredService<Tools>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpBetLimitData = provider.GetRequiredService<IEnumerable<TpBetLimit>>();
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

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }

            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)tpId, getReq.Payload.OperatorCustId);
            if (tpCheck == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideLoginFailed);
            }
            if (tpCheck.Locked)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountLocked);
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)tpId, getReq.Payload.OperatorCustId);
            if (tpCustInfo.CustId == 0)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideDbError);
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);

            // 取出對方支援的Min/Max預設設定
            var limit = JsonConvert.DeserializeObject<Limit>(_tpBetLimitData.Where(i => i.GameType == (int)gameType).FirstOrDefault(item => item.CurrencyId == tpCustInfo.CurrencyId).LimitGroup);

            GetPlayerInfoResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
            {
                Payload = new Payload_GetPlayerInfoResp()
                {
                    OperatorCustId = getReq.Payload.OperatorCustId,
                    UserName = getReq.Payload.OperatorCustId,
                    CurrencyISO = tpCustInfo.IsTest ? _testCurrency : tpCustBalance.TpCurrencyId,
                    Currency = tpCustInfo.IsTest ? _testCurrency : tpCustBalance.TpCurrencyId,
                    Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    MinBet = limit.Min,
                    MaxBet = limit.Max,
                    // SabaPlay大廳是一個整合的大廳，使用Game後拿到的Token，有需要多次呼叫GetPlayerInfo
                    WalletSideToken = _authBLL.ApplyToken(tpCustInfo)
                },
            };
            return _tools.ToJson(response);
        }

        public dynamic GetBalance(string request)
        {
            var getReq = JsonConvert.DeserializeObject<GetBalanceReq_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            // check player account exist
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)tpId, getReq.Payload.OperatorCustId);
            if (tpCustInfo == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountNotExist);
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
            string logName = $"{_commonTp}_PlaceBet";
            var getReq = JsonConvert.DeserializeObject<BettingData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)tpId, getReq.Payload.OperatorCustId);
            if (tpCheck == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideLoginFailed);
            }
            if (tpCheck.Locked)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountLocked);
            }

            int result = PlaceBetPK_MaxGame(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount, out List<BetDataSetList_MaxGame> refData);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
            string logName = $"{_commonTp}_EndGame";
            var getReq = JsonConvert.DeserializeObject<PayoutData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            int result = PayoutPK_MaxGame(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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

        public dynamic PlaceBetAndSettle(string request)
        {
            string logName = $"{_commonTp}_PlaceBetAndSettle";
            var getReq = JsonConvert.DeserializeObject<PayoutData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)tpId, getReq.Payload.OperatorCustId);
            if (tpCheck == null)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideLoginFailed);
            }
            if (tpCheck.Locked)
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideAccountLocked);
            }

            int result = PlaceBetAndSettle(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount);

            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
            string logName = $"{_commonTp}_VoidGame";
            var getReq = JsonConvert.DeserializeObject<CancelBetData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            int result = CancelBetPK_MaxGame(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
            string logName = $"{_commonTp}_Bonus";
            var getReq = JsonConvert.DeserializeObject<BonusData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            int result = BonusPK_MaxGame(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount, out string refNo);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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

        public dynamic FreeSpin(string request)
        {
            string logName = $"{_commonTp}_FreeSpin";
            var getReq = JsonConvert.DeserializeObject<FreeSpinData_MaxGame>(request);

            if (string.IsNullOrEmpty(getReq.Payload.GameType) || !_gameOfTpId.ContainsKey(getReq.Payload.GameType.ToLower()))
            {
                return new BaseResp(ErrorCodeOfMaxGame.OpSideGameNotFound);
            }
            var tpId = _gameOfTpId[getReq.Payload.GameType.ToLower()];
            var gameType = _thirdPartyBLL.GetGameType((int)tpId, getReq.Payload.GameType.ToLower());

            int result = FreeSpinPK_MaxGame(getReq, (int)tpId, (int)gameType, out decimal tpAffterAmount, out List<BetDataSetList_MaxGame> refData);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                FreeSpinResp response = new(ErrorCodeOfMaxGame.Success, getReq.Seq)
                {
                    Payload = new Payload_FreeSpinResp()
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
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)tpId, getReq.Payload.OperatorCustId, (int)gameType);
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

        private static TpBetFailureRecord GetTpBetFailureParam_MaxGame(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, BettingData_MaxGame bettingData_MaxGame, TpCheckRecordCode Code, decimal AfterAmount, int tpId, int gameType)
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
                TpId = tpId,
                TpGameType = gameType,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }

        private int PlaceBetPK_MaxGame(BettingData_MaxGame bettingData_MaxGame, int tpId, int gameType, out decimal afterAmount, out List<BetDataSetList_MaxGame> refData)
        {
            var walletKind = _gameOfWalletKind[gameType];

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bettingData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBLL.GetBalance(tpId, bettingData_MaxGame.Payload.OperatorCustId, gameType);
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
                var TpCheckRecordParam = GetTpBetFailureParam_MaxGame(tpCustInfo, custBalance, tpCurrency, bettingData_MaxGame, TpCheckRecordCode.InsufficientBalance, afterAmount, tpId, gameType);
                _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(totalActualStake);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                var TpCheckRecordParam = GetTpBetFailureParam_MaxGame(tpCustInfo, custBalance, tpCurrency, bettingData_MaxGame, TpCheckRecordCode.InsufficientBalance, afterAmount, tpId, gameType);
                _thirdPartyBLL.AddTpBetFailureRecord(TpCheckRecordParam);
                return 0;
            }

            var spName = GetSpNameList(gameType);
            var result = _bettingService.PlaceBetPK_MaxGame(tpCustInfo, bettingData_MaxGame, tpId, gameType, walletKind, spName, out afterAmount, out refData);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int PlaceBetAndSettle(PayoutData_MaxGame payoutData_MaxGame, int tpId, int gameType, out decimal afterAmount)
        {
            var walletKind = _gameOfWalletKind[gameType];
            var tpCategory = _gameOfTpCategory[gameType];
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, payoutData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in payoutData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                    betDataSet.NetStake *= tpCurrency.ExchangeRate;
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var spName = GetSpNameList(gameType);
            var result = _settledService.PlaceBetAndSettle_MaxGame(tpCustInfo, payoutData_MaxGame, tpId, gameType, walletKind, tpCategory, spName, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }


        private int PayoutPK_MaxGame(PayoutData_MaxGame payoutData_MaxGame, int tpId, int gameType, out decimal afterAmount)
        {
            var walletKind = _gameOfWalletKind[gameType];
            var tpCategory = _gameOfTpCategory[gameType];
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, payoutData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in payoutData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                    betDataSet.NetStake *= tpCurrency.ExchangeRate;
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var spName = GetSpNameList(gameType);
            var result = _settledService.PayoutPK_MaxGame(tpCustInfo, payoutData_MaxGame, tpId, gameType, walletKind, tpCategory, spName, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelBetPK_MaxGame(CancelBetData_MaxGame cancelBetData_MaxGame, int tpId, int gameType, out decimal afterAmount)
        {
            var walletKind = _gameOfWalletKind[gameType];
            var tpCategory = _gameOfTpCategory[gameType];
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, cancelBetData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in cancelBetData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Stake *= tpCurrency.ExchangeRate;
                    betDataSet.ActualStake *= tpCurrency.ExchangeRate;
                }
            }

            var spName = GetSpNameList(gameType);
            var result = _settledService.CancelBetPK_MaxGame(tpCustInfo, cancelBetData_MaxGame, tpId, gameType, walletKind, tpCategory, spName, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int BonusPK_MaxGame(BonusData_MaxGame bonusData_MaxGame, int tpId, int gameType, out decimal afterAmount, out string refNo)
        {
            var walletKind = _gameOfWalletKind[gameType];
            var tpCategory = _gameOfTpCategory[gameType];
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, bonusData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in bonusData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var spName = GetSpNameList(gameType);
            var result = _settledService.BonusPK_MaxGame(tpCustInfo, bonusData_MaxGame, tpId, gameType, walletKind, tpCategory, spName, out afterAmount, out refNo);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int FreeSpinPK_MaxGame(FreeSpinData_MaxGame freeSpinData_MaxGame, int tpId, int gameType, out decimal afterAmount, out List<BetDataSetList_MaxGame> refData)
        {
            var walletKind = _gameOfWalletKind[gameType];
            var tpCategory = _gameOfTpCategory[gameType];
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(tpId, freeSpinData_MaxGame.Payload.OperatorCustId);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            refData = new List<BetDataSetList_MaxGame>();

            if (tpCurrency.ExchangeRate != 0)
            {
                foreach (var betDataSet in freeSpinData_MaxGame.Payload.BetDataSet)
                {
                    betDataSet.Payout *= tpCurrency.ExchangeRate;
                }
            }

            var spName = GetSpNameList(gameType);
            var result = _settledService.FreeSpinPK_MaxGame(tpCustInfo, freeSpinData_MaxGame, tpId, gameType, walletKind, tpCategory, spName, out afterAmount, out refData);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private static SpNameListOfMaxGame GetSpNameList(int gameType)
        {
            var GetTicketNoSpName = "Service_BettransPK_MaxGame_TicketNo_Get";
            var InsertBettransSpName = "Service_BettransPK_MaxGame_Ins_V2";
            var InsertPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins";
            var CancelBettransSpName = "Service_BettransPK_MaxGame_CancelBet_Set";

            switch (gameType)
            {
                case (int)GameType.SabaPlay_Poker:
                    GetTicketNoSpName = "Service_BettransPK_SabaPlay_TicketNo_Get";
                    InsertBettransSpName = "Service_BettransPK_SabaPlay_Ins";
                    CancelBettransSpName = "Service_BettransPK_SabaPlay_CancelBet_Set";
                    break;
                case (int)GameType.UuSlots_RNG:
                    GetTicketNoSpName = "Service_BettransRng_UuSlots_TicketNo_Get";
                    InsertBettransSpName = "Service_BettransRng_UuSlots_Ins";
                    CancelBettransSpName = "Service_BettransRng_UuSlots_CancelBet_Set";
                    break;
                case (int)GameType.Live22_RNG:
                    GetTicketNoSpName = "Service_BettransRng_Live22_TicketNo_Get";
                    InsertBettransSpName = "Service_BettransRng_Live22_Ins";
                    CancelBettransSpName = "Service_BettransRng_Live22_CancelBet_Set";
                    break;
                case (int)GameType.FastSpin_RNG:
                    GetTicketNoSpName = "Service_BettransRng_FastSpin_TicketNo_Get";
                    InsertBettransSpName = "Service_BettransRng_FastSpin_Ins";
                    CancelBettransSpName = "Service_BettransRng_FastSpin_CancelBet_Set";
                    break;
                case (int)GameType.JILI_RNG:
                    GetTicketNoSpName = "Service_BettransRng_JILI_TicketNo_Get";
                    InsertBettransSpName = "Service_BettransRng_JILI_Ins";
                    CancelBettransSpName = "Service_BettransRng_JILI_CancelBet_Set";
                    break;
                default:
                    // GameType.MaxGame_Poker
                    break;
            }

            return new SpNameListOfMaxGame
            {
                GetTicketNo = GetTicketNoSpName,
                InsertBettrans = InsertBettransSpName,
                InsertPayload = InsertPayloadSpName,
                SetCancelBet = CancelBettransSpName,
            };
        }

        #endregion
    }
}
