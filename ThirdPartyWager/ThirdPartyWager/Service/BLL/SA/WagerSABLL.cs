using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSABLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerSAService wagerSAService;
        private readonly ApiSABLL apiSABLL;
        private readonly ILogger<WagerSABLL> logger;

        public WagerSABLL(ILogger<WagerSABLL> logger, WagerSAService wagerSAService, ApiSABLL apiSABLL, ThirdPartyBLL thirdPartyBLL)
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerSAService = wagerSAService;
            this.apiSABLL = apiSABLL;
            this.logger = logger;
        }

        public async Task<bool> GetWagersAsync()
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.SA);

                // define time
                DateTime current = DateTime.Now;
                DateTime timeBase = new DateTime(current.Year, current.Month, current.Day, current.Hour, current.Minute, 0);
                var beginAt = new DateTimeOffset(timeBase.AddMinutes(-10));
                var endAt = new DateTimeOffset(timeBase.AddMinutes(-5));
                var startTime = beginAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");
                var endTime = endAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd HH:mm:ss");

                var dataWager = await apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime);

                // error condition
                var errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsgId : dataWager.APIResponse.ErrorMsgId;
                var errorMsg = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsg : dataWager.APIResponse.ErrorMsg;
                if (errorCode != 0)
                {
                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                    return false;
                }
                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    //logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} )");
                    return false;
                }

                // insert wager to db
                foreach (var bet in dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail)
                {
                    try
                    {
                        wagerSAService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        var wagerId = bet.BetID.ToString();
                        logger.LogError(ex, ex.Message);
                        logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                        continue;
                    }
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
