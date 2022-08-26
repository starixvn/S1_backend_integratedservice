using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
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
		private readonly TpUnsettleService _tpUnsettleService;
		private Dictionary<string, string> liveaCasino;
		private Dictionary<string, string> fishing;

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
			_tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
			liveaCasino = new Dictionary<string, string>();
			fishing = new Dictionary<string, string>();
			liveaCasino.Add("sw_ro_a01bac", "sw_ro_a01bac");
			liveaCasino.Add("sw_ro_a01bac_nc", "sw_ro_a01bac_nc");
			liveaCasino.Add("sw_ro_a04bac", "sw_ro_a04bac");
			liveaCasino.Add("sw_ro_a04bac_nc", "sw_ro_a04bac_nc");
			liveaCasino.Add("sw_ro_a07bac", "sw_ro_a07bac");
			liveaCasino.Add("sw_ro_a07bac_nc", "sw_ro_a07bac_nc");
			liveaCasino.Add("sw_ro_a01ro", "sw_ro_a01ro");
			liveaCasino.Add("sw_ro_a03ro", "sw_ro_a03ro");
			liveaCasino.Add("sw_live_erol_atom", "sw_live_erol_atom");
			fishing.Add("sw_or", "sw_or");
			fishing.Add("sw_fj", "sw_fj");
			fishing.Add("sw_cashdaha", "sw_cashdaha");
		}

		public async Task<bool> GetWagersAsync(int tpId, string apiUrl, string secretKey, string username, string password, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
			try
            {
				var dataWager = await _apiSkywindBLL.GetBetListByTime(apiUrl, secretKey, username, password, startTime, endTime);

				foreach (var bet in dataWager)
                {
                    try
                    {
						var betSkywind = new BetSkywind()
						{
							finished = bet.finished ? 1 : 0,
							revenue = bet.revenue,
							currency = bet.currency,
							debit = bet.debit.HasValue ? bet.debit.Value : 0,
							credit = bet.credit.HasValue ? bet.credit.Value : 0,
							roundId = bet.roundId,
							brandId = bet.brandId,
							playerCode = bet.playerCode,
							gameCode = bet.gameCode,
							device = bet.device,
							win = bet.win,
							bet = bet.bet,
							balanceAfter = bet.balanceAfter,
							balanceBefore = bet.balanceBefore,
							totalEvents = bet.totalEvents,
							firstTs = Convert.ToDateTime(bet.firstTs),
							ts = Convert.ToDateTime(bet.ts),
							isTest = bet.isTest ? 1 : 0,
							insertedAt = Convert.ToDateTime(bet.insertedAt),
							recoveryType = bet.recoveryType,
							totalJpContribution = bet.totalJpContribution,
							totalJpWin = bet.totalJpWin,
						};

						var result = _apiSkywindBLL.InsertWager(betSkywind);

						if (result > 0)
						{
							var gameType = GameType.Skywind_RNG;
							if (fishing.ContainsKey(bet.gameCode))
							{
								gameType = GameType.Skywind_Fish;
							}
							else if (liveaCasino.ContainsKey(bet.gameCode))
							{
								gameType = GameType.Skywind_LiveCasino;
							}
							_tpUnsettleService.InsertTpUnsettle(Tp.Skywind, gameType, bet.roundId);
						}
					}
					catch (Exception ex)
                    {
						msg = $"INSERT_WAGER(WAGERID: {bet.roundId} )";
						_logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
						return false;
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

