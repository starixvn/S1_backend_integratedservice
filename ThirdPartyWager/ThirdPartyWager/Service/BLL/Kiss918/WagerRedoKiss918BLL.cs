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
    public class WagerRedoKiss918BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerKiss918Service wagerKiss918Service;
        private readonly ApiKiss918BLL apiKiss918BLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoKiss918BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoKiss918BLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerKiss918Service wagerKiss918Service,
            ApiKiss918BLL apiKiss918BLL,
            Log2File log2File,
            ILogger<WagerRedoKiss918BLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerKiss918Service = wagerKiss918Service;
            this.apiKiss918BLL = apiKiss918BLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix, string currency)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.Kiss918.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("TpId", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                DateTime getReportStartTime = DateTime.Parse(startTime);
                string ReportStartTime = getReportStartTime.ToString("yyyy-MM-dd");
                DateTime getReportEndTime = DateTime.Parse(endTime);
                string ReportEndTime = getReportEndTime.ToString("yyyy-MM-dd");

                // get report player
                var dataReportPlayer = await apiKiss918BLL.GetReportPlayerListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], ReportStartTime, ReportEndTime, guid);

                if (!dataReportPlayer.success)
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataReportPlayer)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataReportPlayer));
                    return false;
                }

                if (dataReportPlayer.success && dataReportPlayer.results.Count() == 0)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataReportPlayer), "Info");
                    return true;
                }

                //count interval wagers
                var getWagerCount = wagerKiss918Service.GetWagerTotal(startUnix, endUnix, currency);
                Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
                foreach (var wagerItem in getWagerCount)
                {
                    keyValuePairs.Add(wagerItem.Account, wagerItem.wagerCount);
                }

                int perPage = 100;
                foreach (var reportList in dataReportPlayer.results)
                {
                    int page = 1;
                    int totalPage = 1;
                    var Account = reportList.Account;
                    while (page <= totalPage)
                    {
                        // get wager
                        var dataWager = await apiKiss918BLL.GetBetListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], Account, startTime, endTime, page, perPage, guid);

                        // error condition
                        var errorCode = dataWager.code;
                        if (errorCode == 0 && dataWager.total == 0)
                        {
                            msg = "NO_DATA";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                            break;
                        }

                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                            return false;
                        }

                        if (keyValuePairs.ContainsKey(Account) != false && dataWager.total == keyValuePairs[Account])
                        {
                            msg = "MATCH";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                            break;
                        }

                        totalPage = (int)((dataWager.total / perPage) + 1);
                        foreach (var bet in dataWager.results)
                        {
                            try
                            {
                                //if (bet.LogDataType != 2)
                                //{
                                //    continue;
                                //}
                                bet.Currency = currency;
                                bet.Account = Account;
                                wagerKiss918Service.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.uuid} )";
                                logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                                log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                                return false;
                                //continue;
                            }
                        }
                        page++;
                    }
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
