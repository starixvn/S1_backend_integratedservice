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

            if(tpId==0 && transType== TransType.Prize)
            {
                type = 5;
            }

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
        public int SetBonus(int gameType, int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterBonus, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            int result = 0;
            Tp tp = Tp.None;

            int type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out tp);
            var p = new DynamicParameters(new { gameType = gameType, CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = $"{tp} {Memo}", RefTransId = RefTransId });
            p.Add("BonusId", value: bonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BonusWallet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            bonusId = p.Get<int?>("BonusId") ?? 0;
            afterBonus = p.Get<decimal>("AfterBonus");

            return result;
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BonusFulfill>("Service_BonusFulfillSeamless_Get", commandType: CommandType.StoredProcedure);
        }
        public int BonusSettle(BonusFulfill bonusFulfill)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            int result = conn.Execute("Service_BonusSettle_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal bonus = p.Get<decimal>("Bonus");
                if (bonus > 0)
                {
                    result = SetBalance(bonusFulfill.CustId, bonusFulfill.SiteId, bonusFulfill.UserName, bonus, 0, TransType.Prize, $"Bonus Transfer to Cash: {Math.Abs(bonus).ToString("#,##0.##")}", bonusFulfill.BonusId.ToString(), out decimal _, conn, trans);
                }

                if (result > 0)
                    trans.Commit();
            }

            return result;
        }
    }
}
