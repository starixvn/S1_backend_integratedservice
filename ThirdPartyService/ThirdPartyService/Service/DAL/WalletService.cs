using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL
{
    public class WalletService
    {
        private const int TypePosition = 2000;
        private readonly MySqlClient _mainDbClient;
        
        public WalletService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int SetBalance(int custId, int siteId, string userName, decimal amount, int tpId, string memo,
            string refTransId, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            var type = TypePosition + tpId;
            var p = new DynamicParameters(new
            {
                CustId = custId, 
                SiteId = siteId, 
                UserName = userName, 
                Amount = amount, 
                Type = type, 
                Memo = memo,
                RefTransId = refTransId
            });
            p.Add("Is1stDepoist", dbType: DbType.Boolean, direction: ParameterDirection.Output);
            p.Add("AfterAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            _mainDbClient.Execute("Wallet_Set", p, conn, trans);
            afterAmount = p.Get<decimal>("AfterAmount");
            return afterAmount >= 0 ? 0 : -1;
        }

        public int SetWithdrawalLimit(int custId, string userName, int siteId, decimal withdrawalLimit, string memo,
            string refTransId, out decimal afterWithdrawalLimit, MySqlConnection conn, MySqlTransaction trans)
        {
            var p = new DynamicParameters(new
            {
                CustId = custId,
                UserName = userName,
                SiteId = siteId,
                WithdrawalLimit = withdrawalLimit,
                Memo = memo,
                RefTransId = refTransId
            });
            p.Add("AfterWithdrawalLimit", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Common_WithdrawalLimit_Set", p, conn, trans);
            afterWithdrawalLimit = p.Get<decimal?>("AfterWithdrawalLimit") ?? 0;
            return result;
        }
    }
}
