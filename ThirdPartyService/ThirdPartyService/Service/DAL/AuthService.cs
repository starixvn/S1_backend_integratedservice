using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyService.Service.DAL
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
        public string Authenticate(string Key)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { Token = Key });
            p.Add("PermissionData", dbType: DbType.String, direction: ParameterDirection.Output);
            int result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            result = conn.Execute("Common_AuthToken_Sel", p, trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            string permissionData = p.Get<string>("PermissionData");
            return permissionData;
        }
    }
}
