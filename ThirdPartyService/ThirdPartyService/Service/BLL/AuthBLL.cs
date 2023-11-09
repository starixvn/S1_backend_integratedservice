using ThirdPartyCore;

using ThirdPartyService.Service.DAL;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.BLL
{
    public class AuthBLL : IAuthProvider
    {
        private readonly AuthService authService;
        private readonly ITpAuthTokenService _tpAuthTokenService;

        public AuthBLL(AuthService authService, ITpAuthTokenService tpAuthTokenService)
        {
            this.authService = authService;
            this._tpAuthTokenService = tpAuthTokenService;
        }
        public bool ApplyToken(string Key, string Value, int Expiry = 1)
        {
            _tpAuthTokenService.InsertTpAuthToken(new TpAuthToken(Key, Value, Expiry));
            return authService.ApplyToken(Key, Value, Expiry) > 0;
        }
        public string Authenticate(string Key)
        {
            return authService.Authenticate(Key);
        }
    }
}
