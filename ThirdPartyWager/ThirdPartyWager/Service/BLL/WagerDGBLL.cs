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
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerDGBLL
    {
        public IEnumerable Result { get; private set; }

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly ILogger<WagerDGBLL> logger;

        public WagerDGBLL(ILogger<WagerDGBLL> logger, WagerDGService wagerDGService, ApiDGBLL apiDGBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.logger = logger;
        }
        public async Task<bool> GetWagersAsync()
        {
            // get dg api info
            try
            {
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);

                List<long> wagerIds = new List<long>();
                // get wagers
                var dataWager = await apiDGBLL.GetBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"]);

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
                //await apiDGBLL.MarkGotBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"], wagerIds);
                return dataWager.list.Count() == 1000;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }
        }

    }
}
