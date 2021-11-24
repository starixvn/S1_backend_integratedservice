using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        private void DoSettleSGRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.SG;
            var gameType = GameType.SG_RNG;
            var category = TpCategory.Slots;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                            var wager = settledBLL.GetSettle_TpWagerRng_SG(unsettled.referenceId);
                            if (wager != null)
                            {

                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    wager.Stake *= tpCurrency.ExchangeRate;
                                    wager.WinLostAmount *= tpCurrency.ExchangeRate;
                                    wager.ProgressiveBet *= tpCurrency.ExchangeRate;
                                    wager.ProgressiveWin *= tpCurrency.ExchangeRate;
                                }

                                if (wager.Stake == unsettled.Stake && Math.Round(wager.WinLostAmount, 2) == unsettled.WinLostAmount)
                                {
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
                                        TransTime = unsettled.TransTime,
                                        SettlementType = SettlementType.SettledForValid,
                                        ProgressiveBet = wager.ProgressiveBet,
                                        ProgressiveWin = wager.ProgressiveWin,
                                    };

                                    settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                                    settledBLL.SetSettleFlag_SG((int)gameType, JsonConvert.SerializeObject(transferIdList));
                                    settledBLL.SetSettleFlag_TpWagerRng_SG(unsettled.referenceId);
                                }
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettleSGRng");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettlePG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
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

        private void DoSettleMegaWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Mega;
            var gameType = GameType.Mega_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_Mega";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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

                timerSettleMega.Change(dueTimeSpan, TimeSpan.Zero);
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

        private void DoSettleYGGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.YGG;
            var gameType = GameType.YGG_RNG;
            var category = TpCategory.Slots;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_YGG();

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
                                TransId = unsettled.reference,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_YGG(unsettled.reference, unsettled.subreference);
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1}", "SettleYGG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleYGG");
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

                timerSettleYGG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePTRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.PT;
            var gameType = GameType.PT_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_PT_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRng_PT();

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
                                unsettled.ProgressiveBet *= tpCurrency.ExchangeRate;
                                unsettled.ProgressiveWin *= tpCurrency.ExchangeRate;
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
                                ProgressiveBet = unsettled.ProgressiveBet,
                                ProgressiveWin = unsettled.ProgressiveWin,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                TransTime = unsettled.SettleTime,
                                SettlementType = thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId)
                            };

                            if (stake == 0 && validBet == 0 && winLostAmount == 0)
                            {
                                settledBLL.SetSettleFlag_TpWagerRng_PT(long.Parse(unsettled.TransId));
                            }
                            else
                            {
                                settledBLL.Settled(tpCustInfo, betRecordInfo, PromotionCategory.PTSlotsGame);
                                settledBLL.SetSettleFlag_TpWagerRng_PT(long.Parse(unsettled.TransId));
                            }
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

                timerSettlePTRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePPRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.PP;
            var gameType = GameType.PP_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_PP_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListRng_PP();

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
                                SettledTime = unsettled.SettleTime / 1000,
                                TransTime = unsettled.TransTime / 1000,
                                SettlementType = settledBLL.GetSettlementType(gameType, unsettled.gameId),
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            };

                            if (betRecordInfo.WinlostAmount == 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Draw;
                            }

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_BettransRng_PP(unsettled.TransId, unsettled.Account);
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

                timerSettlePPRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleJILIRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.JILI;
            var gameType = GameType.JILI_RNG;
            var category = TpCategory.Slots;
            string logName = "Settle_JILI_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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
                                TicketStatus = (TicketStatus)unsettled.TicketStatus,
                            };
                            if (unsettled.TicketStatus == (int)TicketStatus.Refund)
                            {
                                betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            }
                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_JILI((int)category, unsettled.TransId);
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

                timerSettleJILIRng.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleMGRngWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.MG;
            var gameType = GameType.MG_RNG;
            var category = TpCategory.Slots;
            var logName = "Settle_MG_Rng";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_MG("Service_BettransRng_MG_Unsettle_Get");

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

                            long unixTimestamp = unsettled.SettleTime - new DateTime(1970, 1, 1).Ticks;
                            unixTimestamp /= TimeSpan.TicksPerSecond;

                            long transTime = unsettled.TransTime - new DateTime(1970, 1, 1).Ticks;
                            transTime /= TimeSpan.TicksPerSecond;

                            var betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unixTimestamp,
                                TransTime = transTime,
                                SettlementType = thirdPartyBLL.GetGameCasinoSettlementType(thisTp, unsettled.GameId),
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            };

                            if (betRecordInfo.WinlostAmount == 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Draw;
                            }

                            settledBLL.Settled(tpCustInfo, betRecordInfo, category);
                            settledBLL.SetSettleFlag_MG("Service_BettransRng_MG_Settle_Set", unsettled.TransId);
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
                var dueTimeSpan = TimeSpan.FromMinutes(1);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }
                timerSettleMG.Change(dueTimeSpan, TimeSpan.Zero);
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
