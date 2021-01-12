using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoV8BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerV8Service wagerV8Service;
        private readonly ApiV8BLL apiV8BLL;
        private readonly ILogger<WagerRedoV8BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoV8BLL(
            ILogger<WagerRedoV8BLL> logger,
            WagerV8Service wagerV8Service,
            ApiV8BLL apiV8BLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerV8Service = wagerV8Service;
            this.apiV8BLL = apiV8BLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, long startUnix, long endUnix)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);
                
                // count interval wagers
                var getWagerCount = wagerV8Service.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // get wager
                var dataWager = await apiV8BLL.GetBetListByTime(setting["WagerUrl"], setting["Agent"], setting["Deskey"], setting["Md5key"], startUnix, endUnix);

                // error condition
                var errorCode = dataWager.d.code;
                var errorMsg = dataWager.d.code.ToString();

                if (errorCode != 0 && errorCode != 16)
                {
                    logger.LogError(errorCode, errorMsg);
                    logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startUnix} END: {endUnix} )");
                    return false;
                }

                var totalNum = dataWager.d.count;
                if (totalNum == wagerCount)
                {
                    logger.LogInformation($"MATCH ( START: {startUnix} END: {endUnix} )");
                    return true;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    logger.LogInformation($"NO_DATA ( START: {startUnix} END: {endUnix} )");
                    return true;
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    Dictionary<string, dynamic> keyValuePairs = new Dictionary<string, dynamic>();
                    keyValuePairs.Add("GameID", dataWager.d.list.GameID[i]);
                    keyValuePairs.Add("Accounts", dataWager.d.list.Accounts[i]);
                    keyValuePairs.Add("ServerID", dataWager.d.list.ServerID[i]);
                    keyValuePairs.Add("KindID", dataWager.d.list.KindID[i]);
                    keyValuePairs.Add("TableID", dataWager.d.list.TableID[i]);
                    keyValuePairs.Add("ChairID", dataWager.d.list.ChairID[i]);
                    keyValuePairs.Add("UserCount", dataWager.d.list.UserCount[i]);
                    keyValuePairs.Add("CellScore", dataWager.d.list.CellScore[i]);
                    keyValuePairs.Add("AllBet", dataWager.d.list.AllBet[i]);
                    keyValuePairs.Add("Profit", dataWager.d.list.Profit[i]);
                    keyValuePairs.Add("Revenue", dataWager.d.list.Revenue[i]);
                    keyValuePairs.Add("GameStartTime", dataWager.d.list.GameStartTime[i]);
                    keyValuePairs.Add("GameEndTime", dataWager.d.list.GameEndTime[i]);
                    keyValuePairs.Add("CardValue", dataWager.d.list.CardValue[i]);
                    keyValuePairs.Add("ChannelID", dataWager.d.list.ChannelID[i]);
                    keyValuePairs.Add("LineCode", dataWager.d.list.LineCode[i]);
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {

                    var bet = JsonConvert.DeserializeObject<BetV8>(item);
                    try
                    {
                        wagerV8Service.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, ex.Message);
                        logger.LogInformation($"INSERT_WAGER ( WAGER: {JsonConvert.SerializeObject(item)} )");
                        return false;
                    }
                }
                logger.LogInformation($"FINISH ( START: {startUnix} END: {endUnix} )");

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
