using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoSABLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerSAService wagerSAService;
        private readonly ApiSABLL apiSABLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoSABLL> logger;

        public WagerRedoSABLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerSAService wagerSAService,
            ApiSABLL apiSABLL,
            Log2File log2File,
            ILogger<WagerRedoSABLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerSAService = wagerSAService;
            this.apiSABLL = apiSABLL;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<bool> RedoWagersAsync(int tpId, string startTime, string endTime, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.SA.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // count interval wagers
                var getWagerCount = wagerSAService.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                var getWagerSummary = await apiSABLL.GetBetSummaryByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                var errorCode = getWagerSummary.GetTransactionDetailsResponse != null ? getWagerSummary.GetTransactionDetailsResponse.ErrorMsgId : getWagerSummary.APIResponse.ErrorMsgId;
                var errorMsg = getWagerSummary.GetTransactionDetailsResponse != null ? getWagerSummary.GetTransactionDetailsResponse.ErrorMsg : getWagerSummary.APIResponse.ErrorMsg;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(getWagerSummary)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary));
                    return false;
                }
                if (errorCode == 0 && getWagerSummary.GetTransactionDetailsResponse.Details == null)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary), "Info");
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
                    msg = "MATCH";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(getWagerSummary), "Info");
                    return true;
                }

                var dataWager = await apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsgId : dataWager.APIResponse.ErrorMsgId;
                errorMsg = dataWager.GetAllBetDetailsForTimeIntervalResponse != null ? dataWager.GetAllBetDetailsForTimeIntervalResponse.ErrorMsg : dataWager.APIResponse.ErrorMsg;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }
                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
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
                        msg = $"INSERT_WAGER(WAGERID: {bet.BetID} )";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                        return false;
                    }
                }

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                logger.LogError($"MESSAGE: {msg}, EXCEPTION: {ex.Message}");
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }
    }
}
