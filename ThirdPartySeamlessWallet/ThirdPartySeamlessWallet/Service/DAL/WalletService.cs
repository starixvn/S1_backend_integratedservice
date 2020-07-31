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
        public int SetBalance(int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            Tp tp = Tp.None;
            int type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out tp);
            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = $"{tp} {Memo}", RefTransId = RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            if (allowNegative == AllowNegative.Allow)
            {
                p.Add("AllowNegative", (int)allowNegative, dbType: DbType.Decimal, direction: ParameterDirection.Input);
                conn.Execute("Service_SeamlessWallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else
            {
                conn.Execute("Wallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            _ = p.Get<byte?>("Is1stDepoist") ?? 0;
            afterAmount = p.Get<decimal>("AfterAmount");

            if (allowNegative == AllowNegative.Allow)
            {
                return 1;
            }
            else
            {
                return afterAmount >= 0 ? 1 : 0;
            }
        }
        public decimal GetBalance(int custId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId }, commandType: CommandType.StoredProcedure);
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId }, commandType: CommandType.StoredProcedure);
        }
    }
}
