using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class DragonGamingController : ControllerBase
{
    #region Fields

    private readonly IDragonGamingBLL _dragonGamingBll;

    #endregion

    #region Constructors

    public DragonGamingController(IServiceProvider provider)
    {
        _dragonGamingBll = provider.GetService<IDragonGamingBLL>();
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
    public async Task<SsessionResponse_DragonGaming> GetSessionAsync(SessionRequest_DragonGaming request)
    {
        return await _dragonGamingBll.GetSession(request);
    }

    /// <summary>
    /// GetBalance API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/get_balance")]
    public async Task<BalanceResponse_DragonGaming> GetBalanceAsync(BalanceRequest_DragonGaming request)
    {
        return await _dragonGamingBll.GetBalance(request);
    }

    /// <summary>
    /// Debit API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/debit")]
    public async Task<DebitCreditRefundResponse_DragonGaming> DebitAsync(DebitRequest_DragonGaming request)
    {
        return await _dragonGamingBll.Debit(request);
    }

    /// <summary>
    /// Credit API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/credit")]
    public async Task<DebitCreditRefundResponse_DragonGaming> CreditAsync(CreditRequest_DragonGaming request)
    {
        return await _dragonGamingBll.Credit(request);
    }

    /// <summary>
    /// Refund API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/refund")]
    public async Task<DebitCreditRefundResponse_DragonGaming> RefundAsync(RefundRequest_DragonGaming request)
    {
        return await _dragonGamingBll.Refund(request);
    }

    #endregion
}
