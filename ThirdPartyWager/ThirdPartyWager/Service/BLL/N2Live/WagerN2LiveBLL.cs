using Dapper;
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
    public class WagerN2LiveBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerN2LiveService wagerN2LiveService;
        private readonly ApiN2LiveBLL apiN2LiveBLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerN2LiveBLL> logger;
        private readonly IHostEnvironment environment;

        public WagerN2LiveBLL(
            ILogger<WagerN2LiveBLL> logger,
            WagerN2LiveService wagerN2LiveService,
            ApiN2LiveBLL apiN2LiveBLL,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerN2LiveService = wagerN2LiveService;
            this.apiN2LiveBLL = apiN2LiveBLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string apiUrl, string vendorId, string merchantCode, string startTime, string endTime)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.N2Live.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

            try
            {
                // get wager
                var dataWager = await apiN2LiveBLL.GetBetListByTime(apiUrl, vendorId, merchantCode, startTime, endTime, guid);

                // error condition
                if (dataWager.message.status != "success")
                {
                    // 506是有牌局未完成结算，startdate enddate这段时间的牌局结算完成就能返回
                    msg = "THIRD_PARTY_ERROR";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                    return false;
                }

                if (dataWager.message.result.gameinfo == null)
                {
                    msg = "NO_DATA";
                    log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
                    return true;
                }

                List<GameN2Live> gameData = JsonConvert.DeserializeObject<List<GameN2Live>>(JsonConvert.SerializeObject(dataWager.message.result.gameinfo.game));
                foreach (var gameItem in gameData)
                {
                    IList<DealN2Live> dealData = JsonConvert.DeserializeObject<IList<DealN2Live>>(JsonConvert.SerializeObject(gameItem.deal));

                    foreach (var dealItem in dealData)
                    {
                        try
                        {
                            foreach (var betTmp in dealItem.betinfo.clientbet)
                            {
                                BetN2Live bet = new BetN2Live
                                {
                                    gamecode = Int32.Parse(gameItem.code),
                                    id = Int64.Parse(dealItem.id),
                                    startdate = dealItem.startdate,
                                    enddate = dealItem.enddate,
                                    status = Int32.Parse(dealItem.status),
                                    gameid = dealItem.code,
                                    login = betTmp["@login"],
                                    bet_amount = betTmp["@bet_amount"],
                                    payout_amount = betTmp["@payout_amount"],
                                    valid_turnover = betTmp["@valid_turnover"],
                                    handle = betTmp["@handle"],
                                    hold = betTmp["@hold"],
                                    betid = Convert.ToInt64(betTmp["@betid"]),
                                    schemeid = (betTmp["betdetail"][0]["@schemeid"].ToString() != "") ? Convert.ToInt64(betTmp["betdetail"][0]["@schemeid"]) : 0,
                                    channel = betTmp["betdetail"][0]["@channel"].ToString(),
                                    betdetail = JsonConvert.SerializeObject(betTmp["betdetail"]),
                                    dealdetail = JsonConvert.SerializeObject(dealItem.dealdetails.dealdetail),
                                    result = (dealItem.results != null) ? JsonConvert.SerializeObject(dealItem.results.result) : null,
                                };
                                wagerN2LiveService.InsertWager(bet);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {dealItem.id} )";
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dealItem), "Error", ex.Message);
                            return false;
                        }
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
