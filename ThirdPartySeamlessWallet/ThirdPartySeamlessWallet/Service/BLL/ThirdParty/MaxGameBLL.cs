using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

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

        #region Model

        private class Limit
        {
            public decimal Min { get; set; }
            public decimal Max { get; set; }
        }

        #endregion

        #region Response

        public class ErrorResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
        }


        public class GetPlayerInfoResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
            public Payload_GetPlayerInfoResp Payload { get; set; }
        }

        public class Payload_GetPlayerInfoResp
        {
            private decimal _Balance = 0m;
            private decimal _MinBet = 0m;
            private decimal _MaxBet = 0m;
            public string OperatorCustId { get; set; }
            public string UserName { get; set; }
            public string CurrencyISO { get; set; }
            public string Currency { get; set; }
            public decimal Balance
            {
                get
                {
                    _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                    return _Balance;
                }
                set
                {
                    _Balance = value;
                }
            }
            public decimal MinBet
            {
                get
                {
                    _ = decimal.TryParse(_MinBet.ToString("N2"), out _MinBet);
                    return _MinBet;
                }
                set
                {
                    _MinBet = value;
                }
            }
            public decimal MaxBet
            {
                get
                {
                    _ = decimal.TryParse(_MaxBet.ToString("N2"), out _MaxBet);
                    return _MaxBet;
                }
                set
                {
                    _MaxBet = value;
                }
            }
            public string BetGroup { get; set; }
            public string WalletSideToken { get; set; }
        }

        public class GetBalanceResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
            public Payload_GetBalanceResp Payload { get; set; }
        }

        public class Payload_GetBalanceResp
        {
            private decimal _Balance = 0m;
            public decimal Balance
            {
                get
                {
                    _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                    return _Balance;
                }
                set
                {
                    _Balance = value;
                }
            }
        }

        public class PlaceBetResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
            public Payload_PlaceBetResp Payload { get; set; }
        }

        public class Payload_PlaceBetResp
        {
            private decimal _Balance = 0m;
            public string OperatorCustId { get; set; }
            public decimal Balance
            {
                get
                {
                    _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                    return _Balance;
                }
                set
                {
                    _Balance = value;
                }
            }
            public List<BetDataSetList_MaxGame> BetDataSet { get; set; }
        }

        //public class BetDataSet_PlaceBetResp
        //{
        //    public string TicketNo { get; set; }
        //    public string RefNo { get; set; }
        //}

        public class EndGameResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
            public Payload_EndGameResp Payload { get; set; }
        }

        public class Payload_EndGameResp
        {
            private decimal _Balance = 0m;
            public string OperatorCustId { get; set; }
            public decimal Balance
            {
                get
                {
                    _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                    return _Balance;
                }
                set
                {
                    _Balance = value;
                }
            }
        }

        public class VoidGameResp : EndGameResp
        {
        }

        public class BonusResp
        {
            public string Seq { get; set; }
            public int ErrorCode { get; set; }
            public string ErrorMessage { get; set; }
            public string TimeStamp { get; set; }
            public Payload_BonusResp Payload { get; set; }
        }

        public class Payload_BonusResp
        {
            private decimal _Balance = 0m;
            public string OperatorCustId { get; set; }
            public decimal Balance
            {
                get
                {
                    _ = decimal.TryParse(_Balance.ToString("N2"), out _Balance);
                    return _Balance;
                }
                set
                {
                    _Balance = value;
                }
            }
            public string RefNo { get; set; }
        }

        #endregion

        #region Api Verify & Process

        public dynamic GetPlayerInfo(string request)
        {
            var getReq = JsonConvert.DeserializeObject<GetPlayerInfoReq_MaxGame>(request);

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            if (string.IsNullOrEmpty(getReq.Payload.WalletSideToken))
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideTokenInvalid;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideTokenInvalid.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }

            if (!_authBLL.Authenticate(getReq.Payload.WalletSideToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideTokenExpired;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideTokenExpired.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            if (tpCheck == null)
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideLoginFailed;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideLoginFailed.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }
            if (tpCheck.Locked)
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideAccountLocked;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideAccountLocked.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
            // 取出對方支援的Min/Max預設設定
            var limit = JsonConvert.DeserializeObject<Limit>(_tpBetLimitData[tpCustInfo.CurrencyId].LimitGroup);

            GetPlayerInfoResp response = new()
            {
                Seq = getReq.Seq,
                ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
                Payload = new Payload_GetPlayerInfoResp()
                {
                    OperatorCustId = tpCustInfoAuth.TpUserName,
                    UserName = tpCustInfoAuth.TpUserName,
                    CurrencyISO = tpCustInfoAuth.TpCurrencyId,
                    Currency = tpCustInfoAuth.TpCurrencyId,
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

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            // check player account exist
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, getReq.Payload.OperatorCustId);
            if (tpCustInfo == null)
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideAccountNotExist;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideAccountNotExist.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
            GetBalanceResp response = new()
            {
                Seq = getReq.Seq,
                ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
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

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, getReq.Payload.OperatorCustId);
            if (tpCheck == null)
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideLoginFailed;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideLoginFailed.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }
            if (tpCheck.Locked)
            {
                getErrorResponse.ErrorCode = (int)ErrorCodeOfMaxGame.OpSideAccountLocked;
                getErrorResponse.ErrorMessage = ErrorCodeOfMaxGame.OpSideAccountLocked.GetEnumDescription();
                getErrorResponse.TimeStamp = GetCurrentTimeFormat();
                return getErrorResponse;
            }

            int result = PlaceBetPK_MaxGame(getReq, out decimal tpAffterAmount, out List<BetDataSetList_MaxGame> refData);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                PlaceBetResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                    ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                PlaceBetResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSideDuplicateRequest,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSideDuplicateRequest.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                PlaceBetResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSidePlayerHasInsufficientFunds,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSidePlayerHasInsufficientFunds.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
            return _tools.ToJson(getErrorResponse);
        }

        public dynamic EndGame(string request)
        {
            string logName = $"{_thisTp}_EndGame";
            var getReq = JsonConvert.DeserializeObject<PayoutData_MaxGame>(request);

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            int result = PayoutPK_MaxGame(getReq, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                EndGameResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                    ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                EndGameResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSideDuplicateRequest,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSideDuplicateRequest.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                getErrorResponse = new()
                {
                    Seq = Guid.NewGuid().ToString(),
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSideTransactionFailed,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSideTransactionFailed.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
                };
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(getErrorResponse);
        }

        public dynamic VoidGame(string request)
        {
            string logName = $"{_thisTp}_VoidGame";
            var getReq = JsonConvert.DeserializeObject<CancelBetData_MaxGame>(request);

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            int result = CancelBetPK_MaxGame(getReq, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                VoidGameResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                    ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                VoidGameResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSideDuplicateRequest,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSideDuplicateRequest.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
                    Payload = new Payload_EndGameResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    },
                };
                return _tools.ToJson(response);
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(getErrorResponse);
        }

        public dynamic Bonus(string request)
        {
            string logName = $"{_thisTp}_Bonus";
            var getReq = JsonConvert.DeserializeObject<BonusData_MaxGame>(request);

            ErrorResp getErrorResponse = new()
            {
                Seq = Guid.NewGuid().ToString(),
                ErrorCode = (int)ErrorCodeOfMaxGame.OpSideUnspecifiedError,
                ErrorMessage = ErrorCodeOfMaxGame.OpSideUnspecifiedError.GetEnumDescription(),
                TimeStamp = GetCurrentTimeFormat(),
            };

            int result = BonusPK_MaxGame(getReq, out decimal tpAffterAmount, out string refNo);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, getReq.Payload.OperatorCustId, _gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }

                BonusResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.Success,
                    ErrorMessage = ErrorCodeOfMaxGame.Success.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
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
                BonusResp response = new()
                {
                    Seq = getReq.Seq,
                    ErrorCode = (int)ErrorCodeOfMaxGame.OpSideDuplicateRequest,
                    ErrorMessage = ErrorCodeOfMaxGame.OpSideDuplicateRequest.GetEnumDescription(),
                    TimeStamp = GetCurrentTimeFormat(),
                    Payload = new Payload_BonusResp()
                    {
                        OperatorCustId = getReq.Payload.OperatorCustId,
                        Balance = _tools.Floor(tpCustBalance.Balance, 2),
                    },
                };
                return _tools.ToJson(response);
            }

            _log2File.Error($"result={result}", logName);
            return _tools.ToJson(getErrorResponse);
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

        public string GetCurrentTimeFormat()
        {
            return DateTime.UtcNow.ToString("O");
        }
    }
}
