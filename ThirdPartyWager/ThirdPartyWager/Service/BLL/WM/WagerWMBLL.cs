using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerWMBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerWMService wagerWMService;
        private readonly ApiWMBLL apiWMBLL;
        private readonly ILogger<WagerWMBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerWMBLL(
            ILogger<WagerWMBLL> logger,
            WagerWMService wagerWMService,
            ApiWMBLL apiWMBLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerWMService = wagerWMService;
            this.apiWMBLL = apiWMBLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
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
                    currencyList.Add("IDRK");
                    currencyList.Add("VNDK");
                    currencyList.Add("USD");
                }

                foreach (var curr in currencyList)
                {
                    // get wager
                    var dataWager = await apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{curr}"], setting[$"signature_{curr}"], startTime, endTime);

                    // error condition
                    var errorCode = dataWager.errorCode;
                    var errorMsg = dataWager.errorMessage;
                    
                    if (errorCode == 107)
                    {
                        //logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {curr} )");
                        continue;
                    }

                    if (errorCode != 0 )
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        return false;
                    }

                    
                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            bet.Currency = curr;
                            wagerWMService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            var wagerId = bet.betId;
                            logger.LogError(ex, ex.Message);
                            logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
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
