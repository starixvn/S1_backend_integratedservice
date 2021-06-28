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

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class CF_SettleWorker : BackgroundService
    {
        private Timer timerSettle;
        private Timer timerPayout;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IConfiguration configuration;

        public CF_SettleWorker(
            IConfiguration configuration,
            SettledBLL settledBLL,
            Log2File log2File
        )
        {
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.configuration = configuration;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timerSettle = new Timer(DoSettleWork, null, TimeSpan.Zero, TimeSpan.Zero);
            timerPayout = new Timer(DoPayoutWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private void DoSettleWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            try
            {
                var unSettleList = settledBLL.GetUnSettleList_CF();

                if (unSettleList != null)
                {
                    forContinue = unSettleList.Count() == 500;
                    foreach (var unsettled in unSettleList)
                    {
                        try
                        {
                            settledBLL.Settled_CF(unsettled, out _);
                        }
                        catch (Exception ex1)
                        {
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "CF_SettleWorker", ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "CF_SettleWorker", ex);
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

                timerSettle.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        private void DoPayoutWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            try
            {
                var unPayoutList = settledBLL.GetUnPayoutList_CF();

                if (unPayoutList != null)
                {
                    forContinue = unPayoutList.Count() == 500;
                    foreach (var unpay in unPayoutList)
                    {
                        try
                        {
                            settledBLL.Payout_CF(unpay, out _);
                        }
                        catch (Exception ex1)
                        {
                            log2File.Error($"{JsonConvert.SerializeObject(unpay)}\r\n{ex1.ToString()}", "CF_SettleWorker", ex1);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "CF_PayoutWorker", ex);
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

                timerPayout.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            return Task.CompletedTask;
        }
    }
}
