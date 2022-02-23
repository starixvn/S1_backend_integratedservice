using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class WagerV8BLL
    {
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly ApiV8BLL _apiV8BLL;
        private readonly WagerV8Service _wagerV8Service;
        private readonly ILogger<WagerV8BLL> _logger;

        private readonly int _tpId = (int)Tp.V8;

        public WagerV8BLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _apiV8BLL = provider.GetRequiredService<ApiV8BLL>();
            _wagerV8Service = provider.GetRequiredService<WagerV8Service>();
            _logger = provider.GetRequiredService<ILogger<WagerV8BLL>>();
        }

        public async Task<bool> GetWagersAsync(int tpId, long startUnix, long endUnix)
        {
            var msg = "FATAL_ERROR";
            var guid = Guid.NewGuid().ToString();
            try
            {
                // get api info
                var setting = _thirdPartyBLL.GetThirdPartySetting(tpId);

                // get wager
                var dataWager = await _apiV8BLL.GetBetListByTime(setting["WagerUrl"], setting["Agent"], setting["Deskey"], setting["Md5key"], startUnix, endUnix, guid);

                // error condition
                var errorCode = dataWager.d.code;
                var errorMsg = dataWager.d.code.ToString();

                if (errorCode != 0 && errorCode != 16)
                {
                    msg = "THIRD_PARTY_ERROR";
                    _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return false;
                }

                if (errorCode == 16 || (errorCode == 0 && dataWager.d.count == 0))
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(dataWager)}");
                    return true;
                }

                var field = new List<string>();
                for (var i = 0; i < dataWager.d.count; i++)
                {
                    var keyValuePairs = new Dictionary<string, dynamic>
                    {
                        { "GameID", dataWager.d.list.GameID[i] },
                        { "Accounts", dataWager.d.list.Accounts[i] },
                        { "ServerID", dataWager.d.list.ServerID[i] },
                        { "KindID", dataWager.d.list.KindID[i] },
                        { "TableID", dataWager.d.list.TableID[i] },
                        { "ChairID", dataWager.d.list.ChairID[i] },
                        { "UserCount", dataWager.d.list.UserCount[i] },
                        { "CellScore", dataWager.d.list.CellScore[i] },
                        { "AllBet", dataWager.d.list.AllBet[i] },
                        { "Profit", dataWager.d.list.Profit[i] },
                        { "Revenue", dataWager.d.list.Revenue[i] },
                        { "GameStartTime", dataWager.d.list.GameStartTime[i] },
                        { "GameEndTime", dataWager.d.list.GameEndTime[i] },
                        { "CardValue", dataWager.d.list.CardValue[i] },
                        { "ChannelID", dataWager.d.list.ChannelID[i] },
                        { "LineCode", dataWager.d.list.LineCode[i] }
                    };
                    field.Add(JsonConvert.SerializeObject(keyValuePairs));
                }

                foreach (var item in field)
                {

                    var bet = JsonConvert.DeserializeObject<BetV8>(item);
                    try
                    {
                        _wagerV8Service.InsertWager(bet);
                    }
                    catch (Exception ex)
                    {
                        msg = $"INSERT_WAGER(WAGERID: {bet.GameID} )";
                        _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                        return false;
                    }
                }

                msg = "FINISH";
                _logger.LogInformation($"GetWagersAsync {msg}\r\n{startUnix} ~ {endUnix}");
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
