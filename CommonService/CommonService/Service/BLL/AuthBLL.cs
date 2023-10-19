using CommonService.Service.DAL;

using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class AuthBLL
    {
        private readonly AuthService _authService;
        private readonly IConfiguration _config;

        public AuthBLL(IServiceProvider provider)
        {
            _authService = provider.GetRequiredService<AuthService>();
            _config = provider.GetRequiredService<IConfiguration>();
        }

        public string ApplyToken<T>(T t)
        {
            var g = Guid.NewGuid();
            var GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");
            if (_authService.ApplyToken(GuidString, JsonConvert.SerializeObject(t), _config.GetValue<int>("TokenExpireTime")) > 0)
            {
                return GuidString;
            }

            return null;
        }
    }
}
