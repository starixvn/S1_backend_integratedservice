using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService authService;

        public AuthBLL(AuthService authService)
        {
            this.authService = authService;
        }
        public string ApplyToken<T>(T t)
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            if (authService.ApplyToken(GuidString, Newtonsoft.Json.JsonConvert.SerializeObject(t), 1) > 0)
            {
                return GuidString;
            }
            else
            {
                return null;
            }
        }
        public bool Authenticate<T>(string sToken, out T t)
        {
            string sPermissionData = "";
            t = default;
            sPermissionData = authService.Authenticate(sToken);

            if (!string.IsNullOrEmpty(sPermissionData))
            {
                t = Newtonsoft.Json.JsonConvert.DeserializeObject<T>(sPermissionData);
                return true;
            }

            return false;
        }
    }
}
