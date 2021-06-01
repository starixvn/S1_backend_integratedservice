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
    public class WagerAGBLL
    {

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAGService wagerAGService;
        private readonly ApiAGBLL apiAGBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerAGBLL> logger;

        public WagerAGBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerAGService wagerAGService,
            ApiAGBLL apiAGBLL,
            Log2File log2File,
            ILogger<WagerAGBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAGService = wagerAGService;
            this.apiAGBLL = apiAGBLL;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.AG.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

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
                    var dataWager = await apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime, page, guid);

                    // error condition
                    var errorCode = dataWager.result.info;
                    var errorMsg = dataWager.result.Code ?? "";

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    var totalNum = dataWager.result.addition.total;
                    totalPage = dataWager.result.addition.totalpage;
                    if (totalNum == 0)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        return true;
                    }

                    foreach (var bet in dataWager.result.row)
                    {
                        try
                        {
                            wagerAGService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {bet.billNo.ToString()} )";
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
