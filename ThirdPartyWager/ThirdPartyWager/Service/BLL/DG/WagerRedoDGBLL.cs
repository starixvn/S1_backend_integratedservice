using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoDGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerDGService wagerDGService;
        private readonly ApiDGBLL apiDGBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoDGBLL> logger;

        public WagerRedoDGBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerDGService wagerDGService,
            ApiDGBLL apiDGBLL,
            Log2File log2File,
            ILogger<WagerRedoDGBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerDGService = wagerDGService;
            this.apiDGBLL = apiDGBLL;
            this.log2File = log2File;
            this.logger = logger;
        }
        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.DG.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get dg api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = wagerDGService.GetWagerDGTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    var dataWager = await apiDGBLL.GetBetListByTime(setting["WagerUrl"], setting["AgentAccount"], setting["APIkey"], startTime, endTime, page, guid);

                    var errorCode = dataWager.codeId;
                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    totalPage = dataWager.data.pageCount;
                    int totalNum = dataWager.data.rowCount;

                    if (errorCode == 0 && totalPage == 0)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        return true;
                    }
                    // db wager count equals api bet count do nothing
                    if (totalNum == wagerCount)
                    {
                        msg = "MATCH";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        return true;
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
                            msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                            logger.LogError(ex, $"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                            //continue;
                        }
                    }
                    page++;

                    // sleep 15 sec
                    Thread.Sleep(15000);
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
    }
}
