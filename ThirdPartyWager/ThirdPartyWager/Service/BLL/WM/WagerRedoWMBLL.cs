using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoWMBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerWMService wagerWMService;
        private readonly ApiWMBLL apiWMBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoWMBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoWMBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerWMService wagerWMService,
            ApiWMBLL apiWMBLL,
            Log2File log2File,
            ILogger<WagerRedoWMBLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerWMService = wagerWMService;
            this.apiWMBLL = apiWMBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.WM.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

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
                    currencyList.Add("INR");
                }
                bool reCheck = false;
                foreach (var curr in currencyList)
                {
                    // count interval wagers
                    var getWagerCount = wagerWMService.GetWagerTotal(startUnix, endUnix, curr);
                    int wagerCount = getWagerCount.wagerCount;

                    var dataWager = await apiWMBLL.GetBetListByTime(setting["ApiUrl"], setting[$"vendorId_{curr}"], setting[$"signature_{curr}"], startTime, endTime, guid);

                    // error condition
                    var errorCode = dataWager.errorCode;
                    if (errorCode == 107)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                        continue;
                    }

                    if (errorCode != 0)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    // db wager count equals api bet count do nothing
                    var totalNum = dataWager.result.Count();
                    if (totalNum == wagerCount)
                    {
                        msg = "MATCH";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
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
                            msg = $"INSERT_WAGER(WAGERID: {bet.betId} )";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                        }
                    }

                    reCheck = true;
                }

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return !(reCheck);
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }

        }

    }
}
