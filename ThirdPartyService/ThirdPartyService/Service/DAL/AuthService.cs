using System.Data;

using Dapper;

using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class AuthService
    {
        private readonly MySqlClient _mainDbClient;

        public AuthService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int ApplyToken(string key, string value, int expiry)
        {
            return _mainDbClient.Execute("Common_AuthToken_Ins", new { Token = key, PermissionData = value, Expiry = expiry });
        }

        public string Authenticate(string key)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(new { Token = key });
            p.Add("PermissionData", dbType: DbType.String, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Common_AuthToken_Sel", trans, p);

            trans.Commit();
            conn.Close();
            conn.Dispose();

            var permissionData = p.Get<string>("PermissionData");
            return permissionData;
        }

        public int UpdateTpToken(string token, string tpToken)
        {
            return _mainDbClient.Execute("Common_AuthToken_TpToken_Set", new { Token = token, TpToken = tpToken });
        }
    }
}
