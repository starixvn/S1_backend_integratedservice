using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOne789BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerOne789Service wagerOne789Service;
        private readonly ApiOne789BLL apiOne789BLL;
        private readonly ILogger<WagerOne789BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerOne789BLL(
            ILogger<WagerOne789BLL> logger,
            WagerOne789Service wagerOne789Service,
            ApiOne789BLL apiOne789BLL,
            ThirdPartyBLL thirdPartyBLL,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerOne789Service = wagerOne789Service;
            this.apiOne789BLL = apiOne789BLL;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, int gameType)
        {
            try
            {
                // get api info
                var setting = thirdPartyBLL.GetThirdPartySetting(tpId);

                int page = 1;
                int totalPage = 1;
                int perPage = 500;
                while (page <= totalPage)
                {
                    // get wager
                    var dataWager = await apiOne789BLL.GetBetListByTime(setting["ApiUrl"], setting["ApiKey"], startTime, endTime, page, perPage, gameType);

                    if (dataWager.Count > 0 && dataWager[0]["ErrorCode"] != null)
                    {
                        logger.LogError($"{dataWager[0]["ErrorCode"]}", $"{dataWager[0]["ErrorMessage"]}");
                        logger.LogInformation($"THIRD_PARTY_ERROR ( START: {startTime} END: {endTime} PAGE: {page} )");
                        return false;
                    }
                    if (dataWager.Count == 0)
                    {
                        logger.LogInformation($"NO_DATA ( START: {startTime} END: {endTime} GAMETYPE: {gameType} PAGE: {page} )");
                        return true;
                    }

                    totalPage = (dataWager.Count == perPage) ? totalPage + 1 : totalPage;
                    foreach (var bet in dataWager)
                    {
                        try
                        {
                            var record = JsonConvert.DeserializeObject<BetOne789>(JsonConvert.SerializeObject(bet));
                            if (gameType == (int)GameType.One789_LiveCasino)
                            {
                                record.TransId = $"{record.AreaNumber}_{record.GameNumber}_{record.GameRound}_{record.Username}";
                                record.GameResult = (record.Status == 0) ? -1 : record.GameResult;
                            }
                            else
                            {
                                record.Numbers = JsonConvert.SerializeObject(record.Numbers);
                                record.TransId = $"{record.Term}_{record.GameType}_{record.Username}";
                            }
                            wagerOne789Service.InsertWagerOne789(record, gameType);
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                            logger.LogInformation($"INSERT_WAGER ( WAGER_INFO: {JsonConvert.SerializeObject(bet)}  PAGE: {page} )");
                            return false;
                        }
                    }
                    page++;
                }
                logger.LogInformation($"FINISH ( START: {startTime} END: {endTime} GAMETYPE: {gameType} )");
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
