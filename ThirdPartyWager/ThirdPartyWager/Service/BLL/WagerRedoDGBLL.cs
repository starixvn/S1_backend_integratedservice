using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoDGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly ILogger<WagerRedoDGBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoDGBLL(IHostEnvironment environment, ILogger<WagerRedoDGBLL> logger, ThirdPartyBLL thirdPartyBLL, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.logger = logger;
            this.environment = environment;
        }
        public async Task<bool> RedoWagersAsync()
        {
            try
            {
                // get dg api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);

                // define time
                DateTime current = DateTime.Now;
                DateTime timeBase = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0);
                string beginTime = timeBase.AddHours(-6).ToString("yyyy-MM-dd HH:00:00");
                string endTime = timeBase.AddHours(-5).ToString("yyyy-MM-dd  HH:59:59");
                int pageNow = 1;

                // get wagers
                var dataWager = await apiDGBLL.GetBetListByTime(setting["WagerUrl"], setting["AgentAccount"], setting["APIkey"], beginTime, endTime, pageNow);
               
                // insert wager to db
                //foreach (var bet in dataWager.data.records)
                //{
                //    Console.WriteLine(bet);
                //    Console.WriteLine("=====");
                //    try
                //    {
                //        wagerDGService.InsertWagerDG(bet);
                //    }
                //    catch (Exception ex)
                //    {
                //        logger.LogError(ex, ex.Message);
                //        continue;
                //    }
                //}
                return true;
                //return dataWager.list.Count() == 1000;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }

        }
    }
}
