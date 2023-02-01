using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class YeebetController : ControllerBase
{
    #region Fields

    private readonly Tp thisTp;

    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly IYeebetBLL _yeebetBll;

    #endregion

    #region Constructors

    public YeebetController(IServiceProvider provider)
    {
        thisTp = Tp.Yeebet;

        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _yeebetBll = provider.GetService<IYeebetBLL>();
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
    public async Task<BalanceResponse_Yeebet> BalanceAsync([FromForm] BalanceRequest_Yeebet request)
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
        return new DepositResponse_Yeebet("", "deposit test", 0, 1000);
    }

    /// <summary>
    /// 2.8.3 商户用户扣款接口（/api/deduct）
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/withdraw")]
    public async Task<DeductResponse_Yeebet> WithdrawAsync([FromForm] DeductRequest_Yeebet request)
    {
        return new DeductResponse_Yeebet("", "", "withdraw test", 0, 1000);
    }

    /// <summary>
    /// 2.8.5 商户用户回滚接口（/api/rollback）
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/rollback")]
    public async Task<RollbackResponse_Yeebet> RollbackAsync([FromForm] RollbackRequest_Yeebet request)
    {
        return new RollbackResponse_Yeebet("rollback test", 0, 1000);
    }

    #endregion
}
