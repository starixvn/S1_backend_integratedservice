using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerN2LiveBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiN2LiveBLL _apiN2LiveBLL;
        private readonly WagerN2LiveService _wagerN2LiveService;
        private readonly ILogger<WagerN2LiveBLL> _logger;

        public WagerN2LiveBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiN2LiveBLL = provider.GetRequiredService<ApiN2LiveBLL>();
            _wagerN2LiveService = provider.GetRequiredService<WagerN2LiveService>();
            _logger = provider.GetRequiredService<ILogger<WagerN2LiveBLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, string apiUrl, string vendorId, string merchantCode, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get wager
                var dataWager = await _apiN2LiveBLL.GetBetListByTime(apiUrl, vendorId, merchantCode, startTime, endTime, guid);

                // error condition
                if (dataWager.message.status != "success")
                {
                    // 506是有牌局未完成结算，startdate enddate这段时间的牌局结算完成就能返回
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (dataWager.message.result.gameinfo == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
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
                                var bet = new BetN2Live
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
                                    winningodds = (betTmp.ContainsKey("winningodds") && !string.IsNullOrEmpty(betTmp["winningodds"].ToString())) ? JsonConvert.SerializeObject(betTmp["winningodds"]) : "{}",
                                    dealdetail = JsonConvert.SerializeObject(dealItem.dealdetails.dealdetail),
                                    result = (dealItem.results != null) ? JsonConvert.SerializeObject(dealItem.results) : null,
                                };
                                _wagerN2LiveService.InsertWager(bet);
                            }
                        }
                        catch (Exception ex)
                        {
                            msg = $"INSERT_WAGER(WAGERID: {dealItem.id} )";
                            _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(dealItem)}\r\n{ex.Message}");
                            return false;
                        }
                    }

                }
                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startTime} ~ {endTime}");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
                return false;
            }
        }
    }
}
