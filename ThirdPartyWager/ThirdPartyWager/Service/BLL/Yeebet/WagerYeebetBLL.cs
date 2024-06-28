using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class WagerYeebetBLL
{
    private readonly ApiYeebetBLL _apiYeebetBLL;
    private readonly WagerYeebetService _wagerYeebetService;
    private readonly ILogger<WagerYeebetBLL> _logger;
    private readonly int size = 1000; //預設一次撈1000筆資料
    private readonly Dictionary<int, YeebetConfig> _siteConfigDic;

    public WagerYeebetBLL(IServiceProvider provider)
    {
        _apiYeebetBLL = provider.GetRequiredService<ApiYeebetBLL>();
        _wagerYeebetService = provider.GetRequiredService<WagerYeebetService>();
        _logger = provider.GetRequiredService<ILogger<WagerYeebetBLL>>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YeebetConfig>(Tp.Yeebet.ToString());
    }

    public async Task<(bool isSuccess, bool isContinue)> GetWagersAsync(int tpId, int siteId, string tpCurrencyId)
    {
        var wagerIds = new List<long>();
        var msg = "FATAL_ERROR";
        try
        {
            // get tp info for api
            var config = _siteConfigDic[siteId];
            var currencyData = config.CurrencyData[tpCurrencyId];

            if (!GetAppIdAndKey(currencyData, out string appid, out string key))
            {
                msg = "THIRD_PARTY_SETTING_ERROR";
                _logger.LogError($"{msg}");
                return (isSuccess: false, isContinue: false);
            }

            // get history bets
            var getResult = await _apiYeebetBLL.Getbets(config.ApiUrl, appid, size, key);

            if (getResult == null || getResult.result != 0)
            {
                msg = "THIRD_PARTY_GET_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(getResult)}");
                return (isSuccess: false, isContinue: false);
            }

            if (getResult.array == null || getResult.array.Count() == 0)
            {
                msg = "NO_DATA";
                _logger.LogInformation($"{msg}\r\n{JsonConvert.SerializeObject(getResult)}");
                return (isSuccess: true, isContinue: false);
            }

            var finishData = getResult.array.Where(x => x.state != 0); //state:-2=撤銷,-1=廢棄，0=投注，1=结算

            // insert wager to db
            foreach (var bet in finishData)
            {
                try
                {
                    await _wagerYeebetService.InsertWager(bet);
                    wagerIds.Add(bet.id);
                }
                catch (Exception ex)
                {
                    msg = $"INSERT_WAGER(WAGERID: {bet.id} )";
                    _logger.LogError(ex, $"{msg}\r\n{JsonConvert.SerializeObject(bet)}\r\n{ex.Message}");
                    return (isSuccess: false, isContinue: false);
                }
            }

            // remove wager
            var removeResult = await _apiYeebetBLL.Removebets(config.ApiUrl, appid, string.Join(",", wagerIds), key);

            if (removeResult.result != 0)
            {
                msg = "THIRD_PARTY_REMOVE_ERROR";
                _logger.LogError($"{msg}\r\n{JsonConvert.SerializeObject(removeResult)}");
                return (isSuccess: false, isContinue: false);
            }

            msg = "FINISH";
            _logger.LogInformation($"GetWagersAsync {msg}");
            return (isSuccess: true, isContinue: getResult.array.Count() == size);
        }
        catch (Exception ex)
        {
            msg = "FATAL_ERROR";
            _logger.LogError(ex, $"GetWagersAsync {msg}\r\n{ex.Message}");
            return (isSuccess: false, isContinue: false);
        }
    }

    private bool GetAppIdAndKey(CurrencyDataYeebet setting, out string appid, out string key)
    {
        appid = string.Empty;
        key = string.Empty;
        if (string.IsNullOrWhiteSpace(setting.ApiAppId))
        {
            _logger.LogError($"ApiAppId is invaild.");
            return false;
        }
        if (string.IsNullOrWhiteSpace(setting.ApiSecretKey))
        {
            _logger.LogError($"ApiSecretKey is invaild.");
            return false;
        }

        appid = setting.ApiAppId;
        key = setting.ApiSecretKey;
        return true;
    }
}
