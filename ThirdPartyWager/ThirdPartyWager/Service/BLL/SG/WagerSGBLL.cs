using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSGBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerSGService wagerSGService;
        private readonly ApiSGBLL apiSGBLL;
        private readonly ILogger<WagerSGBLL> logger;

        public WagerSGBLL(
            ILogger<WagerSGBLL> logger,
            WagerSGService wagerSGService,
            ApiSGBLL apiSGBLL,
            ThirdPartyBLL thirdPartyBLL
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerSGService = wagerSGService;
            this.apiSGBLL = apiSGBLL;
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
                    var dataWager = await apiSGBLL.GetBetListByTime(setting["ApiUrl"], setting["merchantCode"], startTime, endTime, page);

                    // error condition
                    var errorCode = dataWager.code;
                    var errorMsg = dataWager.msg;

                    if (errorCode != 0)
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        return false;
                    }

                    var totalNum = dataWager.resultCount;
                    totalPage = dataWager.pageCount;
                    if (totalNum == 0)
                    {
                        //logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} PAGE: {page} )");
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
                            var wagerId = bet.ticketId.ToString();
                            logger.LogError(ex, ex.Message);
                            logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
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
