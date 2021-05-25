using Microsoft.Extensions.Hosting;
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
    public class WagerIconicBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerIconicService wagerIconicService;
        private readonly ApiIconicBLL apiIconicBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerIconicBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerIconicBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerIconicService wagerIconicService,
            ApiIconicBLL apiIconicBLL,
            Log2File log2File,
            ILogger<WagerIconicBLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerIconicService = wagerIconicService;
            this.apiIconicBLL = apiIconicBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, long startUnix, long endUnix, string currency)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.Iconic.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("TpId", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                int perPage = 200;
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await apiIconicBLL.GetBetListByTime(setting["ApiUrl"], setting[$"token_{currency}"], startUnix, endUnix, page, perPage, guid);

                    // error condition
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.error.status;
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                            return false;
                        }
                    }

                    if (dataWager.totalSize == 0)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        return true;
                    }

                    totalPage = (int)((dataWager.totalSize / perPage) + 1);
                    foreach (var bet in dataWager.data)
                    {
                        try
                        {
                            if (bet.gameType != "fish")
                            {
                                continue;
                            }
                            if (bet.status == "playing")
                            {
                                continue;
                            }
                            wagerIconicService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                            logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                logger.LogError($"MESSAGE: {msg}, EXCEPTION: {ex.Message}");
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }
    }
}
