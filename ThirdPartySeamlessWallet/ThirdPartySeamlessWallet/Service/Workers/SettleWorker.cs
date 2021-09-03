using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Org.BouncyCastle.Asn1.Cms;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Context;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {
        #region Fields

        private Timer timerSettleAG;
        private Timer timerSettleDG;
        private Timer timerSettleAllbet;
        private Timer timerSettleSA;
        private Timer timerSettleEBet;
        private Timer timerSettleWM;
        private Timer timerSettleCQ9;
        private Timer timerSettleCQ9Fish;
        private Timer timerSettleGPI;
        private Timer timerSettleSGRng;
        private Timer timerSettleSGFish;
        private Timer timerSettleAWC;
        private Timer timerSettlePG;
        private Timer timerSettleYGG;
        private Timer timerSettleKiss918;
        private Timer timerSettleOne789LT;
        private Timer timerSettleOne789LT_789;
        private Timer timerSettleOne789LC;
        private Timer timerSettleMega;
        private Timer timerSettleIconicRng;
        private Timer timerSettleIconicFish;
        private Timer timerSettleOneGaming;
        private Timer timerSettleV8;
        private Timer timerSettleYLFish;
        private Timer timerSettleN2Live;
        private Timer timerSettleN2LiveLC_Smart;
        private Timer timerSettlePTRng;
        private Timer timerSettlePTLC;
        private Timer timerSettleBTI;
        private Timer timerSettlePPRng;
        private Timer timerSettlePPLC;
        private Timer timerSettleAMB;
        private Timer timerSettle9W;
        private Timer timerSettleKM;
        private Timer timerSettleJILIRng;
        private Timer timerSettleJILIFish;
        private Timer timerSettleMG;
        private Timer timerSettleTCGSEA;
        private Timer timerSettleTCGVN;
        private Timer timerSettleMP;
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private readonly SettledBLL settledBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SettleWorker> _logger;

        #endregion

        public SettleWorker(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            log2File = provider.GetRequiredService<Log2File>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<SettleWorker>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettleAG = new Timer(DoSettleAGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleDG = new Timer(DoSettleDGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleAllbet = new Timer(DoSettleAllbetWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSA = new Timer(DoSettleSAWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleEBet = new Timer(DoSettleEBetWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleWM = new Timer(DoSettleWMWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleCQ9 = new Timer(DoSettleCQ9Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleCQ9Fish = new Timer(DoSettleCQ9FishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleGPI = new Timer(DoSettleGPIWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSGRng = new Timer(DoSettleSGRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSGFish = new Timer(DoSettleSGFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleAWC = new Timer(DoSettleAWCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePG = new Timer(DoSettlePGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleKiss918 = new Timer(DoSettleKiss918Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT = new Timer(DoSettleOne789LTWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT_789 = new Timer(DoSettleOne789LT_789Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LC = new Timer(DoSettleOne789LCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleMega = new Timer(DoSettleMegaWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleIconicRng = new Timer(DoSettleIconicRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleIconicFish = new Timer(DoSettleIconicFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOneGaming = new Timer(DoSettleOneGamingWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleV8 = new Timer(DoSettleV8Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleYGG = new Timer(DoSettleYGGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleYLFish = new Timer(DoSettleYLFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleN2Live = new Timer(DoSettleN2LiveWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleN2LiveLC_Smart = new Timer(DoSettleN2LiveLC_SmartWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePTRng = new Timer(DoSettlePTRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePTLC = new Timer(DoSettlePTLCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleBTI = new Timer(DoSettleBTIWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePPRng = new Timer(DoSettlePPRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePPLC = new Timer(DoSettlePPLCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettle9W = new Timer(DoSettle9WWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleKM = new Timer(DoSettleKMWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleAMB = new Timer(DoSettleAMBWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleJILIRng = new Timer(DoSettleJILIRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleJILIFish = new Timer(DoSettleJILIFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleMG = new Timer(DoSettleMGRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleTCGSEA = new Timer(DoSettleTCGWork, GameType.TCG_SEA_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleTCGVN = new Timer(DoSettleTCGWork, GameType.TCG_VN_Lottery, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleMP = new Timer(DoSettleMPWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        #region Methods

        private void DoSettleAGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            const int thisTp = (int)Tp.AG;
            var tpName = Enum.GetName(typeof(Tp), thisTp);
            
            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_AG();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var payoutList = settledBLL.Get_PayoutList_AG(unsettled.TransactionID);
                            var betList = new List<BettingDataLC_AG>();
                            List<string> transactionIDs = new List<string>();
                            if (payoutList != null)
                            {
                                foreach (var payout in payoutList)
                                {
                                    transactionIDs.AddRange(JsonConvert.DeserializeObject<IEnumerable<string>>(payout.TransactionIDs));
                                }

                                var bBetList = settledBLL.Get_BetList_AG(JsonConvert.SerializeObject(transactionIDs.Distinct()));

                                if (bBetList != null)
                                {
                                    betList.AddRange(bBetList);
                                }

                                if (betList.Count > 0 && betList.Sum(x => x.Value) == payoutList.Sum(x => x.ValidBetAmount))
                                {
                                    var billNoList = payoutList.Select(x => x.BillNo).ToArray();
                                    var result = settledBLL.Get_TpWagerAG_Settle(JsonConvert.SerializeObject(billNoList), out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime);

                                    if (result > 0)
                                    {
                                        string username = unsettled.Playname.Substring(unsettled.AgentCode.Length);

                                        var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AG, username);
                                        var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AG, tpCustInfo.CurrencyId);

                                        if (tpCurrency.ExchangeRate > 1)
                                        {
                                            stake *= tpCurrency.ExchangeRate;
                                            validBet *= tpCurrency.ExchangeRate;
                                            winLostAmount *= tpCurrency.ExchangeRate;
                                        }

                                        if (betList.Sum(x => x.Value) == stake)
                                        {
                                            BetRecordInfo betRecordInfo = new BetRecordInfo
                                            {
                                                TpId = (int)Tp.AG,
                                                Stake = stake,
                                                ValidBet = validBet,
                                                WinlostAmount = winLostAmount,
                                                TransId = unsettled.TransactionID,
                                                GameType = GameType.AG_LiveCasino,
                                                TpUserName = username,
                                                SettledTime = settleTime,
                                                SettlementType = SettlementType.SettledForValid
                                            };

                                            result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                            Task.Run(() =>
                                            {
                                                settledBLL.SetSettleFlag_TpWagerAG(JsonConvert.SerializeObject(billNoList));
                                                settledBLL.SetSettleFlag_AG(unsettled.TransactionID, JsonConvert.SerializeObject(transactionIDs));
                                            });
                                        }
                                    }
                                }
                                else
                                {
                                    continue;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAG", ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleAG", ex);
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

                timerSettleAG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleDGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.DG;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_DG();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var result = settledBLL.Get_TpWagerDG_Settle(unsettled.TransId, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime);

                            if (result > 0)
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Username);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);

                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    stake *= tpCurrency.ExchangeRate;
                                    validBet *= tpCurrency.ExchangeRate;
                                    winLostAmount *= tpCurrency.ExchangeRate;
                                }

                                BetRecordInfo betRecordInfo = new BetRecordInfo
                                {
                                    TpId = thisTp,
                                    Stake = stake,
                                    ValidBet = validBet,
                                    WinlostAmount = winLostAmount,
                                    TransId = unsettled.TransId,
                                    GameType = GameType.DG_LiveCasino,
                                    TpUserName = unsettled.Username,
                                    SettledTime = settleTime,
                                    SettlementType = SettlementType.SettledForValid
                                };

                                result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_TpWagerDG(unsettled.TransId);
                                    settledBLL.SetSettleFlag_DG(unsettled.TransId);
                                });
                            }
                            else if (result == -1)
                            {
                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_TpWagerDG(unsettled.TransId);
                                    settledBLL.SetSettleFlag_DG(unsettled.TransId);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleDG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleDG");
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

                timerSettleDG.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        private void DoSettleAllbetWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.Allbet;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_Allbet();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var result = settledBLL.Get_TpWagerAllbet_Settle(unsettled.BetNum, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime, out long transTime);

                            if (result > 0)
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Client);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);

                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    stake *= tpCurrency.ExchangeRate;
                                    validBet *= tpCurrency.ExchangeRate;
                                    winLostAmount *= tpCurrency.ExchangeRate;
                                }

                                BetRecordInfo betRecordInfo = new BetRecordInfo
                                {
                                    TpId = thisTp,
                                    Stake = stake,
                                    ValidBet = validBet,
                                    WinlostAmount = winLostAmount,
                                    TransId = unsettled.BetNum.ToString(),
                                    GameType = GameType.Allbet_LiveCasino,
                                    TpUserName = unsettled.Client,
                                    SettledTime = settleTime,
                                    TransTime = transTime,
                                    SettlementType = SettlementType.SettledForValid
                                };

                                result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                Task.Run(() =>
                                {
                                    settledBLL.SetSettleFlag_TpWagerAllbet(unsettled.BetNum);
                                    settledBLL.SetSettleFlag_Allbet(unsettled.BetNum);
                                });
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAllbet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleAllbet");
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

                timerSettleAllbet.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleSAWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.SA;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_SA();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            //$"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}"
                            var result = settledBLL.Get_TpWagerSA_Settle(unsettled.gameid, unsettled.username, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime, out long transTime);

                            if (result > 0)
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.username);
                                var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);

                                if (tpCurrency.ExchangeRate > 1)
                                {
                                    stake *= tpCurrency.ExchangeRate;
                                    validBet *= tpCurrency.ExchangeRate;
                                    winLostAmount *= tpCurrency.ExchangeRate;
                                }

                                BetRecordInfo betRecordInfo = new BetRecordInfo
                                {
                                    TpId = thisTp,
                                    Stake = stake,
                                    ValidBet = validBet,
                                    WinlostAmount = winLostAmount,
                                    TransId = $"{unsettled.gameid}_{unsettled.username}",
                                    GameType = GameType.SA_LiveCasino,
                                    TpUserName = unsettled.username,
                                    SettledTime = settleTime,
                                    TransTime = transTime,
                                    SettlementType = SettlementType.SettledForValid
                                };

                                result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                //Task.Run(() =>
                                //{
                                settledBLL.SetSettleFlag_TpWagerSA(unsettled.gameid, unsettled.username);
                                settledBLL.SetSettleFlag_SA(unsettled.gameid, unsettled.username);
                                //});
                            }
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleSA");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleSA");
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

                timerSettleSA.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleEBetWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.EBet;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_EBet();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.username);

                            decimal stake = unsettled.totalBet;
                            decimal validBet = unsettled.validBet;
                            decimal winLostAmount = unsettled.payout - unsettled.totalBet;

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = stake,
                                ValidBet = validBet,
                                WinlostAmount = winLostAmount,
                                TransId = unsettled.TransId,
                                GameType = GameType.EBet_LiveCasino,
                                TpUserName = unsettled.username,
                                SettledTime = unsettled.payoutTime / 1000,
                                SettlementType = SettlementType.SettledForValid
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            Task.Run(() =>
                            {
                                settledBLL.SetSettleFlag_EBet(unsettled.TransId);
                            });
                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleEBet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleEBet");
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

                timerSettleEBet.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleWMWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.WM;
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_WM();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        var wager = settledBLL.GetSettle_TpWagerWM(unsettled.TransId);
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
                                        GameType = GameType.WM_LiveCasino,
                                        TpUserName = wager.Account,
                                        SettledTime = wager.SettleTime,
                                        SettlementType = SettlementType.SettledForValid
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerWM(unsettled.TransId);
                                    settledBLL.SetSettleFlag_BettransLC_WM(unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleWM");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogContext.PushProperty("TP", tpName);
                log2File.Error(ex.ToString(), "SettleWM");
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

                timerSettleWM.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleOne789LCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.One789;
            var gameType = (int)GameType.One789_LiveCasino;
            string logName = "Settle_One789_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

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

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerOne789(gameType, unsettled.TransId);
                                    settledBLL.SetSettleFlag_Bettrans_One789(gameType, unsettled.TransId);
                                }

                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
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

                timerSettleOne789LC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleN2LiveWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.N2Live;
            var gameType = (int)GameType.N2Live_LiveCasino;
            string logName = "Settle_N2Live";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_BettransLC_N2Live();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        string TransId = unsettled.TransId;
                        var wager = settledBLL.GetSettle_TpWagerLC_N2Live(TransId);
                        if (wager != null)
                        {
                            try
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
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
                                        TransId = unsettled.TransId,
                                        GameType = (GameType)gameType,
                                        TpUserName = unsettled.Account,
                                        SettledTime = wager.SettleTime,
                                        SettlementType = SettlementType.SettledForValid,
                                        ReferenceNo = wager.betid // for fix cant mapping bonusid when data fixed can remove
                                    };

                                    var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                                    settledBLL.SetSettleFlag_TpWagerLC_N2Live(TransId);
                                    settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 1);
                                }
                            }
                            catch (Exception ex1)
                            {
                                LogContext.PushProperty("TP", tpName);
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
                            }
                        }
                        else
                        {
                            // smart and player bet
                            var smart = settledBLL.GetSettle_TpSmartWagerLC_N2Live(TransId);
                            if (smart != null)
                            {
                                settledBLL.SetSettleFlag_BettransLC_N2Live(TransId, 0);
                            }
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

                timerSettleN2Live.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettleN2LiveLC_SmartWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.N2Live;
            var gameType = (int)GameType.N2Live_LiveCasino;
            string logName = "Settle_N2Live_LC_Smart";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledList_TpSmartWagerLC_N2Live();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 200;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            string TransId = unsettled.TransId;
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache(thisTp, unsettled.Account);
                            var tpCurrency = thirdPartyBLL.GetTpCurrency(thisTp, tpCustInfo.CurrencyId);
                            if (tpCurrency.ExchangeRate > 1)
                            {
                                unsettled.Stake *= tpCurrency.ExchangeRate;
                                unsettled.ValidBet *= tpCurrency.ExchangeRate;
                                unsettled.WinLostAmount *= tpCurrency.ExchangeRate;
                            }

                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = thisTp,
                                Stake = unsettled.Stake,
                                ValidBet = unsettled.ValidBet,
                                WinlostAmount = unsettled.WinLostAmount,
                                TransId = unsettled.TransId,
                                GameType = (GameType)gameType,
                                TpUserName = unsettled.Account,
                                SettledTime = unsettled.SettleTime,
                                SettlementType = SettlementType.SettledForValid,
                                ReferenceNo = unsettled.schemeid // for smart bet mapping
                            };

                            var result = settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.LiveCasino);

                            settledBLL.SetSettleFlag_TpSmartWagerLC_N2Live(TransId);

                        }
                        catch (Exception ex1)
                        {
                            LogContext.PushProperty("TP", tpName);
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", logName);
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

                timerSettleN2LiveLC_Smart.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePTLCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.PT;
            var gameType = GameType.PT_LiveCasino;
            var category = TpCategory.LiveCasino;
            string logName = "Settle_PT_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_PT();

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
                                SettlementType = SettlementType.SettledForValid
                            };


                            if (stake == 0 && validBet == 0 && winLostAmount == 0)
                            {
                                settledBLL.SetSettleFlag_TpWagerLC_PT(long.Parse(unsettled.TransId));
                            }
                            else
                            {
                                settledBLL.Settled(tpCustInfo, betRecordInfo, PromotionCategory.PTLiveCasino);
                                settledBLL.SetSettleFlag_TpWagerLC_PT(long.Parse(unsettled.TransId));
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

                timerSettlePTLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoSettlePPLCWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            int thisTp = (int)Tp.PP;
            var gameType = GameType.PP_LiveCasino;
            string logName = "Settle_PP_LC";
            var tpName = Enum.GetName(typeof(Tp), thisTp);

            try
            {
                var unsettledList = settledBLL.GetUnsettledListLC_PP();

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

                            if (winLostAmount >= 0 && winLostAmount < stake)
                            {
                                validBet = winLostAmount;
                            }
                            else if (winLostAmount < 0)
                            {
                                validBet = Math.Abs(winLostAmount);
                            }

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
                                SettlementType = SettlementType.SettledForValid,
                                TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                            };

                            if (betRecordInfo.WinlostAmount == 0)
                            {
                                betRecordInfo.TicketStatus = TicketStatus.Draw;
                            }

                            // PP LC 原本已存SLOTS 故使用TpCategory.Slots
                            settledBLL.Settled(tpCustInfo, betRecordInfo, TpCategory.Slots);
                            settledBLL.SetSettleFlag_BettransLC_PP(unsettled.TransId, unsettled.Account);
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

                timerSettlePPLC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        #endregion

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerSettleAG?.Change(Timeout.Infinite, 0);
            timerSettleDG?.Change(Timeout.Infinite, 0);
            timerSettleAllbet?.Change(Timeout.Infinite, 0);
            timerSettleSA?.Change(Timeout.Infinite, 0);
            timerSettleEBet?.Change(Timeout.Infinite, 0);
            timerSettleWM?.Change(Timeout.Infinite, 0);
            timerSettleCQ9?.Change(Timeout.Infinite, 0);
            timerSettleCQ9Fish?.Change(Timeout.Infinite, 0);
            timerSettleGPI?.Change(Timeout.Infinite, 0);
            timerSettleSGRng?.Change(Timeout.Infinite, 0);
            timerSettleSGFish?.Change(Timeout.Infinite, 0);
            timerSettleAWC?.Change(Timeout.Infinite, 0);
            timerSettlePG?.Change(Timeout.Infinite, 0);
            timerSettleKiss918?.Change(Timeout.Infinite, 0);
            timerSettleOne789LT?.Change(Timeout.Infinite, 0);
            timerSettleOne789LT_789?.Change(Timeout.Infinite, 0);
            timerSettleOne789LC?.Change(Timeout.Infinite, 0);
            timerSettleMega?.Change(Timeout.Infinite, 0);
            timerSettleIconicRng?.Change(Timeout.Infinite, 0);
            timerSettleIconicFish?.Change(Timeout.Infinite, 0);
            timerSettleOneGaming?.Change(Timeout.Infinite, 0);
            timerSettleV8?.Change(Timeout.Infinite, 0);
            timerSettleYGG?.Change(Timeout.Infinite, 0);
            timerSettleYLFish?.Change(Timeout.Infinite, 0);
            timerSettleN2Live?.Change(Timeout.Infinite, 0);
            timerSettleN2LiveLC_Smart?.Change(Timeout.Infinite, 0);
            timerSettlePTRng?.Change(Timeout.Infinite, 0);
            timerSettlePTLC?.Change(Timeout.Infinite, 0);
            timerSettleBTI?.Change(Timeout.Infinite, 0);
            timerSettlePPRng?.Change(Timeout.Infinite, 0);
            timerSettlePPLC?.Change(Timeout.Infinite, 0);
            timerSettle9W?.Change(Timeout.Infinite, 0);
            timerSettleKM?.Change(Timeout.Infinite, 0);
            timerSettleAMB?.Change(Timeout.Infinite, 0);
            timerSettleJILIRng?.Change(Timeout.Infinite, 0);
            timerSettleJILIFish?.Change(Timeout.Infinite, 0);
            timerSettleMG?.Change(Timeout.Infinite, 0);
            timerSettleTCGSEA?.Change(Timeout.Infinite, 0);
            timerSettleTCGVN?.Change(Timeout.Infinite, 0);
            timerSettleMP?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
