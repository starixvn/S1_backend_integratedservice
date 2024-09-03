using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class MBSBLL : IMBSBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly Log2File _log2File;
        private readonly Tp _thisTp;
        private readonly int _gameType = 0;

        public MBSBLL(IServiceProvider provider)
        {
            _thisTp = Tp.Marbles;
            _gameType = (int)GameType.Marbles_Lottery;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _log2File = provider.GetRequiredService<Log2File>();
        }
        
        public MBSTransferInOutResponse Bet(MBSTransferInOutRequest requestData)
        {
            var logName = $"{_thisTp.ToString()}_Bet";
            _log2File.Info(JsonConvert.SerializeObject(requestData), $"Marbles_Bet");
            var mbsBetResponse = new MBSTransferInOutResponse()
            {
                code = "-1",
                msg = "Fail",
                resultBets = new List<MBSResultBet>()
            };
            
            try
            {
                //Marbles 下多注會是批量下注 所以走foreach跑
                foreach (var request in requestData.bets)
                {
                    //名字會帶agent跟代理 先處理
                    request.account = GetTpUserName(request.account);
                    var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)Tp.Marbles, request.account);

                    if (tpCheck == null)
                    {
                        mbsBetResponse.code = "-2";
                        mbsBetResponse.msg = "User does not exist";
                        return mbsBetResponse;
                    }

                    if (tpCheck.Locked)
                    {
                        mbsBetResponse.code = "-3";
                        mbsBetResponse.msg = "\"User is prohibited";
                        return mbsBetResponse;
                    }
                    
                    request.TicketType = (int)BettransType.Bet;
                    request.TicketStatus = (int)TicketStatus.UnSettled;
                    
                    var result = Bet_MBS(request,out decimal tpAfterAmount);

                    if (result > 0 || result == -1)
                    {
                        if (tpAfterAmount == 0 || result == -1)
                        {
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.account, _gameType);
                            tpAfterAmount = tpCustBalance.Balance;
                        }

                        var betSuccessResult = new MBSResultBet
                        {
                            transNo = request.transNo,
                            money = tpAfterAmount
                        };
                        
                        mbsBetResponse.resultBets.Add(betSuccessResult);
                        
                    }

                    if (tpAfterAmount < 0)
                    {
                        mbsBetResponse.code = "-4";
                        mbsBetResponse.msg = "Insufficient deduce amount";
                        _log2File.Error($"{request}\r\n{JsonConvert.SerializeObject(mbsBetResponse)}", logName);
                        return mbsBetResponse;
                    }
                }

                if (mbsBetResponse.resultBets.Count() != 0)
                {
                    return new MBSTransferInOutResponse()
                    {
                        traceId = requestData.traceId,
                        resultBets = mbsBetResponse.resultBets
                    };
                }
                
            }
            catch (Exception ex)
            {
                mbsBetResponse.code = "-9000";
                mbsBetResponse.msg = "fail";
                _log2File.Error($"{requestData}\r\n{JsonConvert.SerializeObject(mbsBetResponse)}", logName, ex);
                return mbsBetResponse;
            }

            _log2File.Error($"result={mbsBetResponse}", logName);
            return mbsBetResponse;
        }

        public MBSTransferInOutResponse Settle(MBSTransferInOutRequest requestData)
        {
            var logName = $"{_thisTp.ToString()}_Settle";
            _log2File.Info(JsonConvert.SerializeObject(requestData), $"Marbles_Settle");
            var mbsSettleResponse = new MBSTransferInOutResponse()
            {
                code = "-1",
                msg = "Fail",
                resultBets = new List<MBSResultBet>()
            };
            
            try
            {
                //派彩也是一起派 走foreach
                foreach (var request in requestData.bets)
                {
                    request.account = GetTpUserName(request.account);
                    var result = Settle_MBS(request, out var tpAfterAmount);
                    if (result > 0 || result == -1)
                    {
                        if (tpAfterAmount == 0 || result == -1)
                        { 
                            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, request.account, _gameType);
                            tpAfterAmount = tpCustBalance.Balance;
                        }
                        
                        var settledSuccessResult = new MBSResultBet
                        {
                            transNo = request.transNo,
                            money = tpAfterAmount
                        };
                        
                        mbsSettleResponse.resultBets.Add(settledSuccessResult);
                    }
                }
                
                return new MBSTransferInOutResponse()
                {
                    traceId = requestData.traceId,
                    resultBets = mbsSettleResponse.resultBets
                };
            }
            catch (Exception ex)
            {
                mbsSettleResponse.code = "-9000";
                mbsSettleResponse.msg = "fail";
                _log2File.Error($"{requestData}\r\n{JsonConvert.SerializeObject(mbsSettleResponse)}", logName, ex);
                return mbsSettleResponse;
            }
            
        }
        
        #region Exchange

        private int Bet_MBS(MBSBetSettleData bettingData_MBS, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0.00m;
        
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, bettingData_MBS.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
            var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, bettingData_MBS.account, _gameType);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingData_MBS.bet *= tpCurrency.ExchangeRate;
                bettingData_MBS.profit *= tpCurrency.ExchangeRate;
                bettingData_MBS.effecBet *= tpCurrency.ExchangeRate;
                bettingData_MBS.commission *= tpCurrency.ExchangeRate;
                custBalance.Balance *= tpCurrency.ExchangeRate;
            }

            if (custBalance.Balance < 0)
            {
                afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
                return 0;
            }

            var checkBalance = custBalance.Balance - Math.Abs(bettingData_MBS.bet);
            if (checkBalance < 0)
            {
                afterAmount = checkBalance / tpCurrency.ExchangeRate;
                return 0;
            }

            result = _bettingService.BetLT_Marbles(tpCustInfo, bettingData_MBS, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        private int Settle_MBS(MBSBetSettleData settleData_MBS, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0.00m;
        
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, settleData_MBS.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                settleData_MBS.profit *= tpCurrency.ExchangeRate;
                settleData_MBS.effecBet *= tpCurrency.ExchangeRate;
                settleData_MBS.commission *= tpCurrency.ExchangeRate;
            }
            
            if (settleData_MBS.profit > 0)
            {
                settleData_MBS.TicketStatus = (int)TicketStatus.Win;
            }
            else
            {
                settleData_MBS.TicketStatus = (int)TicketStatus.Lost;
            }
            settleData_MBS.TicketType = (int)BettransType.Payout;
            

            result = _settledService.SettleLT_Marbles(tpCustInfo, settleData_MBS, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }
        
        #endregion
        
        #region Private
        
        private string GetTpUserName(string marblesAccount)
        {
            return marblesAccount.Substring(7);
        }
        
        #endregion
    }
}