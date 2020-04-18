using CommonService.Service.Model;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

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
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<CustInfoProfile>("Service_CustProfile_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
    }
}
