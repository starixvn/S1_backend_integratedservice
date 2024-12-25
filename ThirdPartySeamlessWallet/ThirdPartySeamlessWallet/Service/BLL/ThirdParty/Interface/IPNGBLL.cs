using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

public interface IPNGBLL
{
    /// <summary>
    /// 驗證
    /// </summary>
    /// <param name="authenticateRequest"></param>
    /// <returns></returns>
    PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest, string sitename);

    /// <summary>
    /// 取餘額
    /// </summary>
    /// <param name="balanceRequest"></param>
    /// <returns></returns>
    PNG_BalanceResponse Balance(PNG_BalanceRequest balanceRequest, string sitename);

    /// <summary>
    /// 下注
    /// </summary>
    /// <param name="reserveRequest"></param>
    /// <returns></returns>
    Task<PNG_ReserveResponse> Reserve(PNG_ReserveRequest reserveRequest, string sitename);

    /// <summary>
    /// 取消下注
    /// </summary>
    /// <param name="cancelReserveRequest"></param>
    /// <returns></returns>
    Task<PNG_CancelReserveResponse> CancelReserve(PNG_CancelReserveRequest cancelReserveRequest, string sitename);

    /// <summary>
    /// 派彩
    /// </summary>
    /// <param name="releaseRequest"></param>
    /// <returns></returns>
    Task<PNG_ReleaseResponse> Release(PNG_ReleaseRequest releaseRequest, string sitename);
}