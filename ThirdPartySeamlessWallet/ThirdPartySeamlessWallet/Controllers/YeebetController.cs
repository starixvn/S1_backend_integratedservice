using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Library;
using System.Text;
using NuGet.Protocol;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class YeebetController : ControllerBase
{
    #region Fields

    private readonly Tp thisTp;

    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly IYeebetBLL _yeebetBll;
    private readonly Tools _tools;

    #endregion

    #region Constructors

    public YeebetController(IServiceProvider provider)
    {
        thisTp = Tp.Yeebet;

        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _yeebetBll = provider.GetService<IYeebetBLL>();
        _tools = provider.GetService<Tools>();
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
    /// 2.8.2 商户用户金额查询接口（/api/balance）
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[controller]/balance")]
    public async Task<BalanceResponse_Yeebet> BalanceAsync([FromQuery] BalanceRequest_Yeebet request)
    {
        return await _yeebetBll.Balance(request.username);
    }

    /// <summary>
    /// 2.8.4 商户用户入款接口（/api/deposit）
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/deposit")]
    public async Task<DepositResponse_Yeebet> DepositAsync([FromForm] DepositRequest_Yeebet request)
    {
        Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins
        var requestDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.ToJson());

        if (!CheckAppidAndSign(request.appid, requestDic, out string message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        return await _yeebetBll.Deposit(request);
    }

    /// <summary>
    /// 2.8.3 商户用户扣款接口（/api/deduct）
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/withdraw")]
    public async Task<DeductResponse_Yeebet> DeductAsync([FromForm] DeductRequest_Yeebet request)
    {
        Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins
        var requestDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.ToJson());

        if (!CheckAppidAndSign(request.appid, requestDic, out string message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        return await _yeebetBll.Deduct(request);
    }

    /// <summary>
    /// 2.8.5 商户用户回滚接口（/api/rollback）
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/rollback")]
    public async Task<RollbackResponse_Yeebet> RollbackAsync([FromForm] RollbackRequest_Yeebet request)
    {
        return new RollbackResponse_Yeebet(YeebetErrorCode.Success, "rollback test", 1000);
    }

    #endregion

    #region Private Methods

    private bool CheckAppidAndSign(string appid, Dictionary<string, string> request, out string message)
    {
        message = string.Empty;

        string content = string.Join("&",
            request
            .Where(x => x.Key != "bets" && x.Key != "sign")
            .OrderBy(x => x.Key)
            .Select(kp => $"{kp.Key}={kp.Value}").ToArray());

        // Check appid
        var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(_thirdPartyBLL.GetThirdPartySetting((int)thisTp).Setting);
        string seamlessAppid = setting.ContainsKey("SeamlessAppId") && !string.IsNullOrEmpty(setting["SeamlessAppId"])
            ? setting["SeamlessAppId"]
            : throw new YeebetException(YeebetErrorCode.SystemError, "SeamlessAppId is Invaild.");

        if (seamlessAppid != appid)
        {
            message = "appid is invaild.";
            return false;
        }

        //Check Sign
        if (!request.ContainsKey("sign"))
        {
            message = "sign not found.";
            return false;
        }

        string secretKey = setting.ContainsKey("SeamlessSecretKey") && !string.IsNullOrEmpty(setting["SeamlessSecretKey"])
            ? setting["SeamlessSecretKey"]
            : throw new YeebetException(YeebetErrorCode.SystemError, "SeamlessSecretKey is Invaild.");

        var result = _tools.ToMD5($"{content}&key={secretKey}").ToLower();
        if (result != request["sign"])
        {
            message = "sign is invaild.";
            return false;
        }
        return true;
    }

    #endregion
}
