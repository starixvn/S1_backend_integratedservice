using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerCancelledOne789BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerOne789Service wagerOne789Service;
        private readonly ApiOne789BLL apiOne789BLL;
        private readonly ILogger<WagerCancelledOne789BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerCancelledOne789BLL(
            ILogger<WagerCancelledOne789BLL> logger,
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

        public async Task<ResponseOne789Model> GetWagersCancelledAsync(string date, string lastId, int gameType)
        {
            ResponseOne789Model getResponse = new ResponseOne789Model();
            getResponse.result = false;
            getResponse.lastId = lastId;
            try
            {
                var setting = thirdPartyBLL.GetThirdPartySetting((int)Tp.One789);

                // get wagers (cancelled)
                var dataWager = await apiOne789BLL.GetBetListCancelled(setting["ApiUrl"], setting["ApiKey"], gameType, date, lastId);
                {
                    if (dataWager.Count > 0 && dataWager[0]["ErrorCode"] != null)
                    {
                        logger.LogError($"{dataWager[0]["ErrorCode"]}", $"{dataWager[0]["ErrorMessage"]}");
                        logger.LogInformation($"THIRD_PARTY_ERROR (DATE: {date} LASTID: {lastId} )");
                        return getResponse;
                    }

                    if (dataWager.Count == 0)
                    {
                        logger.LogInformation($"NO_DATA (DATE: {date} LASTID: {lastId} )");
                        if (!string.IsNullOrEmpty(lastId))
                        {
                            getResponse.result = true;
                        }
                        return getResponse;
                    }

                    // insert wager to db
                    foreach (var bet in dataWager)
                    {
                        var result = 0;
                        try
                        {
                            var record = JsonConvert.DeserializeObject<BetOne789>(JsonConvert.SerializeObject(bet));
                            if (record.PayoutTx != null && record.PayoutCancelTx != null)
                            {
                                result = wagerOne789Service.UpdateWagerOne789(record.Id, gameType);
                                if (result <= 0)
                                {
                                    getResponse.result = false;
                                    return getResponse;
                                }
                            }
                            getResponse.result = true;
                            getResponse.lastId = record.Id;
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, ex.Message);
                            getResponse.result = false;
                            throw ex;
                        }
                    }

                    logger.LogInformation($"FINISH");
                    return getResponse;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return getResponse;
            }
        }
    }
}
