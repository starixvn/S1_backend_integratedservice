using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerOne789BLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly WagerOne789Service wagerOne789Service;
        private readonly ApiOne789BLL apiOne789BLL;
        private readonly Log2File log2File;
        private readonly ILogger<WagerOne789BLL> logger;
        private readonly IHostEnvironment environment;

        public WagerOne789BLL(
            ThirdPartyBLL thirdPartyBLL,
            WagerOne789Service wagerOne789Service,
            ApiOne789BLL apiOne789BLL,
            Log2File log2File,
            ILogger<WagerOne789BLL> logger,
            IHostEnvironment environment
        )
        {
            this.thirdPartyBLL = thirdPartyBLL;
            this.wagerOne789Service = wagerOne789Service;
            this.apiOne789BLL = apiOne789BLL;
            this.log2File = log2File;
            this.logger = logger;
            this.environment = environment;
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime, int gameType)
        {
            string msg = "FATAL_ERROR";
            string logName = Tp.One789.ToString();
            string guid = Guid.NewGuid().ToString();
            LogContext.PushProperty("Tp", logName);

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
                    var dataWager = await apiOne789BLL.GetBetListByTime(setting["ApiUrl"], setting["ApiKey"], startTime, endTime, page, perPage, gameType, guid);

                    if (dataWager.Count > 0 && dataWager[0]["ErrorCode"] != null)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        logger.LogError($"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(dataWager)}");
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager));
                        return false;
                    }
                    if (dataWager.Count == 0)
                    {
                        msg = "NO_DATA";
                        log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(dataWager), "Info");
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
                            msg = $"INSERT_WAGER";
                            logger.LogError(ex, $"MESSAGE: {msg}, RESPONSE: {JsonConvert.SerializeObject(bet)}, EXCEPTION: {ex.Message}");
                            log2File.AddResultLog(logName, guid, msg, JsonConvert.SerializeObject(bet), "Error", ex.Message);
                            return false;
                        }
                    }
                    page++;
                }

                msg = "FINISH";
                log2File.AddResultLog(logName, guid, msg, null, "Info");
                return true;
            }
            catch (Exception ex)
            {
                msg = "FATAL_ERROR";
                logger.LogError(ex, $"MESSAGE: {msg}, EXCEPTION: {ex.Message}");
                log2File.AddResultLog(logName, guid, msg, null, "Error", ex.Message);
                return false;
            }
        }
    }
}
