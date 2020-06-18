using CommonService.Service.DAL;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService authService;
        private readonly IConfiguration config;

        public AuthBLL(AuthService authService, IConfiguration config)
        {
            this.authService = authService;
            this.config = config;
        }
        public string ApplyToken<T>(T t)
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            if (authService.ApplyToken(GuidString, Newtonsoft.Json.JsonConvert.SerializeObject(t), config.GetValue<int>("TokenExpireTime")) > 0)
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
