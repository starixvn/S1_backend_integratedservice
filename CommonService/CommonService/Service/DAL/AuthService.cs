using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace CommonService.Service.DAL
{
    public class AuthService
    {
        private readonly IConfiguration configuration;
        public AuthService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int ApplyToken(string Key, string Value, int Expiry)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Common_AuthToken_Ins", new { Token = Key, PermissionData = Value, Expiry = Expiry }, commandType: CommandType.StoredProcedure);
        }
    }
}
