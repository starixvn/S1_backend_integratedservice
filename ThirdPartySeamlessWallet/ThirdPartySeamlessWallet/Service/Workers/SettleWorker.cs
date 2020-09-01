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
                                    decimal stake = 0;
                                    decimal validBet = 0;
                                    decimal winLostAmount = 0;
                                    long settleTime = 0;

                                    var result = settledBLL.Get_TpWagerAG_Settle(JsonConvert.SerializeObject(billNoList), out stake, out validBet, out winLostAmount, out settleTime);

                                    if (result > 0 && betList.Sum(x => x.Value) == stake)
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

                                        result = settledBLL.Settled(tpCustInfo, betRecordInfo);

                                        Task.Run(() =>
                                        {
                                            settledBLL.SetSettleFlag_TpWagerAG(JsonConvert.SerializeObject(billNoList));
                                            settledBLL.SetSettleFlag_AG(unsettled.TransactionID, JsonConvert.SerializeObject(transactionIDs));
                                        });
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
    }
}
