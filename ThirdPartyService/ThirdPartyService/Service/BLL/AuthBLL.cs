using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL
{
    public class AuthBLL : IAuthProvider
    {
        private readonly AuthService authService;

        public AuthBLL(AuthService authService)
        {
            this.authService = authService;
        }
        public bool ApplyToken(string Key, string Value, int Expiry = 1)
        {
            return authService.ApplyToken(Key, Value, Expiry) > 0;
        }
        public string Authenticate(string Key)
        {
            return authService.Authenticate(Key);
        }

        public int KmUserTokenSet(string userName, string token, DateTime expiredTime)
        {
            return authService.KmUserTokenSet(userName, token, expiredTime);
        }
    }
}
