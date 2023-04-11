using Dapper;
using MySql.Data.MySqlClient;
using System.Data;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class WalletService
    {
        private readonly MySqlClient _mainDbClient;
        private const int typePosition = 10000;
        public WalletService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public int SetBalance(int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            var type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out Tp tp);

            if (tpId == 0 && transType == TransType.Prize)
            {
                type = (int)FinancialType.BonusReward;
            }

            var p = new DynamicParameters(new { CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = $"{tp} {Memo}", RefTransId = RefTransId });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            if (allowNegative == AllowNegative.Allow)
            {
                p.Add("AllowNegative", (int)allowNegative, dbType: DbType.Decimal, direction: ParameterDirection.Input);
                _mainDbClient.Execute("Service_SeamlessWallet_Set_V2", p, conn, trans);
            }
            else
            {
                _mainDbClient.Execute("Wallet_Set_V2", p, conn, trans);
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
            return _mainDbClient.ExecuteScalar<decimal>("Balance_Get", new { CustId = custId });
        }
        public int SetBonus(int gameType, int custId, int siteId, string userName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterBonus, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out Tp tp);
            var p = new DynamicParameters(new { gameType = gameType, CustId = custId, SiteId = siteId, UserName = userName, Amount = Amount, Type = type, Memo = $"{tp} {Memo}", RefTransId = RefTransId });
            p.Add("BonusId", value: bonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BonusWallet_Set_V2", p,  conn ,trans);

            bonusId = p.Get<int?>("BonusId") ?? 0;
            afterBonus = p.Get<decimal>("AfterBonus");

            return result;
        }
        public decimal GetBonus(int custId, int promoCategoryId)
        {
            return _mainDbClient.QueryFirst<decimal>("Service_Bonus_Get", new { CustId = custId, PromoCategoryId = promoCategoryId });
        }
        public IEnumerable<BonusFulfill> GetBonusFulfill()
        {
            return _mainDbClient.Query<BonusFulfill>("Service_BonusFulfillSeamless_Get", commandType: CommandType.StoredProcedure);
        }
        public int BonusSettle(BonusFulfill bonusFulfill)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(bonusFulfill);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("Service_BonusSettle_Set_V2", p, conn ,trans);

            if (result > 0)
            {
                var bonus = p.Get<decimal>("Bonus");
                if (bonus > 0)
                {
                    result = SetBalance(bonusFulfill.CustId, bonusFulfill.SiteId, bonusFulfill.UserName, bonus, 0, TransType.Prize, $"Bonus Transfer to Cash: {Math.Abs(bonus):#,##0.##}", bonusFulfill.BonusId.ToString(), out _, conn, trans);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                }
            }

            return result;
        }
    }
}
