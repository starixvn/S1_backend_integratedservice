using NuGet.Common;
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
        public readonly Tp _ThirdParty;
        public readonly GameType _GameType;

        private readonly AuthBLL _authBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;

        public PNGBLL(AuthBLL authBll, ThirdPartyBLL thirdPartyBLL)
        {
            _ThirdParty = Tp.PNG;
            _GameType = GameType.PNG_RNG;

            _authBll = authBll;
            _thirdPartyBLL = thirdPartyBLL;
        }


        public PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest)
        {
            if (!_authBll.Authenticate(authenticateRequest.Username, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = PNG_StatsuCode.NoUser,
                    StatusMessage = "token Expired",
                };
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)_ThirdParty, tpCustInfoAuth.TpUserName);

            if (tpCheck == null || tpCheck.Locked)
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = PNG_StatsuCode.AccountLocked,
                    StatusMessage = "User Account Locked",
                };
            }

            return new PNG_AuthenticateResponse();

        }
    }
}
