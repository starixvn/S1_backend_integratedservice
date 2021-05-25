using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerAllBetBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerAllBetService wagerAllBetService;
        private readonly ApiAllBetBLL apiAllBetBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerAllBetBLL> logger;

        public WagerAllBetBLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerAllBetService wagerAllBetService,
            ApiAllBetBLL apiAllBetBLL,
            Log2File log2File,
            ILogger<WagerAllBetBLL> logger
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerAllBetService = wagerAllBetService;
            this.apiAllBetBLL = apiAllBetBLL;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.Allbet.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("TpId", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                // pagination
                int pageIndex = 1;
                int pageRowCnt = 1;
                while (pageRowCnt >= 1)
                {
                    // bet 
                    var dataWager = await apiAllBetBLL.GetBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndex, guid);

                    if (dataWager == null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    // error condition
                    var errorCode = dataWager.error_code;
                    var errorMsg = dataWager.message;
                    if (errorCode != "OK")
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }

                    pageRowCnt = dataWager.page.datas.Count();
                    if (pageRowCnt == 0)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
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
                            msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                            logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                            //continue;
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
                    var modifyWager = await apiAllBetBLL.GetModifiedBetListByTime(setting["ApiUrl"], setting["DesKey"], setting["MD5Key"], setting["PropertyId"], startTime, endTime, pageIndexOfModify, guid);
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
                            msg = $"INSERT_WAGER(WAGERID: {bet.betNum} )";
                            logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                            //continue;
                        }
                    }
                    pageIndexOfModify++;
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
