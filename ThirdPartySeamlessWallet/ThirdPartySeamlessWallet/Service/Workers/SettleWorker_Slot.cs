using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Context;
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
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

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
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
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
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleCQ9");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.cust_id);

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
                                TransTime = unsettled.TransTime,
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
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleGPI");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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

        private void DoSettleKiss918Work(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Kiss918;
            var gameType = GameType.Kiss918_RNG;
            var category = TpCategory.Slots;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
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
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettleKiss918");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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
        private void DoSettleIconicRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Iconic;
            var gameType = GameType.Iconic_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_Iconic_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRng_Iconic();

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
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.transactionId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.TransTime,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_BettransRng_Iconic(unsettled.transactionId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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

                timerSettleIconicRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        private void DoSettleNetentWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Netent;
            var gameType = GameType.Netent_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_Netent_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_Netent();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId.ToString()),
                            };

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_Netent(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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

                timerSettleNetent.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleRedTigerWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.RedTiger;
            var gameType = GameType.RedTiger_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_RedTiger_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_RedTiger();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);

                            decimal stake = unsettled.Stake;
                            decimal validBet = unsettled.Stake;
                            decimal winLostAmount = unsettled.WinLostAmount;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId.ToString(),
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId.ToString()),
                            };

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_RedTiger(unsettled.TransId);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", logName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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

                timerSettleRedTiger.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
    }
}
