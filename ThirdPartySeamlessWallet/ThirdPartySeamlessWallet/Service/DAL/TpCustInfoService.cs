using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly IConfiguration configuration;

        public TpCustInfoService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName }, commandType: CommandType.StoredProcedure);
        }
    }
}
