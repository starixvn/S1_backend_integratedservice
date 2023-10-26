using Newtonsoft.Json;
using System.Text.Json.Nodes;
using ThirdPartyCore.Extensions;

namespace ThirdParty.Pegasus;

public class PegasusConfig
{
    public string ApiUrl { get; set; }
    public string MerchantID { get; set; }
    public string SecretKey { get; set; }
    public PegasusConfig(Dictionary<string, string> inputs, string tpCurrencyId)
    {
        ApiUrl = inputs.GetValue($"ApiUrl").ToString();
        TpCurrencyConfig currencyConfig = new TpCurrencyConfig();

        if (string.IsNullOrWhiteSpace(tpCurrencyId))
            currencyConfig = JsonConvert.DeserializeObject<TpCurrencyConfig>(inputs.First(x => x.Key.Contains("Currency")).Value);
        else
            currencyConfig = JsonConvert.DeserializeObject<TpCurrencyConfig>(inputs[$"Currency_{tpCurrencyId}"]);

        MerchantID = currencyConfig.MerchantID;
        SecretKey = currencyConfig.SecretKey;
    }
}

public class TpCurrencyConfig
{
    public string Currency { get; set; }
    public string MerchantID { get; set; }
    public string SecretKey { get; set; }
    public string HeaderIdentity { get; set; }
}

public class PegasusGameLaunchResult
{
    public int code { get; set; }
    public string message { get; set; }
    public PegasusGameLaunchData data { get; set; }
}

public class PegasusGameLaunchData
{
    /// <summary>
    /// 网址/链结
    /// </summary>
    public string link { get; set; }
}

public class PegasusGameHistoryResult
{
    public int code { get; set; }
    public string message { get; set; }
    public PegasusGameHistory Data { get; set; }
}

public class PegasusGameHistory
{
    /// <summary>
    /// 网址/链结
    /// </summary>
    public string Url { get; set; }
}