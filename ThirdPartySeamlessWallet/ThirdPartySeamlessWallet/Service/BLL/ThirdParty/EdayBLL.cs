using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class EdayBLL : IEdayBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        public EdayBLL(IServiceProvider provider)
        {
            _thisTp = Tp.Eday;
            _gameType = (int)GameType.Eday_Lottery;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }

        public EdayResponse<EdayGetBalanceResponse> GetBalance(string accountName)
        {
            var getErrorResponse = new EdayResponse<EdayGetBalanceResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };
            
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, accountName);
            
            if (tpCheck == null)
            {
                getErrorResponse.resultCode  = (int)EdayResultCode.PlayerAccountNotExist;
                getErrorResponse.message = EdayResultCode.PlayerAccountNotExist.GetEnumDescription();
                return getErrorResponse;
            }
            
            if (tpCheck.Locked)
            {
                getErrorResponse.resultCode  = (int)EdayResultCode.PlayerFrozen;
                getErrorResponse.message = EdayResultCode.PlayerFrozen.GetEnumDescription();
                return getErrorResponse;
            }
            
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, accountName, _gameType);
            var response = new EdayResponse<EdayGetBalanceResponse>()
            {
                resultCode = (int)EdayResultCode.Success,
                message = EdayResultCode.Success.GetEnumDescription(),
                data = new EdayGetBalanceResponse()
                {
                    balance = tpCustBalance.Balance
                }
            };
            
            return response;
        }

        public EdayResponse<EdayBetRequestResponse> BetRequest(EdayBetRequest request)
        {
            var logName = $"{_thisTp.ToString()}_BetRequest";
            var getErrorResponse = new EdayResponse<EdayBetRequestResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };

            try
            {
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Eday, request.player);
                
                if (tpCheck == null)
                {
                    getErrorResponse.resultCode  = (int)EdayResultCode.PlayerAccountNotExist;
                    getErrorResponse.message = EdayResultCode.PlayerAccountNotExist.GetEnumDescription();
                    return getErrorResponse;
                }
            
                if (tpCheck.Locked)
                {
                    getErrorResponse.resultCode  = (int)EdayResultCode.PlayerFrozen;
                    getErrorResponse.message = EdayResultCode.PlayerFrozen.GetEnumDescription();
                    return getErrorResponse;
                }

                var requestData = new EdayBetRequestData()
                {
                    txId = request.txId,
                    token = request.token,
                    player = request.player,
                    amount = request.amount,
                    billId = request.billId.ToString(),
                    roundCode = request.roundCode,
                    gameCode = request.gameCode,
                    betType = request.betType,
                    timestamp = request.timestamp,
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    UTCBetTime = DateTimeOffset.FromUnixTimeMilliseconds(request.timestamp).DateTime
                };
                
                var result = BetRequest_Eday(requestData, out decimal tpAfterAmount);
                
                if (result > 0 || result == -1)
                {
                    if (tpAfterAmount == 0 || result == -1)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.player, _gameType);
                        tpAfterAmount = tpCustBalance.Balance;
                    }
                    
                    return new EdayResponse<EdayBetRequestResponse>()
                    {
                        resultCode = (int)EdayResultCode.Success,
                        message = EdayResultCode.Success.GetEnumDescription(),
                        data = new EdayBetRequestResponse()
                        {
                            balance = tpAfterAmount
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                getErrorResponse.resultCode = (int)EdayResultCode.ServerError;
                getErrorResponse.message = EdayResultCode.ServerError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }

        public EdayResponse<EdayBetRequestResponse> BetRequestCancel(EdayBetRequest request)
        {
            var logName = $"{_thisTp.ToString()}_BetRequestCancel";
            var getErrorResponse = new EdayResponse<EdayBetRequestResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };
            try
            {
                var requestData = new EdayBetRequestData()
                {
                    txId = request.txId,
                    token = request.token,
                    player = request.player,
                    amount = request.amount,
                    billId = request.billId.ToString(),
                    roundCode = request.roundCode,
                    gameCode = request.gameCode,
                    betType = request.betType,
                    timestamp = request.timestamp,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    UTCBetTime = DateTimeOffset.FromUnixTimeMilliseconds(request.timestamp).DateTime
                };
            
                var result = BetRequestCancel_Eday(requestData, out decimal tpAfterAmount);

                if (result > 0 || result == -1)
                {
                    if (tpAfterAmount == 0 || result == -1)
                    { 
                        var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.player, _gameType);
                        tpAfterAmount = tpCustBalance.Balance;
                    }

                    return new EdayResponse<EdayBetRequestResponse>()
                    { 
                        resultCode = (int)EdayResultCode.Success,
                        message = EdayResultCode.Success.GetEnumDescription(),
                        data = new EdayBetRequestResponse()
                        {
                            balance = tpAfterAmount
                        }
                    };
                }
            
                if (result == -2)
                {
                    //查無注單 回覆相對應的
                    return new EdayResponse<EdayBetRequestResponse>()
                    { 
                        resultCode = (int)EdayResultCode.TransactionNotExist,
                        message = EdayResultCode.TransactionNotExist.GetEnumDescription(),
                    };
                }
                
                if (result == -3)
                {
                    //已經取消不給動 若真的有送 直接詢問Eday
                    _log2File.Info($"Eday BetRequestCancel : {request.billId} , Already CancelBet , Please check with Eday", logName);
                    return new EdayResponse<EdayBetRequestResponse>()
                    { 
                        resultCode = (int)EdayResultCode.InvalidStatus,
                        message = EdayResultCode.InvalidStatus.GetEnumDescription(),
                    };
                }
            }
            catch (Exception ex)
            {
                getErrorResponse.resultCode = (int)EdayResultCode.ServerError;
                getErrorResponse.message = EdayResultCode.ServerError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
        }

        public EdayResponse<EdayRoundUpdateResponse> RoundUpdate(EdayRoundUpdateRequest request)
        {
            var logName = $"{_thisTp.ToString()}_RoundUpdate";
            var result = 0;
            var roundUpdateDAL = new BettransLTEdayRoundUpdateDAL();
            var getErrorResponse = new EdayResponse<EdayRoundUpdateResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };
            try
            {
                foreach (var settleData in request.items)
                {
                    switch (request.txType)
                    {
                        //Round draw 正常派彩
                        case 1:
                            if (settleData.win > 0)
                            {
                                settleData.TicketStatus = (int)TicketStatus.Win;
                                settleData.TicketType = (int)BettransType.Payout;
                                settleData.SettleFlag = (int)SettleFlag.EndGame;
                                roundUpdateDAL = CreateRoundUpdateDAL(settleData, request.roundCode, request.timestamp,
                                    request.txType,request.txId,request.gameCode);
                            }
                            else
                            {
                                settleData.TicketStatus = (int)TicketStatus.Lost;
                                settleData.TicketType = (int)BettransType.Payout;
                                settleData.SettleFlag = (int)SettleFlag.EndGame;
                                roundUpdateDAL = CreateRoundUpdateDAL(settleData, request.roundCode, request.timestamp,
                                    request.txType,request.txId,request.gameCode);
                            }
                            result = RoundUpdate_Eday(roundUpdateDAL, out decimal afterAmount);
                            break;
                        //Void draw 
                        case 2:
                            break;
                        //Void round
                        case 3:
                            break;
                    }
                }
                
                if (result > 0 || result == -1)
                {
                    return new EdayResponse<EdayRoundUpdateResponse>()
                    { 
                        resultCode = (int)EdayResultCode.Success,
                        message = EdayResultCode.Success.GetEnumDescription(),
                        data = null
                    };
                }
                
                if (result == -2)
                {
                    //查無注單 回覆相對應的
                    return new EdayResponse<EdayRoundUpdateResponse>()
                    { 
                        resultCode = (int)EdayResultCode.TransactionNotExist,
                        message = EdayResultCode.TransactionNotExist.GetEnumDescription(),
                    };
                }
                
                if (result == -3)
                {
                    //已經取消不給動 若真的有送 直接詢問Eday
                    _log2File.Info($"Eday BetRequestCancel : {request.roundCode} , Already CancelBet , Please check with Eday", logName);
                    return new EdayResponse<EdayRoundUpdateResponse>()
                    { 
                        resultCode = (int)EdayResultCode.InvalidStatus,
                        message = EdayResultCode.InvalidStatus.GetEnumDescription(),
                    };
                }
            }
            catch (Exception ex)
            {
                getErrorResponse.resultCode = (int)EdayResultCode.ServerError;
                getErrorResponse.message = EdayResultCode.ServerError.GetEnumDescription();
                _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(getErrorResponse)}", logName, ex);
                return getErrorResponse;
            }
            
            _log2File.Error($"result={getErrorResponse}", logName);
            return getErrorResponse;
            
        }

        #region ExChange
        
        private int BetRequest_Eday(EdayBetRequestData bettingDataLT_Eday, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingDataLT_Eday.player);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingDataLT_Eday.player, _gameType);

            //目前只有開放THB，但查看文件有支援多種幣別，所以一樣有匯率換算。
            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataLT_Eday.amount *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(bettingDataLT_Eday.amount);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                return 0;
            }
            
            var result = _bettingService.BetRequest_Eday(tpCustInfo, bettingDataLT_Eday, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        private int BetRequestCancel_Eday(EdayBetRequestData cancelDataLT_Eday, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, cancelDataLT_Eday.player);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            
            var result = _settledService.BetRequestCancel_Eday(tpCustInfo, cancelDataLT_Eday, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        private int RoundUpdate_Eday(BettransLTEdayRoundUpdateDAL settleDataLT_Eday, out decimal afterAmount)
        {
            var result = 0;
            
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, settleDataLT_Eday.player);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            
            if (tpCurrency.ExchangeRate != 0)
            {
                settleDataLT_Eday.bet *= tpCurrency.ExchangeRate;
                settleDataLT_Eday.win *= tpCurrency.ExchangeRate;
            }
            
            result = _settledService.RoundUpdate_Eday(tpCustInfo, settleDataLT_Eday, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        #endregion
        
        #region private
        
        private BettransLTEdayRoundUpdateDAL CreateRoundUpdateDAL(EdayRoundUpdateData roundUpdateData,string roundCode,long timestamp,int txType,string txId, string gameCode,int eventId = 0)
        {
            return new BettransLTEdayRoundUpdateDAL()
            {
                txId = txId,
                timestamp = timestamp,
                player = roundUpdateData.player,
                bet = roundUpdateData.bet,
                win = roundUpdateData.win,
                eventId = eventId,
                roundCode = roundCode,
                gameCode = gameCode,
                TicketType = roundUpdateData.TicketType,
                TicketStatus = roundUpdateData.TicketStatus,
                SettleFlag = roundUpdateData.SettleFlag,
                txType = txType,
                UTCSettleBetTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).DateTime,
            };
        }
        
        
        #endregion
    }
}