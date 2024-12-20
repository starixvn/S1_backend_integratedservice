namespace ThirdPartyCore.Model;

public interface IThirdPartyTokenParticular
{
    ApiResult GetToken(TpCustInfo tpcustInfo);

    ApiResult InvalidateToken(TpCustInfo tpCustInfo);

    ApiResult VerifyToken(int siteId, string token);
}