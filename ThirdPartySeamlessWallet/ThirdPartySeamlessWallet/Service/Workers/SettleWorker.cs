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
    public class SettleWorker : BackgroundService
    {
        private Timer timerSettleAG;
        private Timer timerSettleDG;
        private Timer timerSettleAllbet;
        private Timer timerSettleSA;
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
                                    var bBetList = settledBLL.Get_BetList_AG(payout.TransactionIDs);
                                    if (bBetList != null)
                                    {
                                        betList.AddRange(bBetList);
                                    }
                                }

                                if (betList.Sum(x => x.Value) == payoutList.Sum(x => x.ValidBetAmount))
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
                                                SettlementType = SettlementType.SettledForValidNotRollover
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
                                    SettlementType = SettlementType.SettledForValidNotRollover
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
                                    SettlementType = SettlementType.SettledForValidNotRollover
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
                                    SettlementType = SettlementType.SettledForValidNotRollover
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

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timerSettleAG?.Change(Timeout.Infinite, 0);
            timerSettleDG?.Change(Timeout.Infinite, 0);
            timerSettleAllbet?.Change(Timeout.Infinite, 0);
            timerSettleSA?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
