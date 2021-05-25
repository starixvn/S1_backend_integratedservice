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
    public class WagerSGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerSGService wagerSGService;
        private readonly ApiSGBLL apiSGBLL;
        private readonly ILogger<WagerSGBLL> logger;
        private readonly Log2File log2File;

        public WagerSGBLL(
            ILogger<WagerSGBLL> logger,
            WagerSGService wagerSGService,
            ApiSGBLL apiSGBLL,
            ThirdPartyBLL thirdPartyBLL,
            Log2File log2File
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerSGService = wagerSGService;
            this.apiSGBLL = apiSGBLL;
            this.logger = logger;
            this.log2File = log2File;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.SG.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("TpId", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // pagination
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await apiSGBLL.GetBetListByTime(setting["ApiUrl"], setting["merchantCode"], startTime, endTime, page, guid);

                    // error condition
                    var errorCode = dataWager.code;
                    var errorMsg = dataWager.msg;

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    var totalNum = dataWager.resultCount;
                    totalPage = dataWager.pageCount;
                    if (totalNum == 0)
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
                            if (!bet.gameCode.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            wagerSGService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.ticketId} )";
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
