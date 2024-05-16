using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class WagerAMBBLL
{
    private readonly ApiAMBBLL _apiAMBBLL;
    private readonly WagerAMBService _wagerAMBService;
    private readonly ILogger<WagerAMBBLL> _logger;
    private readonly CommonBLL _commonBLL;
    private readonly int _tpId = (int)Tp.AMB;
    private readonly TpUnsettleService _tpUnsettleService;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly IEnumerable<TpCurrency> _tpCurrencyList;


    public WagerAMBBLL(IServiceProvider provider)
    {
        _apiAMBBLL = provider.GetRequiredService<ApiAMBBLL>();
        _commonBLL = provider.GetRequiredService<CommonBLL>();
        _wagerAMBService = provider.GetRequiredService<WagerAMBService>();
        _logger = provider.GetRequiredService<ILogger<WagerAMBBLL>>();
        _tpUnsettleService = provider.GetRequiredService<TpUnsettleService>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCurrencyList = _commonBLL.GetTpCurrencyList(_tpId);
    }
    
    public async Task<dynamic> GetWagerAsync(string startTime, string endTime)
    {
        var msg = "FATAL_ERROR";
        var ambGameTypeList = new List<int>
        {
            (int)GameType.AMB_Poker,
            (int)GameType.AMB_RNG
        };

        foreach (var gameType in ambGameTypeList)
        {
            foreach (var item in _tpCurrencyList)
            {
                var currency = item.TpCurrencyId;
                if (gameType == (int)GameType.AMB_Poker && (currency == "USD" || currency == "CNY"))
                {
                    continue;
                }
                
                var getWager = await _apiAMBBLL.GetBetListByTime(startTime, endTime,currency, gameType);
                if (getWager.status.code != 0)
                {
                    if (getWager.status.code != 997)
                    {
                        msg = "THIRD_PARTY_ERROR";
                        _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getWager)}");
                        return false;
                    }
                }
                            
                if (getWager.data.totalRecords == 0 || getWager.data.result == null)
                {
                    msg = "NO_DATA";
                    _logger.LogInformation($"AMB GetWager:{msg}\r\nGameType:{(GameType)gameType},Currency:{currency}\r\n{JsonConvert.SerializeObject(getWager)}");
                    continue;
                }
                
                var allBetRecords = getWager.data.result
                    .SelectMany(user => user.result)
                    .ToList();
                
                foreach (var wager in allBetRecords)
                {
                    var result = InsertNewAMBWager(wager,gameType);
                }
            }
        }
        
        return true;
    }
    
    private int InsertNewAMBWager(betRecord_AMB wagerData,int gameType)
    {
        var result = _wagerAMBService.InsertAMBWager(wagerData,gameType);
        if (result == 1)
        {
            _ = gameType switch
            {
                (int)GameType.AMB_Poker => _tpUnsettleService.InsertTpUnsettle(Tp.AMB, (GameType)gameType,
                    $"{wagerData.roundId}_{wagerData.username}"),
                (int)GameType.AMB_RNG => _tpUnsettleService.InsertTpUnsettle(Tp.AMB, (GameType)gameType,
                    wagerData.roundId),
                _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
            };
        }

        return result;
    }
}