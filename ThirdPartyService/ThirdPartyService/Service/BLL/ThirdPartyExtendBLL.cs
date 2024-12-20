using Microsoft.Extensions.Caching.Memory;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.BLL;

public class ThirdPartyExtendBLL
{
    #region Fields

    private readonly ThirdPartyHelper _thirdPartyHelper;
    private readonly IMemoryCache _cache;
    private readonly MySqlClient _mainDbClient;
    private readonly TpCustInfoService _tpCustInfoService;

    #endregion

    #region Constructors

    public ThirdPartyExtendBLL(IServiceProvider provider)
    {
        _thirdPartyHelper = provider.GetRequiredService<ThirdPartyHelper>();
        _cache = provider.GetRequiredService<IMemoryCache>();
        _tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
    }

    #endregion

    public ApiResult<GetTokenResult> GetToken(int custId, int tpId)
    {
        var apiResult = new ApiResult<GetTokenResult>();

        var tpCustInfo = _tpCustInfoService.GetTpCustInfoFromCacheOrDb(custId, tpId);

        if (tpCustInfo == null)
        {
            apiResult.ErrorCode = ErrorCode.UserNotExist;
            return apiResult;
        }
        if (tpCustInfo.Status is (int)CustStatus.Closed or (int)CustStatus.Suspend ||
                  tpCustInfo.CStatus is (int)CustStatus.Closed or (int)CustStatus.Suspend)
        {
            apiResult.ErrorCode = ErrorCode.UserLocked;
            apiResult.Message = $"CustId={tpCustInfo.CustId},Status={tpCustInfo.Status},CStatus={tpCustInfo.CStatus}";
            return apiResult;
        }

        var tpHelper = (IThirdPartyTokenParticular)_thirdPartyHelper.GetThirdPartyHelper(tpId, tpCustInfo.SiteId);

        var result = tpHelper.GetToken(tpCustInfo);

        if (result.ErrorCode == ErrorCode.Success)
        {
            apiResult.ErrorCode = ErrorCode.Success;
            apiResult.Data = new GetTokenResult(result.Message);
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }
        else
        {
            apiResult.ErrorCode = result.ErrorCode;
            apiResult.Message = result.Message;
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }

        return apiResult;
    }

    public ApiResult InvalidateToken(int custId, int tpId)
    {
        var apiResult = new ApiResult();

        var tpCustInfo = _tpCustInfoService.GetTpCustInfoFromCacheOrDb(custId, tpId);

        if (tpCustInfo == null)
        {
            apiResult.ErrorCode = ErrorCode.UserNotExist;
            return apiResult;
        }
        if (tpCustInfo.Status is (int)CustStatus.Closed or (int)CustStatus.Suspend ||
                  tpCustInfo.CStatus is (int)CustStatus.Closed or (int)CustStatus.Suspend)
        {
            apiResult.ErrorCode = ErrorCode.UserLocked;
            apiResult.Message = $"CustId={tpCustInfo.CustId},Status={tpCustInfo.Status},CStatus={tpCustInfo.CStatus}";
            return apiResult;
        }

        var tpHelper = (IThirdPartyTokenParticular)_thirdPartyHelper.GetThirdPartyHelper(tpId, tpCustInfo.SiteId);

        var result = tpHelper.InvalidateToken(tpCustInfo);

        if (result.ErrorCode == ErrorCode.Success)
        {
            apiResult.ErrorCode = ErrorCode.Success;
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }
        else
        {
            apiResult.ErrorCode = result.ErrorCode;
            apiResult.Message = result.Message;
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }

        return apiResult;
    }

    public ApiResult<VerifyTokenResult> VerifyToken(int siteId, int tpId, string token)
    {
        var apiResult = new ApiResult<VerifyTokenResult>();

        var tpHelper = (IThirdPartyTokenParticular)_thirdPartyHelper.GetThirdPartyHelper(tpId, siteId);

        var result = tpHelper.VerifyToken(siteId, token);

        if (result.ErrorCode == ErrorCode.Success)
        {
            apiResult.ErrorCode = ErrorCode.Success;
            apiResult.Data = new VerifyTokenResult(result.Message);
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }
        else
        {
            apiResult.ErrorCode = result.ErrorCode;
            apiResult.Message = result.Message;
            apiResult.RawMessage = result.RawMessage;
            apiResult.RawErrorCode = result.RawErrorCode;
        }

        return apiResult;
    }
}