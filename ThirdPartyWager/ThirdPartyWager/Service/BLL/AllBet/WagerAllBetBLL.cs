using Microsoft.Extensions.Logging;
using Org.BouncyCastle.Asn1.X9;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAllBetBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAllBetService wagerAllBetService;
        private readonly ApiAllBetBLL apiAllBetBLL;
        private readonly ILogger<WagerAllBetBLL> logger;

        public WagerAllBetBLL(ILogger<WagerAllBetBLL> logger, WagerAllBetService wagerAllBetService, ApiAllBetBLL apiAllBetBLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAllBetService = wagerAllBetService;
            this.apiAllBetBLL = apiAllBetBLL;
            this.logger = logger;
        }

        public async Task<bool> GetWagersAsync()
        {
            
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.Allbet);

                // define time
                DateTime current = DateTime.Now;
                DateTime timeBase = new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0);
                var beginAt = new DateTimeOffset(timeBase.AddMinutes(-10));
                var endAt = new DateTimeOffset(timeBase.AddMinutes(-5));
                var startTime = beginAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                var endTime = endAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                while (pageRowCnt >= 1)
                {
                    // bet 
                    var dataWager = await apiAllBetBLL.GetBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex);

                    if (dataWager == null)
                    {
                        return false;
                    }
                    // error condition
                    var errorCode = dataWager.error_code;
                    var errorMsg = dataWager.message;
                    if(errorCode != "OK")
                    {
                        logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                        break;
                    }

                    pageRowCnt = dataWager.page.datas.Count();
                    if (pageRowCnt == 0)
                    {
                        //logger.LogError(errorCode, errorMsg);
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} PAGE: {pageRowCnt} )");
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
                            var wagerId = bet.betNum.ToString();
                            logger.LogError(ex, ex.Message);
                            logger.LogError($"INSERT_WAGER ( WAGERID: {wagerId} )");
                            continue;
                        }
                    }
                    pageIndex++;
                }

                // pagination
                int pageIndexOfModify = 1;
                int pageRowCntOfModify = 1;
                while (pageRowCntOfModify >= 1)
                {
                    // modified bet
                    var modifyWager = await apiAllBetBLL.GetModifiedBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndexOfModify);
                    pageRowCntOfModify = modifyWager.page.datas.Count();
                    if (pageRowCntOfModify == 0)
                    {
                        break;
                    }

                    // insert wager to db
                    foreach (var bet in modifyWager.page.datas)
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
                    pageIndexOfModify++;
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
