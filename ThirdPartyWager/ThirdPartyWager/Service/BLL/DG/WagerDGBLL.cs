using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerDGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerDGBLL> logger;

        public WagerDGBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerDGService wagerDGService,
            ApiDGBLL apiDGBLL,
            Log2File log2File,
            ILogger<WagerDGBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.log2File = log2File;
            this.logger = logger;
        }
        public async Task<bool> GetWagersAsync()
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.DG.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            // get dg api info
            try
            {
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.DG);

                List<long> wagerIds = new List<long>();
                // get wagers
                var dataWager = await apiDGBLL.GetBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"], guid);

                var errorCode = dataWager.codeId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }

                if (dataWager.list == null || dataWager.list.Count() == 0)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

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
                        msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                        continue;
                    }
                }
                // mark wager
                await apiDGBLL.MarkGotBetList(setting["ApiUrl"], setting["AgentAccount"], setting["APIkey"], wagerIds, guid);

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return dataWager.list.Count() == 1000;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                logger.LogError($"MESSAGE: {msg}, EXCEPTION: {ex.Message}");
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return true;
            }
        }
    }
}
