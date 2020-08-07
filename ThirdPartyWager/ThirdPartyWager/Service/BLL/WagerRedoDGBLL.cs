using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoDGBLL
    {
        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly ILogger<WagerRedoDGBLL> logger;

        public WagerRedoDGBLL(ILogger<WagerRedoDGBLL> logger, ThirdPartyHelper thirdPartyHelper, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.logger = logger;
        }
        public async Task RedoWagersAsync()
        {
            // get dg api info
            int tpId = 4;
            var dataTpList = thirdPartyHelper.GetThirdPartySetting(tpId);
            var setting = JsonConvert.DeserializeObject<Setting>(dataTpList.Setting);
            string beginTime = "";
            string endTime = "";
            int pageNow = 1;

            // get wagers
            var dataWager = await apiDGBLL.GetBetListByTime(setting.WagerUrl, setting.AgentAccount, setting.APIkey, beginTime, endTime, pageNow);
            // insert wager to db
            foreach (var bet in dataWager.list)
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

            return;
        }
    }
}
