namespace ThirdPartyWager.Service.BLL;

public class CommonBGBLL
{
    internal bool GetSnAndSecretKey(Dictionary<string, string> setting, string tpCurrencyId, out string sn, out string secretKey, out string message)
    {
        sn = string.Empty;
        secretKey = string.Empty;
        message = string.Empty;

        if (!setting.ContainsKey($"Sn_{tpCurrencyId}")
            || string.IsNullOrWhiteSpace(setting[$"Sn_{tpCurrencyId}"]))
        {
            message = $"Sn_{tpCurrencyId} is invaild.";
            return false;
        }
        sn = setting[$"Sn_{tpCurrencyId}"];

        if (!setting.ContainsKey($"ApiSecretKey_{tpCurrencyId}")
            || string.IsNullOrWhiteSpace(setting[$"ApiSecretKey_{tpCurrencyId}"]))
        {
            message = $"ApiSecretKey_{tpCurrencyId} is invaild.";
            return false;
        }
        secretKey = setting[$"ApiSecretKey_{tpCurrencyId}"];

        return true;
    }
}
