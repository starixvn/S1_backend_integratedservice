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
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerRedoV8BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerV8Service wagerV8Service;
        private readonly ApiV8BLL apiV8BLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerRedoV8BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerRedoV8BLL(
            ILogger<WagerRedoV8BLL> logger,
            WagerV8Service wagerV8Service,
            ApiV8BLL apiV8BLL,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerV8Service = wagerV8Service;
            this.apiV8BLL = apiV8BLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> RedoWagersAsync(int tpId, long startUnix, long endUnix)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.V8.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);
                
                // count interval wagers
                var getWagerCount = wagerV8Service.GetWagerTotal(startUnix, endUnix);
                int wagerCount = getWagerCount.wagerCount;

                // get wager
                var dataWager = await apiV8BLL.GetBetListByTime(setting["WagerUrl"], setting["Agent"], setting["Deskey"], setting["Md5key"], startUnix, endUnix, guid);

                // error condition
                var errorCode = dataWager.d.code;

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }

                var totalNum = dataWager.d.count;
                if (totalNum == wagerCount)
                {
                    msg = "MATCH";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
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
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
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
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }
    }
}
