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

    public class WagerMegaBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerMegaService wagerMegaService;
        private readonly ApiMegaBLL apiMegaBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerMegaBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerMegaBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerMegaService wagerMegaService,
            ApiMegaBLL apiMegaBLL,
            Log2File log2File,
            ILogger<WagerMegaBLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerMegaService = wagerMegaService;
            this.apiMegaBLL = apiMegaBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.Mega.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // get report player
                var dataReportPlayer = await apiMegaBLL.GetReportPlayerListByTime(setting["ApiUrl"], setting["sn"], setting[$"secretCode_{currency}"], setting[$"agentLoginId_{currency}"], startTime, endTime, guid);

                if (dataReportPlayer.code != null)
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataReportPlayer)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataReportPlayer));
                    return false;
                }

                if (/*dataReportPlayer.success == true &&*/ dataReportPlayer.result.Count() == 0)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataReportPlayer), "Info");
                    return true;
                }

                foreach (var reportList in dataReportPlayer.result)
                {
                    try
                    {
                        reportList.win = reportList.win * -1;
                        reportList.Currency = currency;
                        reportList.WagerDate = Convert.ToDateTime(startTime);
                        wagerMegaService.InsertWager(reportList);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {reportList.loginId} )";
                        logger.LogError(ex, $"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(reportList)}, EXCEPTION: {ex.Message}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(reportList), "Error", ex.Message);
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
    }
}
