using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerIconicBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerIconicService wagerIconicService;
        private readonly ApiIconicBLL apiIconicBLL;
        private readonly ILogger<WagerIconicBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerIconicBLL(
            ILogger<WagerIconicBLL> logger,
            WagerIconicService wagerIconicService,
            ApiIconicBLL apiIconicBLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerIconicService = wagerIconicService;
            this.apiIconicBLL = apiIconicBLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, long startUnix, long endUnix, string currency)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                int perPage = 200;
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await apiIconicBLL.GetBetListByTime(setting["ApiUrl"], setting[$"token_{currency}"], startUnix, endUnix, page, perPage);

                    // error condition
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.error.status;
                        var errorMsg = dataWager.error.message;

                        if (errorCode != 0)
                        {
                            logger.LogError(errorCode, errorMsg);
                            logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startUnix} END: {endUnix}  CURRENCY: {currency} )");
                            return false;
                        }
                    }

                    if (dataWager.totalSize == 0)
                    {
                        logger.LogInformation($"NO_DATA ( START: {startUnix} END: {endUnix} CURRENCY: {currency} )");
                        return true;
                    }

                    totalPage = (int)((dataWager.totalSize / perPage) + 1);
                    foreach (var bet in dataWager.data)
                    {
                        try
                        {
                            if (bet.gameType != "fish")
                            {
                                continue;
                            }
                            if (bet.status == "playing")
                            {
                                continue;
                            }
                            wagerIconicService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            var wagerId = bet.id;
                            logger.LogError(ex, ex.Message);
                            logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                            return false;
                        }
                    }
                    page++;
                }
                logger.LogInformation($"FINISH ( START: {startUnix} END: {endUnix} CURRENCY: {currency} )");
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
