using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers
{
    public class JwtIconic : BackgroundService
    {
        private readonly ILogger<JwtIconic> logger;
        private readonly ApiIconicBLL apiIconicBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private Timer timer;
        private int tpId = (int)Tp.Iconic;


        public JwtIconic(
            ILogger<JwtIconic> logger,
            ApiIconicBLL apiIconicBLL,
            ThirdPartyBLL thirdPartyBLL
        )
        {
            this.logger = logger;
            this.apiIconicBLL = apiIconicBLL;
            this.thirdPartyBLL = thirdPartyBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            timer = new Timer(DoTaskJWT, null, TimeSpan.Zero, TimeSpan.Zero);
            return Task.CompletedTask;
        }

        private async void DoTaskJWT(object state)
        {
            try
            {
                // get setting
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // call jwt
                var currencyList = new List<string>();
                currencyList.Add("THB");
                currencyList.Add("MYR");
                currencyList.Add("IDR");
                currencyList.Add("VND");
                currencyList.Add("CNY");
                currencyList.Add("USDT");
                foreach (var curr in currencyList)
                {
                    var getJwt = await apiIconicBLL.GetJwtToken(setting["ApiUrl"], $"novax{curr}", setting["password"]);
                    if (getJwt.token != null)
                    {
                        setting[$"token_{curr}"] = getJwt.token;
                    }
                }

                //update setting
                var result = thirdPartyBLL.SetThirdPartySetting(tpId, setting);
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromDays(7);
                timer.Change(dueTimeSpan, TimeSpan.Zero);
            }
        }


        public override Task StopAsync(CancellationToken cancellationToken)
        {
            //WriteToFile("WriteToFileHostedService: Process Stopped");
            timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
