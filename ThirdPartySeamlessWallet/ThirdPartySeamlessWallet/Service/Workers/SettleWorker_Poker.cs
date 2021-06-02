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

        private void DoSettleAMBWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.AMB;
            var gameType = GameType.AMB_Poker;
            var category = TpCategory.Poker;
            string logName = "Settle_AMB";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListPK_AMB();

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
                            decimal revenue = unsettled.Revenue;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                Revenue = revenue,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            if (unsettled.IsRefund == 1)
                            {
                                //betRecordInfo.WinlostAmount = 0;
                                //betRecordInfo.Revenue = 0;
                                betRecordInfo.TicketStatus = (TicketStatus)TicketStatus.Refund;
                            }

                            var result = settledBLL.Settled_Without_Bonus(tpCustInfo, betRecordInfo);
                            settledBLL.SetSettleFlag_BettransPK_AMB(unsettled.TransId);
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

                timerSettleAMB.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}