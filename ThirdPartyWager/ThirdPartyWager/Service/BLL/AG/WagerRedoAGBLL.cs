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
    public class WagerRedoAGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAGService wagerAGService;
        private readonly ApiAGBLL apiAGBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoAGBLL> logger;

        public WagerRedoAGBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerAGService wagerAGService,
            ApiAGBLL apiAGBLL,
            Log2File log2File,
            ILogger<WagerRedoAGBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAGService = wagerAGService;
            this.apiAGBLL = apiAGBLL;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.AG.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = wagerAGService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await apiAGBLL.GetBetSummaryByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime, guid);
                var arrSummary = getWagerSummary.result;

                // error condition
                var errorCode = arrSummary.info;
                var errorMsg = arrSummary.Code ?? "";
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary));
                    return false;
                }

                if (errorCode == 0 && arrSummary.row == null)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary), "Info");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in arrSummary.row)
                {
                    totalNum += data.wagersTotal;
                }
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary), "Info");
                    return true;
                }

                // parse 10 minute
                var st = Convert.ToDateTime(startTime);
                var ed = Convert.ToDateTime(startTime).AddMinutes(10).AddMilliseconds(-1);
                var bg = "";
                var fn = "";
                for (int i = 1; i <= 6; i++)
                {
                    int page = 1;
                    int totalPage = 1;
                    if (i > 1)
                    {
                        st = st.AddMinutes(10);
                        ed = ed.AddMinutes(10);
                    }
                    bg = st.ToString("yyyy-MM-dd HH:mm:ss");
                    fn = ed.ToString("yyyy-MM-dd HH:mm:ss");
                    while (page <= totalPage)
                    {
                        var dataWager = await apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], bg, fn, page, guid);
                        var arrWager = dataWager.result;

                        // error condition
                        errorCode = arrWager.info;
                        errorMsg = arrWager.Code ?? "";
                        if (errorCode != 0)
                        {
                            msg = "THIRD_PARTY_ERROR";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                            return false;
                        }

                        // pagination
                        var pageInfo = arrWager.addition;
                        var totalNo = pageInfo.total;
                        totalPage = pageInfo.totalpage;
                        if (totalNo == 0)
                        {
                            msg = "NO_DATA";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                            continue;
                        }

                        foreach (var bet in arrWager.row)
                        {
                            try
                            {
                                wagerAGService.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                msg = $"INSERT_WAGER(WAGERID: {bet.billNo.ToString()} )";
                                log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                                return false;
                                //continue;
                            }
                        }
                        page++;
                    }
                }

                msg = "FINISH_REDO";
                log2File.AddResultLog(logName, guid, msg, null, "Error");
                return false;

            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }
    }
}
