using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {
        private void DoSettleAWCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.AWC;

            try
            {
                var unsettledList = settledBLL.GetUnsettledListCF_AWC();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.userId);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.platformTxId,
                                GameType = GameType.AWC_Rooster,
                                TpUserName = unsettled.userId,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = unsettled.TicketStatus == 0 ? SettlementType.UnSettled : SettlementType.SettledForValidNotRollover,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus
                            };

                            var result = settledBLL.Settled_AWC(tpCustInfo, betRecordInfo);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAWC");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleAWC");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleAWC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleOne789LTWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.One789;
            var gameType = (int)GameType.One789_Lottery;
            string logName = "Settle_One789_LT";

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_One789(gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        var wager = settledBLL.GetSettle_TpWagerOne789(gameType, unsettled.TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, wager.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (wager.Stake == unsettled.Stake && wager.WinLostAmount == unsettled.WinLostAmount)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = wager.Stake,
                                        ValidBet = wager.ValidBet,
                                        WinlostAmount = wager.WinLostAmount,
                                        TransId = wager.TransId,
                                        GameType = (GameType)gameType,
                                        TpUserName = wager.Account,
                                        SettledTime = unsettled.SettleTime,
                                        SettlementType = SettlementType.SettledForValid
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Lottery);

                                    settledBLL.SetSettleFlag_TpWagerOne789(gameType, unsettled.TransId);
                                    settledBLL.SetSettleFlag_Bettrans_One789(gameType, unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleOne789LT.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleOne789LT_789Work(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.One789;
            var gameType = (int)GameType.One789_Lottery_789;
            string logName = "Settle_One789_LT_789";

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_One789(gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        var wager = settledBLL.GetSettle_TpWagerOne789(gameType, unsettled.TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, wager.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.ValidBet *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                }
                                if (wager.Stake == unsettled.Stake && wager.WinLostAmount == unsettled.WinLostAmount)
                                {
                                    BetRecordInfo betRecordInfo = new BetRecordInfo
                                    {
                                        TpId = thisTp,
                                        Stake = wager.Stake,
                                        ValidBet = wager.ValidBet,
                                        WinlostAmount = wager.WinLostAmount,
                                        TransId = wager.TransId,
                                        GameType = (GameType)gameType,
                                        TpUserName = wager.Account,
                                        SettledTime = unsettled.SettleTime,
                                        SettlementType = SettlementType.SettledForValid
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Lottery);

                                    settledBLL.SetSettleFlag_TpWagerOne789(gameType, unsettled.TransId);
                                    settledBLL.SetSettleFlag_Bettrans_One789(gameType, unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), logName);
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timerSettleOne789LT_789.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

    }
}
