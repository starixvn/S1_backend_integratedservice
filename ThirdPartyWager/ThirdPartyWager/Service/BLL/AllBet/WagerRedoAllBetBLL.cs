using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoAllBetBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAllBetService wagerAllBetService;
        private readonly ApiAllBetBLL apiAllBetBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoAllBetBLL> logger;

        public WagerRedoAllBetBLL(
            ApiAllBetBLL apiAllBetBLL,
            ThirdPartyBLL thirdPartyBLL,
            WagerAllBetService wagerAllBetService,
            Log2File log2File,
            ILogger<WagerRedoAllBetBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAllBetService = wagerAllBetService;
            this.apiAllBetBLL = apiAllBetBLL;
            this.log2File = log2File;
            this.logger = logger;
        }
        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.Allbet.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = wagerAllBetService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                while (pageRowCnt >= 1)
                {
                    // bet 
                    var dataWager = await apiAllBetBLL.GetBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex, guid);
                    if (dataWager == null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }
                    // error condition
                    var errorCode = dataWager.error_code;
                    if (errorCode != "OK")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    pageRowCnt = dataWager.page.datas.Count();
                    int totalNum = dataWager.page.count;
                    if (pageRowCnt == 0)
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
                    foreach (var bet in dataWager.page.datas)
                    {
                        try
                        {
                            wagerAllBetService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                            logger.LogError(ex, $"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                        }
                    }
                    pageIndex++;
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
