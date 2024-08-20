using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using NuGet.Protocol;
using System.Text;

namespace ThirdPartySeamlessWallet.Controllers;

public class DragonGamingController
{
    #region Fields

    private readonly Tp thisTp;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly IYeebetBLL _yeebetBll;
    private readonly AuthBLL _authBll;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly Tools _tools;
    private readonly Dictionary<int, YeebetConfig> _siteConfigDic;

    #endregion

    #region Constructors

    public DragonGamingController(IServiceProvider provider)
    {
        thisTp = Tp.DragonGaming;
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _authBll = provider.GetRequiredService<AuthBLL>();
        _tools = provider.GetService<Tools>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<YeebetConfig>(thisTp.ToString());
    }

    #endregion

    #region Action

    /// <summary>
    /// 測試API有通
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[controller]/ping")]
    public string PingTest() => "Ping Ok.";

    /// <summary>
    /// GetSession API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/get_session")]
    public SsessionResponse GetSession(CommonTokenRequest request)
    {
        var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(request)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);

        var (token, gameCode) = GetToken(request.token);

        if (_authBll.Authenticate(token, out TpCustInfoAuth tpCustInfoAuth))
        {
            var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
            if (tpCheck == null || tpCheck.Locked)
            {
                return new SsessionResponse(tpCustInfoAuth.TpUserName, token);
            }
            else
            {
                var gameType = (int)_thirdPartyBll.GetGameType((int)thisTp, gameCode);
                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                return new SsessionResponse(tpCustInfoAuth.TpUserName, token, tpCustBalance.Balance, tpCustBalance.TpCurrencyId);
            }
        }
        else
        {
            return new SsessionResponse(token);
        }
    }


    /// <summary>
    /// GetBalance API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/get_balance")]
    public BalanceResponse GetBalance(BalanceRequest request)
    {
        var dicP = JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(request)).OrderBy(kp => kp.Key).ToDictionary(kp => kp.Key, kp => kp.Value);

        var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request.account_id);
        var gameType = _tpCustInfoBll.GetCustGotoGameType(tpCustInfo.CustId, (int)thisTp);
        var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, request.account_id, gameType);
        return new BalanceResponse(request.account_id, request.token, tpCustBalance.Balance, tpCustBalance.TpCurrencyId);
    }

    /// <summary>
    /// Debit API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/debit")]
    public async Task<DepositResponse_Yeebet> DebitAsync([FromForm] DepositRequest_Yeebet request)
    {
        //Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins
        var requestDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.ToJson());

        if (!CheckAppidAndSign(request.appid, requestDic, out string message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        return await _yeebetBll.Deposit(request);
    }

    /// <summary>
    /// Credit API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/credit")]
    public async Task<DepositResponse_Yeebet> CreditAsync([FromForm] DepositRequest_Yeebet request)
    {
        //Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins
        var requestDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.ToJson());

        if (!CheckAppidAndSign(request.appid, requestDic, out string message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        return await _yeebetBll.Deposit(request);
    }

    /// <summary>
    /// Refund API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/refund")]
    public async Task<DeductResponse_Yeebet> RefundAsync([FromForm] DeductRequest_Yeebet request)
    {
        #region rollback test

        //Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins
        //return new DeductResponse_Yeebet(YeebetErrorCode.Success, request.serialnumber, "rollback_test", "",0);

        #endregion

        var requestDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.ToJson());

        if (!CheckAppidAndSign(request.appid, requestDic, out string message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        return await _yeebetBll.Deduct(request);
    }

    #endregion

    #region Private Methods

    private static (string Token, string gameCode) GetToken(string token)
    {
        try
        {
            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return (sessionKeyArr[0], sessionKeyArr[1]);
        }
        catch
        {
            return (null, null);
        }
    }

    private bool CheckAppidAndSign(string appid, Dictionary<string, string> request, out string message)
    {
        message = string.Empty;

        if (!request.ContainsKey("username"))
        {
            message = "username not found.";
            return false;
        }

        var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request["username"]);
        if (tpCustInfo == null)
        {
            message = "TpUserInfo not found.";
            return false;
        }

        if (request.ContainsKey("errmsg"))
            request["errmsg"] = request["errmsg"].Replace(" ", "+");

        string content = string.Join("&",
            request
            .Where(x => x.Key != "bets" && x.Key != "sign")
            .OrderBy(x => x.Key)
            .Select(kp => $"{kp.Key}={kp.Value}").ToArray());

        var config = _siteConfigDic[tpCustInfo.SiteId];
        var currencyData = config.CurrencyData[tpCustInfo.TpCurrencyId];
        string seamlessAppid = currencyData.SeamlessAppId;
        string secretKey = currencyData.SeamlessSecretKey;

        // Check appid
        if (seamlessAppid != appid)
        {
            message = "request.appid is invaild.";
            return false;
        }

        //Check Sign
        if (!request.ContainsKey("sign"))
        {
            message = "request.sign not found.";
            return false;
        }

        var result = _tools.ToMD5($"{content}&key={secretKey}").ToLower();
        if (result != request["sign"])
        {
            message = $"sign is invaild.request.sign={request["sign"]}. correct sign={result}.";
            return false;
        }
        return true;
    }

    #endregion
}
