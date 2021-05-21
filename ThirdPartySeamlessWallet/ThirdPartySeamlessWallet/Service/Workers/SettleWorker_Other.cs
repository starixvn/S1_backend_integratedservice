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

        private void DoSettleOneGamingWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.OneGaming;
            string logName = "Settle_OneGaming_PK";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListPK_OneGaming();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate != 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                                unsettled.Revenue *= tpCurrency.ExchangeRate;
                            }
                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                Revenue = unsettled.Revenue,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.ticket,
                                GameType = GameType.OneGaming_Poker,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };
                            var result = settledBLL.Settled_OneGaming(tpCustInfo, betRecordInfo);
                            settledBLL.SetSettleFlag_TpWagerPK_OneGaming(unsettled.ticket);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
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

                timerSettleOneGaming.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleV8Work(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.V8;
            string logName = "Settle_V8_PK";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListPK_V8();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var account = unsettled.Account;

                            if (account.IndexOf("_") > -1)
                            {
                                account = account[(account.IndexOf("_") + 1)..];
                            }

                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                                unsettled.Revenue *= tpCurrency.ExchangeRate;
                            }

                            // revenue games
                            GameType gameType;
                            int[] kindId = { 220, 600, 610, 620, 630, 830, 860, 890, 930, 8100, 8540, 8600, 8700 };
                            if (kindId.Contains(unsettled.KindID))
                            {
                                gameType = GameType.V8_Poker;
                            }
                            else
                            {
                                gameType = GameType.V8_Poker_None;
                            }

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                Revenue = unsettled.Revenue,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.GameID,
                                GameType = gameType,
                                TpUserName = account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };
                            var result = settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            settledBLL.SetSettleFlag_TpWagerPK_V8(unsettled.GameID);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
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

                timerSettleV8.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleBTIWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.BTI;
            var logName = "Settle_BTI_Sports";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListBTI();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = GameType.BTI_Sports,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                                TicketStatus = unsettled.WinLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Sports);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_BTI(unsettled.Account, unsettled.TransId);
                                });
                            }

                        }
                        catch (Exception ex1)
                        {
                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlag_BTI(unsettled.Account, unsettled.TransId, isSuccess: false);
                            });
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

                timerSettleBTI.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        /// <summary>
        /// TpWagerKM 投注資料做結算
        /// </summary>
        /// <param name="state"></param>
        private void DoSettleKMWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.KM;
            var logName = "Settle_KM_PK";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListPK_KM("Service_TpWagerPK_KM_Unsettle_Get");

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = GameType.KM_Poker,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                            };

                            var result = settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            if (result == 1)
                            {
                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_KM("Service_TpWagerPK_KM_Settle_Set", unsettled.TransId, true);
                                });
                            }

                        }
                        catch (Exception ex1)
                        {
                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlag_KM("Service_TpWagerPK_KM_Settle_Set", unsettled.TransId, isSuccess: false);
                            });
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                log2File.Error(ex.ToString(), logName);
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

                timerSettleKM.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}