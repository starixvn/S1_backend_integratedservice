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
        private const int typePosition = 10000;
        private readonly MySqlClient _mainDbClient;

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

        public decimal GetBalance(int CustId)
        {
            return _mainDbClient.QueryFirst<decimal>("Balance_Get", new { CustId });
        }

        public int SetBonus(int GameType, int CustId, int SiteId, string UserName, decimal Amount, int tpId, TransType transType, string Memo, string RefTransId, out decimal afterBonus, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            int result = 0;
            Tp tp = Tp.None;

            int Type = typePosition + ((int)transType * 1000) + tpId;
            Enum.TryParse(tpId.ToString(), out tp);
            var p = new DynamicParameters(new { GameType, CustId, SiteId, UserName, Amount, Type, Memo = $"{tp} {Memo}", RefTransId });
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("BonusId", value: bonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
           
            result = _mainDbClient.Execute("Service_BonusWallet_Set_V3", p,  conn ,trans);
            afterBonus = p.Get<decimal>("AfterBonus");
            bonusId = p.Get<int?>("BonusId") ?? 0;           
            return result;
        }

        /// <summary>
        /// 找到用戶還在Running的CustBonus
        /// </summary>
        public IEnumerable<CustBonus> GetCustBonusList(int CustId)
        {
            return _mainDbClient.Query<CustBonus>("Service_Bonus_Get_V2", parameters: new { CustId });
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
