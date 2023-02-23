using CommonService.Service.DAL;
using Newtonsoft.Json;

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
            var g = Guid.NewGuid();
            var GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            if (authService.ApplyToken(GuidString, JsonConvert.SerializeObject(t), config.GetValue<int>("TokenExpireTime")) > 0)
            {
                return GuidString;
            }

            return null;
        }
    }
}
