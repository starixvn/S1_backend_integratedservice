using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
            string logName = Tp.N2Live.ToString();
            string logInfo = "";
            string logMsg = $"( START: {startTime} END: {endTime}  VENDORID: {vendorId})";
            string guid = Guid.NewGuid().ToString();
            try
            {
                // get wager
                var dataWager = await apiN2LiveBLL.GetBetListByTime(apiUrl, vendorId, merchantCode, startTime, endTime, guid);

                // error condition
                if (dataWager.message.status != "success")
                {
                    var errorCode = dataWager.message.result.element.properties[0].text;
                    var errorMsg = dataWager.message.status;
                    // 506是有牌局未完成结算，startdate enddate这段时间的牌局结算完成就能返回
                    logger.LogError(errorCode, errorMsg);
                    logInfo = $"THIRD_PARTY_ERROR {logMsg}";
                    logger.LogInformation(logInfo);
                    log2File.Error($"Guid:\r\n{guid}\r\nInfo:\r\n{logInfo}\r\nResponse:\r\n{JsonConvert.SerializeObject(dataWager)}", logName);
                    return false;
                }

                if (dataWager.message.result.gameinfo == null)
                {
                    logger.LogInformation($"NO_DATA {logMsg}");
                    logInfo = $"NO_DATA {logMsg}";
                    log2File.Info($"Guid:\r\n{guid}\r\nInfo:\r\n{logInfo}\r\nResponse:\r\n{JsonConvert.SerializeObject(dataWager)}", logName);
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
                            BetN2Live bet = new BetN2Live
                            {
                                gamecode = Int32.Parse(gameItem.code),
                                id = Int64.Parse(dealItem.id),
                                startdate = dealItem.startdate,
                                enddate = dealItem.enddate,
                                status = Int32.Parse(dealItem.status),
                                gameid = dealItem.code,
                                login = dealItem.betinfo.clientbet.login,
                                bet_amount = dealItem.betinfo.clientbet.bet_amount,
                                payout_amount = dealItem.betinfo.clientbet.payout_amount,
                                valid_turnover = dealItem.betinfo.clientbet.valid_turnover,
                                handle = dealItem.betinfo.clientbet.handle,
                                hold = dealItem.betinfo.clientbet.hold,
                                betid = Int64.Parse(dealItem.betinfo.clientbet.betid),
                                schemeid = (dealItem.betinfo.clientbet.betdetail[0]["@schemeid"].ToString() != "") ? Int64.Parse(dealItem.betinfo.clientbet.betdetail[0]["@schemeid"].ToString()) : 0,
                                channel = dealItem.betinfo.clientbet.betdetail[0]["@channel"].ToString(),
                                betdetail = JsonConvert.SerializeObject(dealItem.betinfo.clientbet.betdetail),
                                dealdetail = JsonConvert.SerializeObject(dealItem.dealdetails.dealdetail),
                                result = (dealItem.results != null) ? JsonConvert.SerializeObject(dealItem.results.result) : null,
                            };

                            wagerN2LiveService.InsertWager(bet);
                        }
                        catch (Exception ex)
                        {
                            var wagerId = dealItem.id;
                            logger.LogError(ex, ex.Message);
                            logger.LogInformation($"INSERT_WAGER ( WAGERID: {wagerId} )");
                            return false;
                        }

                    }

                }
                logger.LogInformation($"FINISH {logMsg}");
                logInfo = $"FINISH {logMsg}";
                log2File.Info($"Guid:\r\n{guid}\r\nInfo:\r\n{logInfo}\r\nResponse:\r\n{JsonConvert.SerializeObject(dataWager)}", logName);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                logInfo = $"FATAL_ERROR {logMsg}";
                log2File.Error($"Guid:\r\n{guid}\r\nInfo:\r\n{logInfo}\r\nException:\r\n{ex}", logName);
                return false;
            }
        }

    }
}
