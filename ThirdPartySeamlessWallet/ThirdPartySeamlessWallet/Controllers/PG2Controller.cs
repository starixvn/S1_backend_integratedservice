using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers;
[ApiController]
public class PG2Controller : ControllerBase
{
    #region Fields

    private readonly IPG2BLL _PG2Bll;

    #endregion

    #region Constructors

    public PG2Controller(IServiceProvider provider)
    {
        _PG2Bll = provider.GetService<IPG2BLL>();
    }

    #endregion
    /// <summary>
    /// 測試API有通
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[controller]/ping")]
    public string PingTest() => "Ping Ok.";

    /// <summary>
    /// GetBalance API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/seamless/balance")]
    public BalanceResponse_PG2 GetBalanceAsync(CommonRequest_PG2 request)
    {
        return _PG2Bll.GetBalance(request);
    }

    /// <summary>
    /// Bet API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/seamless/bet")]
    public async Task<BetResponse_PG2> BetAsync(BetRequest_PG2 request)
    {
        return await _PG2Bll.Bet(request);
    }

    /// <summary>
    /// Payout API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/seamless/payout")]
    public async Task<PayoutResponse_PG2> PayoutAsync(PayoutRequest_PG2 request)
    {
        return await _PG2Bll.Payout(request);
    }

    /// <summary>
    /// Cancel API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/seamless/cancel")]
    public async Task<CancelResponse_PG2> CancelAsync(CancelRequest_PG2 request)
    {
        return await _PG2Bll.Cancel(request);
    }

    /// <summary>
    /// Settle API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/seamless/settlement")]
    public async Task<SettleResponse_PG2> SlotSettleAsync(SettlementRequest_PG2 request)
    {
        return await _PG2Bll.Settlement(request);
    }
}
