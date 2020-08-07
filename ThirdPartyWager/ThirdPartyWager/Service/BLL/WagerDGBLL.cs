using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerDGBLL
    {
        public IEnumerable Result { get; private set; }

        private readonly ThirdPartyHelper thirdPartyHelper;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly ILogger<WagerDGBLL> logger;

        public WagerDGBLL(ILogger<WagerDGBLL> logger, ThirdPartyHelper thirdPartyHelper, WagerDGService wagerDGService, ApiDGBLL apiDGBLL)
        {
            this.thirdPartyHelper = thirdPartyHelper;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.logger = logger;
        }

        public async Task GetWagersAsync()
        {
            // get dg api info
            int tpId = 4;
            try
            {
                var dataTpList = thirdPartyHelper.GetThirdPartySetting(tpId);
                var setting = JsonConvert.DeserializeObject<Setting>(dataTpList.Setting);
                List<long> wagerIds = new List<long>();
                // get wagers
                var dataWager = await apiDGBLL.GetBetList(setting.ApiUrl, setting.AgentAccount, setting.APIkey);

                // insert wager to db
                foreach (var bet in dataWager.list)
                {
                    try
                    {
                        wagerDGService.InsertWagerDG(bet);
                        wagerIds.Add(bet.id);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                        continue;
                    }
                }
                // mark wager
                await apiDGBLL.MarkGotBetList(setting.ApiUrl, setting.AgentAccount, setting.APIkey, wagerIds);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
            }
        }

    }
}
