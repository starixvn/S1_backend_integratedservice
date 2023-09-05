using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL
{
    public class ServerInfoService
    {

        #region Fields

        private readonly MySqlClient _mainDbClient;

        #endregion

        #region Constructors

        public ServerInfoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        #endregion

        #region Methods

        /// <summary>
        /// 取得指定站台資訊
        /// </summary>
        /// <param name="siteId">站台ID</param>
        /// <returns></returns>
        public ServerInfo GetServerInfo(int siteId)
        {
            return _mainDbClient.QueryFirst<ServerInfo>("ServerInfo_Get", new { SiteId = siteId });
        }

        /// <summary>
        /// 取得所有站台資訊
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ServerInfo> GetServiceInfoList()
        {
            return _mainDbClient.Query<ServerInfo>("Admin_ServerInfoList_Get");
        }

        public DefTpCurrencyId GetDefaultTpCurrencyIdByTpId(int siteId, int tpId)
        {
            return _mainDbClient.QueryFirst<DefTpCurrencyId>("Service_ServerInfo_DefCurrency_ByTpId_Get", new { _SiteId = siteId, _TpId = tpId });
        }
        #endregion
    }
}
