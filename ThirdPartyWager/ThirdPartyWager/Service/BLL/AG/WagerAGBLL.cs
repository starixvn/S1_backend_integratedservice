using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAGBLL
    {

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAGService wagerAGService;
        private readonly ApiAGBLL apiAGBLL;
        private readonly ILogger<WagerAGBLL> logger;

        public WagerAGBLL(
            ILogger<WagerAGBLL> logger,
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

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
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
                return false;
            }
        }
    }
}
