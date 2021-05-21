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
                    var spName = "Service_BettransSports_BTI_Settle_Set";
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
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName, ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), logName, ex);
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

        private void DoSettle9WWork(object state)
        {
            var forContinue = false;
            var isError = false;
            var thisTp = (int)Tp.NineW;
            var logName = "Settle_9W_Sports";

            try
            {
                var unsettledList = settledBLL.GetUnsettledListBySpName("Service_TpWager9W_Unsettle_Get", DataBase.Wager);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    var spName = "Service_TpWager9W_Settle_Set";
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
                                GameType = GameType.NineW_Sports,
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
                                   settledBLL.SetSettleFlagBySpName(spName, unsettled.TransId, dataBase: DataBase.Wager);
                                });
                            }

                        }
                        catch (Exception ex1)
                        {
                            Task.Run(() =>
                            {
                               settledBLL.SetSettleFlagBySpName(spName, unsettled.TransId, isSuccess: false, DataBase.Wager);
                            });
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName, ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), logName, ex);
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

                timerSettle9W.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

    }
}