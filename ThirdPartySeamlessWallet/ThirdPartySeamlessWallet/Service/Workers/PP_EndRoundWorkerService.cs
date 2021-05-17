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
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Workers
{
    public class PP_EndRoundWorkerService : BackgroundService
    {
        private Timer timer;
        private Timer timerPPLC;
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
            //timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.Zero);
            //timerPPLC = new Timer(DoPPLCWork, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }
        //private void DoWork(object state)
        //{
        //    bool forContinue = false;
        //    bool isError = false;
        //    var gameType = GameType.PP_RNG;
        //    try
        //    {
        //        var unsettledList = settledBLL.GetUnsettledListRNG_PP((int)gameType);

        //        if (unsettledList != null)
        //        {
        //            forContinue = unsettledList.Count() == 500;
        //            foreach (var unsettled in unsettledList)
        //            {
        //                try
        //                {
        //                    settledBLL.SettledRNG_PP(unsettled, (int)gameType, out _);
        //                }
        //                catch (Exception ex1)
        //                {
        //                    log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "PP_RNG_EndRoundWorkerService");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log2File.Error(ex.ToString(), "PP_RNG_EndRoundWorkerService");
        //        isError = true;
        //    }
        //    finally
        //    {
        //        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(3);

        //        if (forContinue)
        //        {
        //            dueTimeSpan = TimeSpan.FromSeconds(1);
        //        }
        //        else if (isError)
        //        {
        //            dueTimeSpan = TimeSpan.FromMinutes(1);
        //        }

        //        timer.Change(dueTimeSpan, TimeSpan.Zero);
        //    }
        //}
        //private void DoPPLCWork(object state)
        //{
        //    bool forContinue = false;
        //    bool isError = false;
        //    var gameType = GameType.PP_LiveCasino;

        //    try
        //    {
        //        var unsettledList = settledBLL.GetUnsettledListRNG_PP((int)gameType);

        //        if (unsettledList != null)
        //        {
        //            forContinue = unsettledList.Count() == 500;
        //            foreach (var unsettled in unsettledList)
        //            {
        //                try
        //                {
        //                    settledBLL.SettledRNG_PP(unsettled, (int)gameType, out _);
        //                }
        //                catch (Exception ex1)
        //                {
        //                    log2File.Error($"{JsonConvert.SerializeObject(unsettled)}\r\n{ex1.ToString()}", "PP_LC_EndRoundWorkerService");
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        log2File.Error(ex.ToString(), "PP_LC_EndRoundWorkerService");
        //        isError = true;
        //    }
        //    finally
        //    {
        //        TimeSpan dueTimeSpan = TimeSpan.FromMinutes(3);

        //        if (forContinue)
        //        {
        //            dueTimeSpan = TimeSpan.FromSeconds(1);
        //        }
        //        else if (isError)
        //        {
        //            dueTimeSpan = TimeSpan.FromMinutes(1);
        //        }

        //        timerPPLC.Change(dueTimeSpan, TimeSpan.Zero);
        //    }
        //}
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            return Task.CompletedTask;
        }
    }
}
