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
        private void DoSettleSGFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            //int thisTp = (int)Tp.SG;
            var gameType = GameType.SG_Fish;
            //var category = TpCategory.Fish;
            try
            {
                var unsettledList = settledBLL.GetUnsettledList_SG((int)gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 500;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            settledBLL.SettledFish_SG(unsettled, out _);
                        }
                        catch (Exception ex1)
                        {
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleSGFish");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleSGFish");
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

                timerSettleSGFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleIconicFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Iconic;
            var gameType = GameType.Iconic_Fish;
            var category = TpCategory.Fish;
            string logName = "Settle_Iconic_Fish";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListFish_Iconic();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 0)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.transactionId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_TpWagerFish_Iconic(unsettled.transactionId);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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

                timerSettleIconicFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleCQ9FishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.CQ9;
            var gameType = GameType.CQ9_Fish;
            var category = TpCategory.Fish;
            string logName = "Settle_CQ9_Fish";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListFish_CQ9();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.roundid,
                                GameType = gameType,
                                TpUserName = unsettled.account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                            settledBLL.SetSettleFlagFish_CQ9(unsettled.roundid, 1);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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

                timerSettleCQ9Fish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleYLFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.YL;
            var gameType = GameType.YL_Fish;
            var category = TpCategory.Fish;
            string logName = "Settle_YL_Fish";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListFish_YL();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = unsettled.TicketStatus == (int)TicketStatus.Refund ? SettlementType.SettledForInvalid : SettlementType.SettledForValid,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);

                            settledBLL.SetSettleFlagFish_YL(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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

                timerSettleYLFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleJILIFishWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.JILI;
            var gameType = GameType.JILI_Fish;
            var category = TpCategory.Fish;
            string logName = "Settle_JILI_Fish";

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_JILI((int)category);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.ValidBet;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = unsettled.TicketStatus == (int)TicketStatus.Refund ? SettlementType.SettledForInvalid : SettlementType.SettledForValid,
                                TicketStatus = (TicketStatus)unsettled.TicketStatus
                            };

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_JILI((int)category, unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
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

                timerSettleJILIFish.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
