using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly MySqlClient _mainDbClient;

        public TpCustInfoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName });
        }
    }
}
