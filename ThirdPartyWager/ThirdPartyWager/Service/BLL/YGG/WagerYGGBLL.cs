using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerYGGBLL
    {
        private readonly ILogger<WagerYGGBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Common _common;
        private readonly Tp _thisTp;
        private readonly MySqlClient _wagerDbClient;
        private readonly Dictionary<int, YGGConfig> _siteConfigDic;

        private const int SuccessCode = 0;

        private int _jobRunCounter = 0; // Job計數器，每一分鐘執行一次，每1小時會累積到 60
        private int _JobCounterLimit = 60;
        private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";


        public WagerYGGBLL(IServiceProvider provider)
        {
            _thisTp = Tp.YGG; 
            _logger = provider.GetRequiredService<ILogger<WagerYGGBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _common = provider.GetRequiredService<Common>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YGGConfig>(_thisTp.ToString());
        }
    
        /// <summary>
        ///  去第三方取得注單，寫入 DB
        /// </summary>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                var siteList = _thirdPartyBLL.GetTpListByTpId((int)_thisTp);
                foreach (var site in siteList)
                {
                    if (site.TicketKey == 0)
                    {
                        continue;
                    }
                    // 分Site取得TpList
                    var setting = _siteConfigDic[site.SiteId];
                    
                    await QueryBetRecordAsync(setting, startTime, endTime);
                    _jobRunCounter++;
                    if (_jobRunCounter != _JobCounterLimit) continue;
                    
                    #region 補撈2天前的資料

                    var hours = -54; // Hans說做前54小時
                    const int addHours = 18;
                    while (hours < 0)
                    {
                        var redoStartTime = DateTime.Parse(endTime).AddHours(hours);
                        var redoEndTime = redoStartTime.AddHours(addHours);

                        await QueryBetRecordAsync(setting, redoStartTime.ToString(DateTimeFormat), redoEndTime.ToString(DateTimeFormat));
                        hours += addHours;
                    }

                    _jobRunCounter = 0; // 歸零

                    #endregion

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YGG GetWagersAsync Exception.", ex.Message);
                return false;
            }

            return true;
        }

        private async Task QueryBetRecordAsync(YGGConfig config, string startTime, string endTime)
        {
            var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "topOrg", config.topOrg },
                { "org", config.org },
                { "lastId", "0"},
                { "startTime", startTime},
                { "endTime",endTime },
                { "sign", _common.ToMD5($"{config.topOrg}{config.org}{config.key}") },
                { "gametype", "yg" }
            });

            var response = await _httpHelper.GetClient().PostAsync(config.WagerUrl, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
            }

            var responseResult = await response.Content.ReadAsStringAsync();
            var requestContent = await httpContent.ReadAsStringAsync();
            _logger.LogInformation($"{config.WagerUrl} RequestParameter：{requestContent} WagerYGG：{responseResult}");

            var betRecordResponse = _common.FromJson<WagerYggResponse>(responseResult);

            if (betRecordResponse.Code == SuccessCode && (betRecordResponse.Data != null && betRecordResponse.Data.Count > 0))
            {
                var insertData = betRecordResponse.Data.Where(r => r.Type == "wager").ToList(); // wager 投注
                var updateWinLoseAmount = betRecordResponse.Data.Where(r => r.Type == "endwager" || r.Type == "endWager").ToList(); // endwager 結算
                var updateJackpotAmount = betRecordResponse.Data.Where(r => r.Type == "appendwager" || r.Type == "appendWager").ToList(); // 獎金池

                var tasks = new List<Task<int>>();
                if (insertData.Count > 0) tasks.AddRange(insertData.Select(item => Task.Run(() => InsertWager(item))).ToList());
                if (tasks.Count > 0)
                    await Task.WhenAll(tasks); // 寫入Wager資料

                tasks.Clear();
                if (updateWinLoseAmount.Count > 0) tasks.AddRange(updateWinLoseAmount.Select(item => Task.Run(() => UpdateWinLoseAmount(item))).ToList());
                if (tasks.Count > 0)
                    await Task.WhenAll(tasks); // 更新輸贏值

                tasks.Clear();
                if (updateJackpotAmount.Count > 0) tasks.AddRange(updateJackpotAmount.Select(item => Task.Run(() => UpdateJockPotAmount(item))).ToList());
                if (tasks.Count > 0)
                    await Task.WhenAll(tasks);// 更新獎金池
            }
        }

        private Task<int> InsertWager(WagerYGGModel betData)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerYGG_Ins", new
            {
                Org = betData.Org,
                LoginName = betData.LoginName,
                Currency = betData.Currency,
                Type = betData.Type,
                Amount = betData.Amount.Value,
                GameName = betData.GameName,
                _Reference = betData.Reference,
                CreateTime = betData.CreateTime,
                DCGameID = betData.DCGameID
            });
        }

        private Task<int> UpdateWinLoseAmount(WagerYGGModel betData)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerYGG_Set", new
            {
                Amount = betData.Amount.Value,
                _Reference = betData.Reference
            });
        }

        private Task<int> UpdateJockPotAmount(WagerYGGModel betData)
        {
            return _wagerDbClient.ExecuteAsync("Service_TpWagerYGGJackPotAmount_Set", new
            {
                Amount = betData.Amount.Value,
                _Reference = betData.Reference
            });
        }
    }
}

