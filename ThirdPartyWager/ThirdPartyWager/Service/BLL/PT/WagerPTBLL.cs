using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerPTBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerPTService wagerPTService;
        private readonly ApiPTBLL apiPTBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerPTBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerPTBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerPTService wagerPTService,
            ApiPTBLL apiPTBLL,
            Log2File log2File,
            ILogger<WagerPTBLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerPTService = wagerPTService;
            this.apiPTBLL = apiPTBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }
        public async Task<bool> GetWagersAsync(int tpId, string startUnix, string endUnix, string currency, int siteId, int gmt)
        {
            string logName = Tp.PT.ToString();
            string logInfo = "";
            string logMsg = $"( START: {startUnix} END: {endUnix}  CURRENCY: {currency} )";
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartyBySite(tpId, siteId);
                string apiUrl = setting["ApiUrl"];
                string pass = setting[$"Pass_{currency}"];
                string entityKey = setting[$"XEntityKey_{currency}"];

                Dictionary<string, string> certificate = new Dictionary<string, string>();
                certificate.Add("path", $@"{environment.ContentRootPath}\Storage\PT\{currency}.p12");
                certificate.Add("password", pass);

                int perPage = 500;
                int page = 1;
                int totalPage = 1;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await apiPTBLL.GetBetListByTime(apiUrl, entityKey, certificate, startUnix, endUnix, page, perPage);

                    // error condition
                    if (dataWager.error != null)
                    {
                        var errorCode = dataWager.errorcode;
                        var errorMsg = dataWager.error;

                        if (errorCode != 0)
                        {
                            logger.LogError(errorCode, errorMsg);
                            logInfo = $"THIRD_PARTY_ERROR {logMsg}";
                            logger.LogInformation(logInfo);
                            log2File.Error($"Info:\r\n{logInfo}\r\nResponse:\r\n{JsonConvert.SerializeObject(dataWager)}", logName);
                            return false;
                        }
                    }

                    if (dataWager.pagination.totalCount == 0)
                    {
                        logger.LogInformation($"NO_DATA {logMsg}");
                        return true;
                    }

                    totalPage = dataWager.pagination.totalPages;
                    foreach (var bet in dataWager.result)
                    {
                        try
                        {
                            // enter/exit game no need to insert
                            if (bet.EXITGAME == 1)
                            {
                                continue;
                            }

                            bet.gameUnix = new DateTimeOffset(bet.GAMEDATE, new TimeSpan(gmt, 0, 0)).ToUnixTimeSeconds();
                            wagerPTService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            var wagerId = bet.GAMECODE;
                            logger.LogError(ex, ex.Message);
                            logInfo = $"INSERT_WAGER ( WAGERID: {wagerId} )";
                            logger.LogInformation(logInfo);
                            log2File.Error($"Info:\r\n{logInfo}\r\nResponse:\r\n{JsonConvert.SerializeObject(bet)}\r\nException:\r\n{ex}", logName);
                            return false;
                        }
                    }
                    page++;
                }

                logger.LogInformation($"FINISH {logMsg}");
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
