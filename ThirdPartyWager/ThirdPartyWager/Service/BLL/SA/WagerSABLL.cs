using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerSABLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiSABLL _apiSABLL;
        private readonly WagerSAService _wagerSAService;
        private readonly ILogger<WagerSABLL> _logger;
        private readonly MySqlClient _wagerDbClient;
        private readonly TpUnsettleService _tpUnsettleService;

        public WagerSABLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiSABLL = provider.GetRequiredService<ApiSABLL>();
            _wagerSAService = provider.GetRequiredService<WagerSAService>();
            _logger = provider.GetRequiredService<ILogger<WagerSABLL>>();
            _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<bool> GetWagersAsync(int tpId, string startTime, string endTime)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await _apiSABLL.GetBetListByTime(setting["WagerUrl"], setting["EncrypKey"], setting["MD5Key"], setting["SecretKey"], startTime, endTime, guid);

                // error condition
                var errorCode = dataWager.GetAllBetDetailsForTimeIntervalResponse?.ErrorMsgId ?? dataWager.APIResponse.ErrorMsgId;
                if (errorCode != 0)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 0 && dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                var groups = dataWager.GetAllBetDetailsForTimeIntervalResponse.BetDetailList.BetDetail.GroupBy(i =>
                    (i.Username, i.GameID));

                // insert wager to db
                foreach (var group in groups)
                {
                    await using var conn = _wagerDbClient.GetMySqlConnection();
                    conn.Open();
                    await using var trans = await conn.BeginTransactionAsync();

                    try
                    {
                        foreach (var bet in group)
                        {
                            bet.GameResult = JsonConvert.SerializeObject(bet.GameResult);
                        }

                        var result = _wagerSAService.InsertWager(group, trans);
                        if (result == group.Count())
                        {
                            if (_tpUnsettleService.InsertTpUnsettle(Tp.SA, GameType.SA_LiveCasino,
                                    $"{group.Key.GameID}_{group.Key.Username}", trans) > 0)
                            {
                                trans.Commit();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {group.Key.GameID}_{group.Key.Username} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(group)}\r\n{ex.Message}");
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
