using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoWMBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerWMService wagerWMService;
        private readonly ApiWMBLL apiWMBLL;
        private readonly ILogger<WagerRedoWMBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoWMBLL(
            ILogger<WagerRedoWMBLL> logger,
            ThirdPartyBLL thirdPartyBLL,
            WagerWMService wagerWMService,
            ApiWMBLL apiWMBLL,
            IHostEnvironment environment
        )
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerWMService = wagerWMService;
            this.apiWMBLL = apiWMBLL;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                var currencyList = new List<string>();
                currencyList.Add("RMB");
                if (environment.IsProduction())
                {
                    currencyList.Add("THB");
                    currencyList.Add("MYR");
                    currencyList.Add("IDR");
                    currencyList.Add("VND");
                }

                foreach (var curr in currencyList)
                {
                    // count interval wagers
                    var getWagerCount = wagerWMService.GetWagerTotal(startUnix, endUnix, curr);
                    int wagerCount = getWagerCount.wagerCount;

                    var dataWager = await apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{curr}"], setting[$"signature_{curr}"], startTime, endTime);

                    // error condition
                    var errorCode = dataWager.errorCode;
                    var errorMsg = dataWager.errorMessage;
                    if (errorCode == 107)
                    {
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {curr} )");
                        continue;
                    }

                    if (errorCode != 0)
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        return false;
                    }

                    // db wager count equals api bet count do nothing
                    var totalNum = dataWager.result.Count();
                    if (totalNum == wagerCount)
                    {
                        logger.LogInformation($"MATCH ( START: {startTime} END: {endTime} TOTAL: {totalNum} CURRENCY: {curr} )");
                        continue;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            bet.Currency = curr;
                            wagerWMService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                            continue;
                        }
                    }

                    logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} CURRENCY: {curr} )");
                }
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
