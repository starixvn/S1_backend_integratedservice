using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class WalletService
    {
        private readonly IConfiguration configuration;
        private const int typePosition = 10000;
        public WalletService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int SetBalance(int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            int type = typePosition + ((int)transType * 1000) + tpId;
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = Memo, RefTransId = RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            conn.Execute("Wallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            _ = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal>("AfterAmount");
            return afterAmount >= 0 ? 0 : -1;
        }
        public TpCustBalance GetBalance(int tpId, string tpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCustBalance>("Service_TpCustBalance_Get", new { TpId = tpId, TpUserName = tpUserName }, commandType: CommandType.StoredProcedure);
        }
    }
}
