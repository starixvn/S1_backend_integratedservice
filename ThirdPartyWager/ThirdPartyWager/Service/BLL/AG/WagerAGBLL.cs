using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAGBLL
    {

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAGService wagerAGService;
        private readonly ApiAGBLL apiAGBLL;
        private readonly ILogger<WagerAGBLL> logger;

        public WagerAGBLL(ILogger<WagerAGBLL> logger, WagerAGService wagerAGService, ApiAGBLL apiAGBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAGService = wagerAGService;
            this.apiAGBLL = apiAGBLL;
            this.logger = logger;
        }

        public async Task<bool> GetWagersAsync()
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.AG);

                // define time
                DateTime current = DateTime.Now;
                DateTime timeBase = new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0);
                var beginAt = new DateTimeOffset(timeBase.AddMinutes(-10));
                var endAt = new DateTimeOffset(timeBase.AddMinutes(-5));
                var startTime = beginAt.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                var endTime = endAt.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");

                // pagination
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    var dataWager = await apiAGBLL.GetBetListByTime(setting["WagerUrl"], setting["HashKey"], setting["data_cagent"], startTime, endTime, page);

                    // error condition
                    var errorCode = dataWager.result.info;
                    var errorMsg = dataWager.result.Code ?? "";

                    if (errorCode != 0)
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        return false;
                    }

                    var totalNum = dataWager.result.addition.total;
                    totalPage = dataWager.result.addition.totalpage;
                    if (totalNum == 0)
                    {
                        //logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} PAGE: {page} )");
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
                            var wagerId = bet.billNo.ToString();
                            logger.LogError(ex, ex.Message);
                            logger.LogError($"INSERT_WAGER ( WAGERID: {wagerId} )");
                            continue;
                        }
                    }
                    page++;
                }
                
                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} )");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }
        }
    }
}
