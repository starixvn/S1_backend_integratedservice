using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class NTConnectBLL : INTConnectBLL
    {
        private readonly Tools _tools;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        public NTConnectBLL(IServiceProvider provider)
        {
            _thisTp = Tp.NTConnect;
            _gameType = (int)GameType.NTConnect_Lottery;
            _tools = provider.GetRequiredService<Tools>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public NTCResponse<NTCGetBalanceResponse> GetBalance(NTCGetBalanceRequest request)
        {
            
            var getErrorResponse = new NTCResponse<NTCGetBalanceResponse>()
            {
                status = "error",
                data = new NTCGetBalanceResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };
            
            if (string.IsNullOrEmpty(request.token))
            {
                getErrorResponse.data.ErrorCode = (int)NTConnectErrorCode.PlayerInvalid;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }
            
            if (!_authBLL.AuthenticateByTpToken(request.token, out TpCustInfoAuth tpCustInfoAuth))
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerInvalid;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }
            
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, tpCustInfoAuth.TpUserName);
            
            if (tpCheck == null)
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerInvalid;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerInvalid.GetEnumDescription();
                return getErrorResponse;
            }
            
            if (tpCheck.Locked)
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerFrozen;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }
            
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
            var response = new NTCResponse<NTCGetBalanceResponse>()
            {
                status = "success",
                data = new NTCGetBalanceResponse()
                {
                    username = tpCustInfoAuth.TpUserName,
                    balance = tpCustBalance.Balance
                }
            };
            
            return response;
        }
        
        public NTCResponse<NTCCheckBetResponse> CheckBet(NTCCheckBetRequest request)
        {
            var logName = $"{_gameType.ToString()}_CheckBet";
            DateTime dateTime = default;
            var getErrorResponse = new NTCResponse<NTCCheckBetResponse>()
            {
                status = "error",
                data = new NTCCheckBetResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };

            var checkBet = _bettingService.CheckBetLottery_93Connect(request);
            if (checkBet == null)
            {
                getErrorResponse.data.ErrorCode = (int)NTConnectErrorCode.CantFoundBetId;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.CantFoundBetId.GetEnumDescription();
                return getErrorResponse;
            }

            if (checkBet.status == "R")
            {
                checkBet.win_amt = 0.00m;
            }

            if (string.IsNullOrEmpty(checkBet.betTime))
            {
                if (checkBet.betTime != null) 
                    dateTime = DateTime.Parse(checkBet.betTime);
            }

            return new NTCResponse<NTCCheckBetResponse>()
            {
                status = "success",
                data = new NTCCheckBetResponse()
                {
                    stake = checkBet.stake,
                    win_amt = checkBet.win_amt,
                    status = checkBet.status,
                    bet_date = dateTime
                }

            };

        }

        public NTCResponse<NTCPlaceBetResponse> PlaceBet(NTCPlaceBetRequest request)
        {
            var logName = $"{_gameType.ToString()}_PlaceBet";
            var getErrorResponse = new NTCResponse<NTCPlaceBetResponse>()
            {
                status = "error",
                data = new NTCPlaceBetResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };
            try
            {
                if (!_authBLL.AuthenticateByTpToken(request.token, out TpCustInfoAuth tpCustInfoAuth))
                {
                    getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerInvalid;
                    getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerInvalid.GetEnumDescription();
                    return getErrorResponse;
                }
                
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.NTConnect, tpCustInfoAuth.TpUserName);
            
                if (tpCheck == null)
                {
                    getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerInvalid;
                    getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerInvalid.GetEnumDescription();
                    return getErrorResponse;
                }
            
                if (tpCheck.Locked)
                {
                    getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.PlayerFrozen;
                    getErrorResponse.data.ErrorMessage = NTConnectErrorCode.PlayerFrozen.GetEnumDescription();
                    return getErrorResponse;
                }

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
                request.betTime = CheckBetTime(request.betTime);
                request.bet_type = CheckBetType(request.bet_type);
                request.bet_typegroup = CheckBetTypeGroup(request.bet_typegroup);
                request.TicketType = (int)BettransType.Bet;
                request.TicketStatus = (int)TicketStatus.UnSettled;
                request.LocalBetTime = ConvertBetTime(request.betTime);
                var result = PlaceBetLottery_NTConnect(request, out decimal tpAffterAmount);
                
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, tpCustInfoAuth.TpUserName, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }
                    
                    return new NTCResponse<NTCPlaceBetResponse>()
                    {
                        status = "success",
                        data = new NTCPlaceBetResponse()
                        {
                            username = request.username,
                            balance = tpAffterAmount
                        }
                    };
                }
                
                if (tpAffterAmount < 0)
                {
                    getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.InsufficientBalance;
                    getErrorResponse.data.ErrorMessage = NTConnectErrorCode.InsufficientBalance.GetEnumDescription();
                    _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName);
                    return getErrorResponse;
                }

            }
            catch (Exception ex)
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.InternalError;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }

        public NTCResponse<NTCSettleBetResponse> SettleBet(NTCSettleBetRequest request)
        {
            var logName = $"{_gameType.ToString()}_PlaceBet";
            var getErrorResponse = new NTCResponse<NTCSettleBetResponse>()
            {
                status = "error",
                data = new NTCSettleBetResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };

            try
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
                request.TicketType = (int)BettransType.Payout;
                request.status = request.win_amt > 0 ? "W" : "L";
                request.TicketStatus = request.win_amt > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
                var result = SettleLottery_NTConnect(request, out decimal tpAffterAmount);
                if (result > 0 || result == -1)
                {
                    if (tpAffterAmount == 0 || result == -1)
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;
                    }

                    return new NTCResponse<NTCSettleBetResponse>()
                    {
                        status = "success",
                        data = new NTCSettleBetResponse()
                        {
                            username = request.username,
                            balance = tpAffterAmount
                        }
                    };
                }

            }
            catch (Exception ex)
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.InternalError;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }

        public NTCResponse<NTCCancelBetResponse> CancelBet(NTCCancelBetRequest request)
        {
            var logName = $"{_gameType.ToString()}_PlaceBet";
            var getErrorResponse = new NTCResponse<NTCCancelBetResponse>()
            {
                status = "error",
                data = new NTCCancelBetResponse()
                {
                    ErrorCode = (int)NTConnectErrorCode.InternalError,
                    ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription()
                }
            };

            try
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, request.username, _gameType);
                request.TicketType = (int)BettransType.CancelBet;
                request.TicketStatus = (int)TicketStatus.Refund;
                request.status = "C";
                var result = CancelLottery_NTConnect(request, out decimal tpAffterAmount , out string TransId);

                switch (result)
                {
                    case -2:
                        return new NTCResponse<NTCCancelBetResponse>()
                        {
                            status = "error",
                            data = new NTCCancelBetResponse()
                            {
                                ErrorCode = (int)NTConnectErrorCode.CantFoundBetId,
                                ErrorMessage = NTConnectErrorCode.CantFoundBetId.GetEnumDescription()
                            }
                        };
                    case > 0:
                    case -1:
                    {
                        if (tpAffterAmount != 0 && result != -1)
                            return new NTCResponse<NTCCancelBetResponse>()
                            {
                                status = "success",
                                data = new NTCCancelBetResponse()
                                {
                                    username = request.username,
                                    bet_id = TransId
                                }
                            };
                        tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.username, _gameType);
                        tpAffterAmount = tpCustBalance.Balance;

                        return new NTCResponse<NTCCancelBetResponse>()
                        {
                            status = "success",
                            data = new NTCCancelBetResponse()
                            {
                                username = request.username,
                                bet_id = TransId
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                getErrorResponse.data.ErrorCode  = (int)NTConnectErrorCode.InternalError;
                getErrorResponse.data.ErrorMessage = NTConnectErrorCode.InternalError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }
        
        #region Exchange

        private int PlaceBetLottery_NTConnect(NTCPlaceBetRequest bettingData_NTConnect, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_NTConnect.username);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_NTConnect.stake *= tpCurrency.ExchangeRate;
            }

            var result = _bettingService.PlaceBetLottery_93Connect(tpCustInfo, bettingData_NTConnect, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        private int SettleLottery_NTConnect(NTCSettleBetRequest settleData_NTConnect, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, settleData_NTConnect.username);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                settleData_NTConnect.bet_amt *= tpCurrency.ExchangeRate;
                settleData_NTConnect.win_amt *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.PayoutLT_93Connect(tpCustInfo, settleData_NTConnect, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int CancelLottery_NTConnect(NTCCancelBetRequest cancelData_NTConnect, out decimal afterAmount, out string TransId)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.NTConnect, cancelData_NTConnect.username);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)Tp.NTConnect, tpCustInfo.CurrencyId);

            var result = _settledService.CancelBetLT_93Connect(tpCustInfo, cancelData_NTConnect, out afterAmount, out TransId);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }
        
        #endregion

        #region private

        private string CheckBetTime(string betTime)
        {
            DateTime dateTime;
            if (DateTime.TryParse(betTime, out dateTime))
            {
                return dateTime.ToString("yyyy-MM-dd hh:MM:ss");
            }
            return DateTime.Now.ToString("yyyy-MM-dd hh:MM:ss");

        }

        private string CheckBetType(string BetType)
        {
            return string.IsNullOrEmpty(BetType) ? "NA" : BetType;
        }

        private string CheckBetTypeGroup(string BetTypeGroup)
        {
            return string.IsNullOrEmpty(BetTypeGroup) ? "NA" : BetTypeGroup;
        }

        private DateTime ConvertBetTime(string betTime)
        {
            DateTime dateTime;
            var convert = DateTime.TryParse(betTime, out dateTime);
            return convert ? new DateTimeOffset(dateTime, new TimeSpan(-4, 0, 0)).UtcDateTime : dateTime;
        }
        #endregion
    }
}