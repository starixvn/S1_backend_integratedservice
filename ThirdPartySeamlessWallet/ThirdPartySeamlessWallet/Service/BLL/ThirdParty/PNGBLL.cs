using Microsoft.AspNetCore.Http;
using NuGet.Common;
using Org.BouncyCastle.Crypto.Tls;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public interface IPNGBLL
    {
        PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest);
    }

    public class PNGBLL : IPNGBLL
    {
        public Tp TpId => Tp.PNG;
        public GameType TpGameType => GameType.PNG_RNG;

        private readonly AuthBLL _authBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBll;

        public PNGBLL(AuthBLL authBll, ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBll)
        {
            _authBll = authBll;
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBll = tpCustInfoBll;
        }


        public PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest)
        {
            // 檢查 AuthToken
            if (!_authBll.Authenticate(authenticateRequest.Username, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.NoUser,
                    StatusMessage = "AuthToken Expired",
                };
            }

            // 檢查 PlayNGo 給的AccessToken 是否是同一個Key
            var tpListSetting = _thirdPartyBLL.GetThirdPartySetting((int)TpId);
            var accessToken = tpListSetting.TpSetting.ContainsKey("AccessToken")
                ? tpListSetting.TpSetting["AccessToken"]
                : string.Empty;

            if (string.IsNullOrEmpty(accessToken) || accessToken != authenticateRequest.AccessToken)
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.Internal,
                    StatusMessage = "AccessToken Invalid",
                };
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)TpId, tpCustInfoAuth.TpUserName);

            if (tpCheck == null || tpCheck.Locked)
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.AccountLocked,
                    StatusMessage = "User Account Locked",
                };
            }

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)TpId, tpCustInfoAuth.TpUserName, (int)TpGameType);
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, tpCustInfoAuth.TpUserName);
            var tpCountryCode = _tpCustInfoBll.GetTpCurrencyCode((int)TpId, tpCustInfo.CurrencyId);
            var tpCurrencyOfIsoCode = _thirdPartyBLL.GeTpCurrencyOfIsoCode((int)TpId);

            return new PNG_AuthenticateResponse()
            {
                ExternalId = tpCustInfoAuth.TpUserName,
                StatusCode = (int)PNG_StatsuCode.Ok,
                StatusMessage = "Success",
                UserCurrency = tpCurrencyOfIsoCode[tpCountryCode.TpCurrencyId],
                Country = tpCountryCode.CountryCode,
                Birthdate = new DateTime(1970, 1, 1), // 給default
                Registration = new DateTime(1970, 1, 1), // 給default
                Real = tpCustBalance.Balance
            };
        }
    }
}
