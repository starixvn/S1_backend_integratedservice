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
	public class WagerSkywindBLL
	{
		private readonly ThirdPartyBLL _thirdPartyBLL;
		private readonly ILogger<WagerSkywindBLL> _logger;
		private readonly HttpHelper _httpHelper;
		private readonly Common _common;
		private readonly MySqlClient _wagerDbClient;
		private readonly SkywindConfig _config;
		private readonly ApiSkywindBLL _apiSkywindBLL;
		private const int TpId = (int)Tp.Skywind;
		private const int SuccessCode = 0;

		public WagerSkywindBLL(IServiceProvider provider)
		{
			_logger = provider.GetRequiredService<ILogger<WagerSkywindBLL>>();
			_httpHelper = provider.GetRequiredService<HttpHelper>();
			_common = provider.GetRequiredService<Common>();
			_wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
			_thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
			_apiSkywindBLL = provider.GetRequiredService<ApiSkywindBLL>();
			var setting = _thirdPartyBLL.GetThirdPartySetting(TpId);
			_config = new SkywindConfig(setting);
		}

		public async Task<bool> GetWagersAsync(int tpId, string apiUrl, string secretKey, string username, string password, string startTime, string endTime)
        {
            try
            {
				var dataWager = await _apiSkywindBLL.GetBetListByTime(apiUrl, secretKey, username, password, startTime, endTime);
			}
			catch (Exception ex)
            {

            }
			return true;
        }
	}
}

