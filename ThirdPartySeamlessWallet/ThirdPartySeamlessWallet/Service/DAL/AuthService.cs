using System.Data;

using Dapper;

using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
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
            return _mainDbClient.Execute("Common_AuthToken_Ins", CommandType.StoredProcedure, new { Token = Key, PermissionData = Value, Expiry = Expiry });
        }

        public string Authenticate(string Key)
        {
            var p = new DynamicParameters(new { Token = Key });
            p.Add("PermissionData", dbType: DbType.String, direction: ParameterDirection.Output);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _mainDbClient.Execute("Common_AuthToken_Sel", trans, p);
            var permissionData = p.Get<string>("PermissionData");
            _mainDbClient.CommitAndDisposeConnection(conn, trans);

            return permissionData;
        }
    }
}
