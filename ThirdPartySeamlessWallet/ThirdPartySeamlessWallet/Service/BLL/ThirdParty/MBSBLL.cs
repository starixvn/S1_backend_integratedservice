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
        private readonly int _agentLength = 7;

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
            _log2File.Info(JsonConvert.SerializeObject(requestData), logName);
            var mbsBetResponse = new MBSTransferInOutResponse()
            {
                code = "-1",
                msg = "Fail",
                resultBets = new List<MBSResultBet>()
            };
            
            try
            {
                //Marbles 下注需要判断余额是否足够【批量处理】，如果失败全部回滚
                var checkMarblesBet = CheckMarblesBet(requestData);
                
                if (checkMarblesBet != 1)
                {
                    switch (checkMarblesBet)
                    {
                        case -1:
                            mbsBetResponse.code = "-1";
                            mbsBetResponse.msg = "Insufficient balance";
                            return mbsBetResponse;
                        case -2:
                            mbsBetResponse.code = "-2";
                            mbsBetResponse.msg = "User does not exist";
                            return mbsBetResponse;
                        case -3:
                            mbsBetResponse.code = "-3";
                            mbsBetResponse.msg = "User is prohibited";
                            return mbsBetResponse;
                        default:
                            return mbsBetResponse;
                    }
                }
                
                //Marbles 下多注會是批量下注 所以走foreach跑
                foreach (var request in requestData.bets)
                {
                    
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
            _log2File.Info(JsonConvert.SerializeObject(requestData), logName);
            var mbsSettleResponse = new MBSTransferInOutResponse()
            {
                code = "-1",
                msg = "Fail",
                resultBets = new List<MBSResultBet>()
            };
            
            try
            {
                //派彩也是一起派 走foreach
                //派彩【批量处理】，可以部分成功或者全部成功
                //若失敗的直接如部分订单处理失败，无须返回对应的单条订单，我方直接判断缺失订单为失败处理
                foreach (var request in requestData.bets)
                {
                    request.account = MarblesGetTpUserName(request.account);
                    var result = Settle_MBS(request, out var tpAfterAmount);
                    //成功才進，失敗的直接不寫入response，要不然Marbles會視為成功單
                    if (result > 0)
                    {
                        if (tpAfterAmount == 0)
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

        public string MarblesGetTpUserName(string marblesAccount)
        {
            return marblesAccount.Substring(_agentLength);
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

        private int CheckMarblesBet(MBSTransferInOutRequest request)
        {
            var groupedBets = request.bets.GroupBy(b => MarblesGetTpUserName(b.account));
            
            foreach (var group in groupedBets)
            {
                var account = group.Key;
                var totalBet = group.Sum(b => b.bet);
                
                //名字會帶agent跟代理 先處理
                account = MarblesGetTpUserName(account);
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, account);

                if (tpCheck == null)
                {
                    return -2;
                }

                if (tpCheck.Locked)
                {
                    return -3;
                }
                
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, account);
                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)_thisTp, tpCustInfo.CurrencyId);
                var custBalance = _tpCustInfoBLL.GetBalance((int)_thisTp, account, _gameType);

                if (tpCurrency.ExchangeRate != 0)
                {
                    totalBet *= tpCurrency.ExchangeRate;
                    custBalance.Balance *= tpCurrency.ExchangeRate;
                }
                
                if (custBalance.Balance < totalBet)
                {
                    return -1;
                }
            }

            // 通過所有檢查回傳1
            return 1;
        }
        
        #endregion
    }
}