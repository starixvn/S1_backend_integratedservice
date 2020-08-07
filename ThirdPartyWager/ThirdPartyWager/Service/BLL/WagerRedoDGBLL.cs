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

        public WagerRedoDGBLL(ThirdPartyHelper thirdPartyHelper, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
        }
        public async Task<IEnumerable<dynamic>> RedoWagersAsync()
        {
            // get dg api info
            int tpId = 4;
            var dataTpList = thirdPartyHelper.GetThirdPartySetting(tpId);
            var setting = JsonConvert.DeserializeObject<Setting>(dataTpList.Setting);
            string beginTime = "";
            string endTime = "";
            int pageNow = 1;

            // get wagers
            var dataWager = apiDGBLL.GetBetListByTime(setting.WagerUrl, setting.AgentAccount, setting.APIkey, beginTime, endTime, pageNow);
            // insert to db
            var count = wagerDGService.InsertWagerDG(dataWager.list);
           
            return;
        }
    }
}
