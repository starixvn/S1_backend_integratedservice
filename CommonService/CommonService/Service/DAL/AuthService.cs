using CommonService.Service.Library;
using CommonService.Service.Model.enums;

namespace CommonService.Service.DAL
{
    public class AuthService
    {
        private readonly MySqlClient _mainDbClient;

        public AuthService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public int ApplyToken(string Key, string Value, int Expiry)
        {
            return _mainDbClient.Execute("Common_AuthToken_Ins", new { Token = Key, PermissionData = Value, Expiry = Expiry });
        }
    }
}
