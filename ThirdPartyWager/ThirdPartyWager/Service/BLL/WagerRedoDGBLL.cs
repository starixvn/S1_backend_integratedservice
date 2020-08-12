using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public WagerRedoDGBLL(ILogger<WagerRedoDGBLL> logger, ThirdPartyBLL thirdPartyBLL, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.logger = logger;
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
                //string beginTime = timeBase.AddHours(-4).ToString("yyyy-MM-dd HH:00:00");
                //string endTime = timeBase.AddHours(-3).ToString("yyyy-MM-dd HH:59:59");

                var beginTime = new DateTimeOffset(timeBase.AddHours(-4));
                var endTime = new DateTimeOffset(timeBase.AddHours(-3).AddMilliseconds(-1));

                // count interval wagers
                var getWagerCount = wagerDGService.GetWagerDGTotal(beginTime.ToUnixTimeSeconds(), endTime.ToUnixTimeSeconds());
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    var dataWager = await apiDGBLL.GetBetListByTime(setting["WagerUrl"], setting["AgentAccount"], setting["APIkey"], beginTime.ToOffset(new TimeSpan(8,0,0)).ToString("yyyy-MM-dd HH:mm:ss"), endTime.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss"), page);
                    totalPage = dataWager.data.pageCount;
                    int totalNum = dataWager.data.rowCount;

                    // db wager count equals api bet count do nothing
                    if (totalNum == wagerCount)
                    {
                        break;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.data.records)
                    {
                        try
                        {
                            wagerDGService.InsertWagerDG(bet);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                            continue;
                        }
                    }
                    page++;

                    // sleep 15 sec
                    Thread.Sleep(15000);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }

        }
    }
}
