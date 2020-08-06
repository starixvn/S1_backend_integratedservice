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
    public class PP_EndRoundWorkerService : BackgroundService
    {
        private Timer timer;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly IConfiguration configuration;

        public PP_EndRoundWorkerService(IConfiguration configuration, SettledBLL settledBLL, Log2File log2File)
        {
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.configuration = configuration;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (configuration.GetValue<bool>("RunWorkerService"))
                timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        private void DoWork(object state)
        {
            bool forContinue = false;
            bool isError = false;
            try
            {
                var unsettledList = settledBLL.GetUnsettledListRNG_PP();

                if (unsettledList != null)
                {
                    forContinue = unsettledList.Count() == 500;
                    foreach (var unsettled in unsettledList)
                    {
                        try
                        {
                            settledBLL.SettledRNG_PP(unsettled, out _);
                        }
                        catch (Exception ex1)
                        {
                            log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "PP_EndRoundWorkerService");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log2File.Error(ex.ToString(), "PP_EndRoundWorkerService");
                isError = true;
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(30);

                if (forContinue)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(1);
                }
                else if (isError)
                {
                    dueTimeSpan = TimeSpan.FromMinutes(1);
                }

                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            return Task.CompletedTask;
        }
    }
}
