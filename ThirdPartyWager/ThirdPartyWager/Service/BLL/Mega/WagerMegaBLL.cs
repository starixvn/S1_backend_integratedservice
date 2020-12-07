using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{

    public class WagerMegaBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerMegaService wagerMegaService;
        private readonly ApiMegaBLL apiMegaBLL;
        private readonly ILogger<WagerMegaBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerMegaBLL(
            ILogger<WagerMegaBLL> logger,
            WagerMegaService wagerMegaService,
            ApiMegaBLL apiMegaBLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerMegaService = wagerMegaService;
            this.apiMegaBLL = apiMegaBLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, string currency)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // get report player
                var dataReportPlayer = await apiMegaBLL.GetReportPlayerListByTime(setting["ApiUrl"], setting["sn"], setting[$"secretCode_{currency}"], setting[$"agentLoginId_{currency}"], startTime, endTime);

                if (dataReportPlayer.code != null)
                {
                    logger.LogError("", dataReportPlayer.message);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime}  CURRENCY: {currency} )");
                    return false;
                }

                if (/*dataReportPlayer.success == true &&*/ dataReportPlayer.result.Count() == 0)
                {
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
                    return true;
                }

                //int perPage = 100;
                foreach (var reportList in dataReportPlayer.result)
                {
                    try
                    {
                        reportList.win = reportList.win * -1;
                        reportList.Currency = currency;
                        wagerMegaService.InsertWager(reportList);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                        logger.LogInformation($"INSERT_WAGER ( START: {startTime} END: {endTime} CURRENCY: {currency} loginId: {reportList.loginId} )");
                        return false;
                    }

                    //int page = 1;
                    //int totalPage = 1;
                    //var Account = reportList.loginId;
                    //while (page <= totalPage)
                    //{
                    //    // get wager
                    //    var dataWager = await apiMegaBLL.GetBetListByTime(setting["ApiUrl"], setting["sn"], setting[$"secretCode_{currency}"], Account, startTime, endTime, page, perPage);

                    //    // error condition
                    //    var errorCode = dataWager.code;
                    //    var errorMsg = dataWager.message;

                    //    if (errorCode != null)
                    //    {
                    //        logger.LogError(errorCode, errorMsg);
                    //        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime}  CURRENCY: {currency} )");
                    //        return false;
                    //    }

                    //    if (/*errorCode == 0 &&*/ dataWager.total == 0)
                    //    {
                    //        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
                    //        break;
                    //    }

                    //    totalPage = (int)((dataWager.total / perPage) + 1);
                    //    foreach (var bet in dataWager.result)
                    //    {
                    //        try
                    //        {
                    //            //if (bet.LogDataType != 2)
                    //            //{
                    //            //    continue;
                    //            //}
                    //            bet.Account = Account;
                    //            bet.Currency = currency;
                    //            wagerMegaService.InsertWager(bet);
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            var wagerId = bet.uuid;
                    //            logger.LogError(ex, ex.Message);
                    //            logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                    //            continue;
                    //        }
                    //    }
                    //    page++;
                    //}
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
