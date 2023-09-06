using Dapper;
using System.Data;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class BonusService
    {
        private int typePosition = 1000;
        private readonly MySqlClient _mainDbClient;

        public BonusService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool BonusTransferOutAll(int custId, int siteId, string userName, int tpId, string Memo, string RefTransId, out int BonusId, out decimal Bonus, out decimal LockedAmount, out decimal BonusAmount, out decimal RolloverAmount, out long ExpiryTime, IDbTransaction trans)
        {
            var type = typePosition + tpId;
            BonusId = 0;
            Bonus = 0;
            LockedAmount = 0;
            BonusAmount = 0;
            ExpiryTime = 0;
            RolloverAmount = 0;

            var p = new DynamicParameters(new
            {
                TpId = tpId,
                CustId = custId,
                SiteId = siteId,
                UserName = userName,
                Type = type,
                Memo,
                RefTransId
            });
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("Bonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("LockedAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("BonusAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("RolloverAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ExpiryTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Common_BonusTransferOutAll_Set", trans, p);

            if (result > 0)
            {
                BonusId = p.Get<int>("BonusId");
                Bonus = p.Get<decimal>("Bonus");
                LockedAmount = p.Get<decimal>("LockedAmount");
                BonusAmount = p.Get<decimal>("BonusAmount");
                RolloverAmount = p.Get<decimal>("RolloverAmount");
                ExpiryTime = p.Get<long>("ExpiryTime");
            }

            return BonusId > 0;
        }
        
        public bool BonusTransferRollback(int custId, int siteId, string userName, decimal Bonus, int tpId, string Memo, string RefTransId, int bonusId, out decimal afterBonus, IDbTransaction trans)
        {
            var type = typePosition + tpId;
            var p = new DynamicParameters(new
            {
                BonusId = bonusId, 
                CustId = custId, 
                SiteId = siteId, 
                UserName = userName,
                Bonus, 
                Type = type,
                Memo,
                RefTransId
            });
            p.Add("AfterBonus", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Common_BonusTransferRollback_Set", trans, p);
            afterBonus = p.Get<decimal>("AfterBonus");
            return afterBonus >= 0;
        }
        
        public int SetBonusStatus(int bonusId, int status, int forStatus, IDbTransaction trans)
        {
            return _mainDbClient.Execute("Common_BonusStatus_Set",trans,
                new
                {
                    BonusId = bonusId,
                    Status = status,
                    ForStatus = forStatus
                });
        }
    }
}
