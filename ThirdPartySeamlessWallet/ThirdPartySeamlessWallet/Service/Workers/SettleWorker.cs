using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public partial class SettleWorker : BackgroundService
    {
        private Timer timerSettleAG;
        private Timer timerSettleDG;
        private Timer timerSettleAllbet;
        private Timer timerSettleSA;
        private Timer timerSettleEBet;
        private Timer timerSettleWM;
        private Timer timerSettleCQ9;
        private Timer timerSettleGPI;
        private Timer timerSettleSGRng;
        private Timer timerSettleSGFish;
        private Timer timerSettleAWC;
        private Timer timerSettlePG;
        private Timer timerSettleKiss918;
        private Timer timerSettleOne789LT;
        private Timer timerSettleOne789LT_789;
        private Timer timerSettleOne789LC;
        private Timer timerSettleMega;
        private readonly IConfiguration configuration;
        private readonly IHostEnvironment environment;
        private readonly Log2File log2File;
        private readonly SettledBLL settledBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;

        public SettleWorker(IConfiguration configuration, IHostEnvironment environment, Log2File log2File, ThirdPartyBLL thirdPartyBLL, SettledBLL settledBLL, TpCustInfoBLL tpCustInfoBLL)
        {
            this.configuration = configuration;
            this.environment = environment;
            this.log2File = log2File;
            this.settledBLL = settledBLL;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
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
            timerSettleGPI = new Timer(DoSettleGPIWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSGRng = new Timer(DoSettleSGRngWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleSGFish = new Timer(DoSettleSGFishWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleAWC = new Timer(DoSettleAWCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettlePG = new Timer(DoSettlePGWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerSettleKiss918 = new Timer(DoSettleKiss918Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT = new Timer(DoSettleOne789LTWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LT_789 = new Timer(DoSettleOne789LT_789Work, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleOne789LC = new Timer(DoSettleOne789LCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerSettleMega = new Timer(DoSettleMegaWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoSettleAGWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
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

                                        var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)Tp.AG, username);
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
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "SettleAG");
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
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Username);
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
                        }
                        catch (Exception ex1)
                        {
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleDG");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                            var result = settledBLL.Get_TpWagerAllbet_Settle(unsettled.BetNum, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime);

                            if (result > 0)
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.Client);
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
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleAllbet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                            var result = settledBLL.Get_TpWagerSA_Settle(unsettled.gameid, unsettled.username, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime);

                            if (result > 0)
                            {
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.username);
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
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleSA");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                            var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, unsettled.username);

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
                            isError = true;
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleEBet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, wager.Account);
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
                                isError = true;
                                log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "SettleWM");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
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
                                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo(thisTp, wager.Account);
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

                timerSettleOne789LC.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
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
            return Task.CompletedTask;
        }
    }
}
