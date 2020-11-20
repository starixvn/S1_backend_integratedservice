using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoKiss918BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerKiss918Service wagerKiss918Service;
        private readonly ApiKiss918BLL apiKiss918BLL;
        private readonly ILogger<WagerRedoKiss918BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoKiss918BLL(
            ILogger<WagerRedoKiss918BLL> logger,
            WagerKiss918Service wagerKiss918Service,
            ApiKiss918BLL apiKiss918BLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerKiss918Service = wagerKiss918Service;
            this.apiKiss918BLL = apiKiss918BLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix, string currency)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                DateTime getReportStartTime = DateTime.Parse(startTime);
                string ReportStartTime = getReportStartTime.ToString("yyyy-MM-dd");
                DateTime getReportEndTime = DateTime.Parse(endTime);
                string ReportEndTime = getReportEndTime.ToString("yyyy-MM-dd");

                // get report player
                var dataReportPlayer = await apiKiss918BLL.GetReportPlayerListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], ReportStartTime, ReportEndTime);

                if (dataReportPlayer.success != true)
                {
                    logger.LogError("", dataReportPlayer.msg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime}  CURRENCY: {currency} )");
                    return false;
                }

                if (dataReportPlayer.success == true && dataReportPlayer.results.Count() == 0)
                {
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
                    return true;
                }

                //count interval wagers
                var getWagerCount = wagerKiss918Service.GetWagerTotal(startUnix, endUnix, currency);
                Dictionary<string, int> keyValuePairs = new Dictionary<string, int>();
                foreach (var wagerItem in getWagerCount)
                {
                    keyValuePairs.Add(wagerItem.Account, wagerItem.wagerCount);
                }

                int perPage = 100;
                foreach (var reportList in dataReportPlayer.results)
                {
                    int page = 1;
                    int totalPage = 1;
                    var Account = reportList.Account;
                    while (page <= totalPage)
                    {
                        // get wager
                        var dataWager = await apiKiss918BLL.GetBetListByTime(setting["WagerUrl"], setting[$"authcode_{currency}"], setting[$"secretkey_{currency}"], setting[$"userName_{currency}"], Account, startTime, endTime, page, perPage);

                        // error condition
                        var errorCode = dataWager.code;
                        var errorMsg = dataWager.msg;

                        if (errorCode == 0 && dataWager.total == 0)
                        {
                            logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} CURRENCY: {currency} )");
                            break;
                        }

                        if (errorCode != 0)
                        {
                            logger.LogError(errorCode, errorMsg);
                            logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime}  CURRENCY: {currency} )");
                            return false;
                        }

                        if (keyValuePairs.ContainsKey(Account) != false && dataWager.total == keyValuePairs[Account])
                        {
                            logger.LogInformation($"MATCH ( START: {startTime} END: {endTime} TOTAL: {dataWager.total} CURRENCY: {currency} )");
                            break;
                        }

                        totalPage = (int)((dataWager.total / perPage) + 1);
                        foreach (var bet in dataWager.results)
                        {
                            try
                            {
                                //if (bet.LogDataType != 2)
                                //{
                                //    continue;
                                //}
                                bet.Currency = currency;
                                bet.Account = Account;
                                wagerKiss918Service.InsertWager(bet);
                            }
                            catch (Exception ex)
                            {
                                var wagerId = bet.uuid;
                                logger.LogError(ex, ex.Message);
                                logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                                continue;
                            }
                        }
                        page++;
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
