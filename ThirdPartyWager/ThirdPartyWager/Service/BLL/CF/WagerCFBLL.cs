using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerCFBLL
    {

        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerCFService wagerCFService;
        private readonly ApiCFBLL apiCFBLL;
        private readonly ILogger<WagerCFBLL> logger;


        public WagerCFBLL(
            ILogger<WagerCFBLL> logger,
            WagerCFService wagerCFService,
            ApiCFBLL apiCFBLL,
            ThirdPartyBLL thirdPartyBLL
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerCFService = wagerCFService;
            this.apiCFBLL = apiCFBLL;
            this.logger = logger;

        }

        public async Task<bool> GetWagersAsync(int TpId, string startTime, string endTime)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(TpId);

                // get wager
                var dataWager = await apiCFBLL.GetBetListByTime(setting["ApiUrl"], setting["api_key"], setting["agent_code"], startTime, endTime);
                var arrWager = dataWager.get_cockfight_processed_ticket;

                // error condition
                var errorCode = arrWager.status_code;
                var errorMsg = arrWager.status_text;
                if (errorCode != "00")
                {
                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} )");
                    return false;
                }
                if (arrWager.total_records == 0)
                {
                    //logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} )");
                    return true;
                }

                // order need to same as document
                var field = new string[]{
                    "ticket_id", "login_id", "arena_code", "arena_name_cn", "match_no",
                    "match_type", "match_date", "fight_no", "fight_datetime", "meron_cock",
                    "meron_cock_cn", "wala_cock", "wala_cock_cn", "bet_on", "odds_type",
                    "odds_asked", "odds_given", "stake", "stake_money", "balance_open",
                    "balance_close", "created_datetime", "fight_result", "status", "winloss",
                    "comm_earned", "payout", "balance_open1", "balance_close1", "processed_datetime"
                };

                string sList = arrWager.data[0];
                string[] aRow = sList.Split('|');
                foreach (var item in aRow)
                {
                    Dictionary<string, string> keyValuePairs = new Dictionary<string, string>();
                    string[] aRecord = item.Split(',');
                    for (var i = 0; i < field.Length; i++)
                    {
                        keyValuePairs.Add(field[i], aRecord[i]);
                    }

                    var bet = JsonConvert.DeserializeObject<BetCF>(JsonConvert.SerializeObject(keyValuePairs));
                    try
                    {
                        wagerCFService.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        var wagerId = bet.ticket_id;
                        logger.LogError(ex, ex.Message);
                        logger.LogError($"INSERT_WAGER ( WAGERID: {wagerId} )");
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
