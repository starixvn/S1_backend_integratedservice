using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerYGGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerYGGBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly MySqlClient _wagerDbClient;
        private readonly YggConfig _config;

        private const int TpId = (int)Tp.YGG;
        private const int SuccessCode = 0;

        public WagerYGGBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<WagerYGGBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            var setting = _thirdPartyBLL.GetThirdPartySetting(TpId);
            _config = new YggConfig(setting);
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
                await QueryBetRecordAsync(_config, startTime, endTime);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YGG GetWagersAsync Exception.", ex.Message);
                return false;
            }
        }

        private async Task QueryBetRecordAsync(YggConfig config, string startTime, string endTime)
        {
            var httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "topOrg", config.TopOrg },
                { "org", config.Org },
                { "lastId", "0"},
                { "startTime", startTime},
                { "endTime",endTime },
                { "sign", _common.ToMD5($"{config.TopOrg}{config.Org}{config.Key}") },
                { "gametype", "yg" }
            });

            var response = await _httpHelper.GetClient().PostAsync(config.WagerUrl, httpContent);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"StatusCode:{response.StatusCode} | Reason:{response.ReasonPhrase}");
            }
            var json = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"WagerYGG：{json}");

            var betRecordResponse = _common.FromJson<WagerYggResponse>(json);

            if (betRecordResponse.Code == SuccessCode && betRecordResponse.Data.Count > 0)
            {
                var insertData = betRecordResponse.Data.Where(r => r.Type == "wager").ToList(); // wager 投注
                var updateWinLoseAmount = betRecordResponse.Data.Where(r => r.Type == "endwager").ToList(); // endwager 結算
                var updateJackpotAmount = betRecordResponse.Data.Where(r => r.Type == "appendwager").ToList(); // 獎金池

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

