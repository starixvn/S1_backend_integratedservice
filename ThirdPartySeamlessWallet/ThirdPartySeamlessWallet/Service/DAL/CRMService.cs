using Dapper;
using System.Data;
using System.Xml.Linq;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class CRMService
    {
        private readonly MySqlClient _mainDbClient;

        public CRMService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int SetCustPromotion(int custId, string bonusCode)
        {
            var p = new DynamicParameters(new { _BonusCode = bonusCode , _CustId = custId });
            return _mainDbClient.Execute("Service_CustPromotion_Set", parameters: p);
        }
    }
}
