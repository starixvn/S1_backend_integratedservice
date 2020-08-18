using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.AllBet
{
    public class WagerRedoAllBetBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAllBetService wagerAllBetService;
        private readonly ApiAllBetBLL apiAllBetBLL;
        private readonly ILogger<WagerRedoAllBetBLL> logger;

        public WagerRedoAllBetBLL(ILogger<WagerRedoAllBetBLL> logger, ThirdPartyBLL thirdPartyBLL, WagerAllBetService wagerAllBetService, ApiAllBetBLL apiAllBetBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAllBetService = wagerAllBetService;
            this.apiAllBetBLL = apiAllBetBLL;
            this.logger = logger;
        }
        public async Task<bool> RedoWagersAsync()
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Allbet);

                // define time
                DateTime current = DateTime.Now;
                DateTime timeBase = new DateTime(current.Year, current.Month, current.Day, current.Hour, 0, 0);
                var beginAt = new DateTimeOffset(timeBase.AddHours(-4));
                var endAt = new DateTimeOffset(timeBase.AddHours(-3));
                var startTime = beginAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                var endTime = endAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");

                // count interval wagers
                var getWagerCount = wagerAllBetService.GetWagerTotal(beginAt.ToUnixTimeSeconds(), endAt.ToUnixTimeSeconds());
                int wagerCount = getWagerCount.wagerCount;

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                while (pageRowCnt >= 1)
                {
                    // bet 
                    var dataWager = await apiAllBetBLL.GetBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex);

                    // error condition
                    var errorCode = dataWager.error_code;
                    var errorMsg = dataWager.message;
                    if (errorCode != "OK")
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        break;
                    }

                    pageRowCnt = dataWager.page.datas.Count();
                    int totalNum = dataWager.page.count;
                    if (pageRowCnt == 0)
                    {
                        //logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} PAGE: {pageRowCnt} )");
                        break;
                    }

                    // db wager count equals api bet count do nothing
                    if (totalNum == wagerCount)
                    {
                        logger.LogInformation($"MATCH ( START: {startTime} END: {endTime} TOTAL: {totalNum} )");
                        break;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager.page.datas)
                    {
                        try
                        {
                            wagerAllBetService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                            continue;
                        }
                    }
                    pageIndex++;
                }

                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} )");
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return true;
            }

        }
    }
}
