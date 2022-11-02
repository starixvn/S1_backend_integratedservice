using CommonService.Service.Model;
using Dapper;
using MySql.Data.MySqlClient;
using System.Data;

namespace CommonService.Service.DAL
{
    public class CustInfoService
    {
        private readonly IConfiguration configuration;
        public CustInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public CustInfoProfile GetCustInfoProfile(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Service_CustProfile_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
    }
}
