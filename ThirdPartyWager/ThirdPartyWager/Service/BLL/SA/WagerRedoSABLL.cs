using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoSABLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerSAService wagerSAService;
        private readonly ApiSABLL apiSABLL;
        private readonly ILogger<WagerRedoSABLL> logger;

        public WagerRedoSABLL(
            ILogger<WagerRedoSABLL> logger,
            ThirdPartyBLL thirdPartyBLL,
            WagerSAService wagerSAService,
            ApiSABLL apiSABLL
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerSAService = wagerSAService;
            this.apiSABLL = apiSABLL;
            this.logger = logger;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = wagerSAService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await apiSABLL.GetBetSummaryByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime);

                // error condition
                var errorCode = getWagerSummary.GetTransactionDetailsResponse != null ? getWagerSummary.GetTransactionDetailsResponse.ErrorMsgId : getWagerSummary.APIResponse.ErrorMsgId;
                var errorMsg = getWagerSummary.GetTransactionDetailsResponse != null ? getWagerSummary.GetTransactionDetailsResponse.ErrorMsg : getWagerSummary.APIResponse.ErrorMsg;
                if (errorCode != 0)
                {
                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                    return false;
                }
                if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Details == null)
                {
                    //logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} )");
                    return true;
                }

                // db wager count equals api bet count do nothing
                var totalNum = 0;
                foreach (var data in getWagerSummary.GetTransactionDetailsResponse.Details.TransactionDetails)
                {
                    totalNum += data.Count;
                }
                if (totalNum == wagerCount)
                {
                    logger.LogInformation($"MATCH ( START: {startTime} END: {endTime} TOTAL: {totalNum} )");
                    return true;
                }

                var dataWager = await apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime);

                // error condition
                errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsgId : dataWager.APIResponse.ErrorMsgId;
                errorMsg = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsg : dataWager.APIResponse.ErrorMsg;
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
                    return true;
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
                        logger.LogError(ex, ex.Message);
                        continue;
                    }
                }

                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} )");
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
