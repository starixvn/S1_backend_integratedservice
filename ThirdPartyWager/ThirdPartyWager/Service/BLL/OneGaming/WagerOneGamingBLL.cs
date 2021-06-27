using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOneGamingBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerOneGamingService wagerOneGamingService;
        private readonly ApiOneGamingBLL apiOneGamingBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerOneGamingBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerOneGamingBLL(
            ILogger<WagerOneGamingBLL> logger,
            WagerOneGamingService wagerOneGamingService,
            ApiOneGamingBLL apiOneGamingBLL,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerOneGamingService = wagerOneGamingService;
            this.apiOneGamingBLL = apiOneGamingBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.OneGaming.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await apiOneGamingBLL.GetBetListByTime(setting["ApiUrl"], setting[$"agent_code_{currency}"], setting[$"secret_key_{currency}"], startTime, endTime, guid);

                // error condition
                if (dataWager.status_code != "0")
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }

                if (dataWager.data.Count == 0)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                foreach (var bet in dataWager.data)
                {
                    try
                    {
                        wagerOneGamingService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.ticket} )";
                        logger.LogError(ex, $"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                        return false;
                    }
                }

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                logger.LogError(ex, $"MESSAGE: {msg}, EXCEPTION: {ex.Message}");
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }

        //public WagerOneGamingTotal GetWagerTotal(long startUnix, long endUnix)
        //{
        //    return wagerOneGamingService.GetWagerTotal(startUnix, endUnix);
        //}
    }
}
