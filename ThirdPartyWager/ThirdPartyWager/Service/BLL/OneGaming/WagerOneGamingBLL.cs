using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOneGamingBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerOneGamingService wagerOneGamingService;
        private readonly ApiOneGamingBLL apiOneGamingBLL;
        private readonly ILogger<WagerOneGamingBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerOneGamingBLL(
            ILogger<WagerOneGamingBLL> logger,
            WagerOneGamingService wagerOneGamingService,
            ApiOneGamingBLL apiOneGamingBLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerOneGamingService = wagerOneGamingService;
            this.apiOneGamingBLL = apiOneGamingBLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await apiOneGamingBLL.GetBetListByTime(setting["ApiUrl"], setting[$"agent_code_{currency}"], setting[$"secret_key_{currency}"], startTime, endTime);

                // error condition
                if (dataWager.status_code != "0")
                {
                    var errorCode = dataWager.error_code;
                    var errorMsg = dataWager.message;

                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime}  CURRENCY: {currency} )");
                    return false;
                }

                if (dataWager.data.Count == 0)
                {
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
                    return true;
                }

                foreach (var bet in dataWager.data)
                {
                    try
                    {
                        wagerOneGamingService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        var wagerId = bet.ticket;
                        logger.LogError(ex, ex.Message);
                        logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                        return false;
                    }
                }
                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
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
