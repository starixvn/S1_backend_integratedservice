using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Http;
using NuGet.Common;
using Org.BouncyCastle.Crypto.Tls;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public interface IPNGBLL
    {
        PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest);

        PNG_BalanceResponse Balance(PNG_BalanceRequest balanceRequest);
    }

    public class PNGBLL : IPNGBLL
    {
        public Tp TpId => Tp.PNG;
        public GameType TpGameType => GameType.PNG_RNG;

        private readonly AuthBLL _authBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly ILogger<PNGBLL> _logger;

        public PNGBLL(AuthBLL authBll, ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBll,
            ILogger<PNGBLL> logger)
        {
            _authBll = authBll;
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBll = tpCustInfoBll;
            _logger = logger;
        }


        public PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest)
        {
            // 檢查 AuthToken
            if (!_authBll.Authenticate(authenticateRequest.AuthToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.NoUser,
                    StatusMessage = "AuthToken Expired",
                };
            }

            // 檢查 PlayNGo AccessToken
            if (!ValidToken(authenticateRequest.AccessToken))
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
            var userCurrency = this.GetCurrencyIsoCode(tpCountryCode.TpCurrencyId);
            
            return new PNG_AuthenticateResponse()
            {
                TpUserName = tpCustInfoAuth.TpUserName,
                StatusCode = (int)PNG_StatsuCode.Ok,
                StatusMessage = "Success",
                UserCurrency = userCurrency,
                Country = tpCountryCode.CountryCode,
                Birthdate = new DateTime(1970, 1, 1), // 給default
                Registration = new DateTime(1970, 1, 1), // 給default
                Real = tpCustBalance.Balance
            };
        }

        public PNG_BalanceResponse Balance(PNG_BalanceRequest balanceRequest)
        {
            // check playNGo accessToken
            if (!ValidToken(balanceRequest.AccessToken))
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.Internal,
                    StatusMessage = "AccessToken not equal",
                    Real = default
                };
            }

            // check user exist
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, balanceRequest.TpUserName);
            if (tpCustInfo == null)
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.Internal,
                    StatusMessage = "tpCustInfo not found user not exist",
                    Real = default
                };
            }

            // check player currency 是否正確
            var userCurrency = GetCurrencyIsoCode(tpCustInfo.TpCurrencyId);
            if (userCurrency != balanceRequest.Currency)
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCode.Internal,
                    StatusMessage = $"{balanceRequest.TpUserName} Currency wrong",
                    Real = default
                };
            }

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)TpId, balanceRequest.TpUserName, (int)TpGameType);
            return new PNG_BalanceResponse
            {
                StatusCode = (int)PNG_StatsuCode.Ok,
                StatusMessage = "Success",
                Real = tpCustBalance.Balance
            };
        }

        #region private method

        private bool ValidToken(string inputAccessToken)
        {
            var tpListSetting = _thirdPartyBLL.GetThirdPartySetting((int)TpId);
            var accessToken = tpListSetting.TpSetting.ContainsKey("AccessToken")
                ? tpListSetting.TpSetting["AccessToken"]
                : string.Empty;

            if (!string.IsNullOrEmpty(accessToken)) return accessToken == inputAccessToken;

            _logger.LogError("thirdPartySetting not found");
            return false;
        }

        private string GetCurrencyIsoCode(string tpCurrencyId)
        {
            var tpCurrencyOfIsoCode = _thirdPartyBLL.GeTpCurrencyOfIsoCode((int)TpId);

            if (tpCurrencyOfIsoCode.ContainsKey(tpCurrencyId)) return tpCurrencyOfIsoCode[tpCurrencyId];

            _logger.LogError("tpCurrencyOfIsoCode not found");
            return string.Empty;
        }

        #endregion
    }
}
