using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.YGG
{
    public class WagerYGGBLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ILogger<WagerYGGBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly MySqlClient _wagerDbClient;
        private readonly int _tpId = (int)Tp.YGG;
        private readonly YggConfig _config;
        private readonly int TimeZone = 0;

        public WagerYGGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _logger = provider.GetRequiredService<ILogger<WagerYGGBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            var setting = _thirdPartyBLL.GetThirdPartySetting(_tpId);
            _config = new YggConfig(setting);

        }

        public async Task<bool> GetWagersAsync(string startTime, string endTime)
        {
            try
            {
                //var result = await QuerySettleBetHistoryAsync(_config, startTime, endTime);
                //var tasks = result.Select(item => Task.Run(() => InsertWager(item))).ToArray();
                //await Task.WhenAll(tasks);
                await Task.Delay(1);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "YGG GetWagersAsync Exception.");
                return false;
            }

        }

        private async Task<List<object>> QueryBetRecordAsync(YggConfig config, string startTime, string endTime) {

            await Task.Delay(1);
            return null;
        }
    }

}

