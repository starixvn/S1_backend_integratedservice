using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoAGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAGService wagerAGService;
        private readonly ApiAGBLL apiAGBLL;
        private readonly ILogger<WagerRedoAGBLL> logger;

        public WagerRedoAGBLL(
            ILogger<WagerRedoAGBLL> logger,
            WagerAGService wagerAGService,
            ApiAGBLL apiAGBLL,
            ThirdPartyBLL thirdPartyBLL
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAGService = wagerAGService;
            this.apiAGBLL = apiAGBLL;
            this.logger = logger;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);


                // count interval wagers
                var getWagerCount = wagerAGService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await apiAGBLL.GetBetSummaryByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime);
                var arrSummary = getWagerSummary.result;

                // error condition
                var errorCode = arrSummary.info;
                var errorMsg = arrSummary.Code ?? "";
                if (errorCode != 0)
                {
                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                    return false;
                }
                if (errorCode == 0 && arrSummary.row == null)
                {
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} )");
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
                    logger.LogInformation($"MATCH ( START: {startTime} END: {endTime} TOTAL: {totalNum} )");
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
                        var dataWager = await apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], bg, fn, page);
                        var arrWager = dataWager.result;

                        // error condition
                        errorCode = arrWager.info;
                        errorMsg = arrWager.Code ?? "";
                        if (errorCode != 0)
                        {
                            logger.LogError(errorCode, errorMsg);
                            logger.LogInformation($"THIRD_PARTY_ERROR ( START: {bg} END: {fn} )");
                            return false;
                        }

                        // pagination
                        var pageInfo = arrWager.addition;
                        var totalNo = pageInfo.total;
                        totalPage = pageInfo.totalpage;
                        if (totalNo == 0)
                        {
                            logger.LogInformation($"NO_DATA ( START: {bg} END: {fn} PAGE: {page} )");
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
                                var wagerId = bet.billNo.ToString();
                                logger.LogError(ex, ex.Message);
                                logger.LogError($"INSERT_WAGER ( WAGERID: {wagerId} )");
                                continue;
                            }
                        }
                        logger.LogInformation($"FINISH ( START: {bg} END: {fn} PAGE: {page} )");
                        page++;
                    }
                }

                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} )");
                return true;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return false;
            }
        }
    }
}
