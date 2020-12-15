using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {

        private void DoSettleCQ9Work(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.CQ9;

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRNG_CQ9();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.roundid,
                                GameType = GameType.CQ9_RNG,
                                TpUserName = unsettled.account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlagRNG_CQ9(unsettled.roundid, 1);
                            });
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleCQ9");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleCQ9");
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

                timerSettleCQ9.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleGPIWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.GPI;

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRNG_GPI();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.cust_id);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.round_id,
                                GameType = GameType.GPI_RNG,
                                TpUserName = unsettled.cust_id,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = settledBLL.GetSettlementType(GameType.GPI_RNG, unsettled.game_id)
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);

                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlagRNG_GPI(unsettled.round_id, unsettled.cust_id, 1);
                            });
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleGPI");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleGPI");
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

                timerSettleGPI.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleSGRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.SG;
            var gameType = GameType.SG_RNG;
            var category = TpCategory.Slots;

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_SG((int)gameType);

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;
                            List<string> transferIdList = JsonConvert.DeserializeObject<List<string>>(unsettled.transferIdList);
                            transferIdList.Add(unsettled.referenceId);

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.referenceId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                                //BonusId = bonusId
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_SG((int)gameType, JsonConvert.SerializeObject(transferIdList));
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettleSGRng");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleSGRng");
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

                timerSettleSGRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.PG;
            var gameType = GameType.PG_RNG;
            var category = TpCategory.Slots;

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_PG();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.ParentBetId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_PG(unsettled.TransactionIdList);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettlePG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettlePG");
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

                timerSettlePG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleKiss918Work(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Kiss918;
            var gameType = GameType.Kiss918_RNG;
            var category = TpCategory.Slots;

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_Kiss918();

                if (unsettledList != null)
                {

                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            decimal stake = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = stake,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.uuid,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValidNotRollover,
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_TpWagerKiss918(unsettled.uuid);
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettleKiss918");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleKiss918");
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

                timerSettleKiss918.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleMegaWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Mega;
            var gameType = GameType.Mega_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_Mega";

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_Mega();

                if (unsettledList != null)
                {

                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            decimal stake = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = stake,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValidNotRollover,
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_TpWagerMega(unsettled.TransId);
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

                timerSettleMega.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
