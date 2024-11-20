using System.Data;

using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class ThirdPartyService
    {
        private readonly MySqlClient _mainDbClient;
        public ThirdPartyService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public TpCheck ThirdPartyCheck(int tpId, string tpUserName)
        {
            return _mainDbClient.QueryFirst<TpCheck>("Service_TpCheck_Get", new { TpId = tpId, TpUserName = tpUserName });
        }

        [Obsolete("Please use SiteConfig Get Setting")]
        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            return _mainDbClient.QueryFirst<ThirdPartySetting>("Tp_Get", new { TpId = tpId });
        }

        [Obsolete("Please use SiteConfig Get Setting")]
        public ThirdPartySetting GetThirdPartyBySite(int tpId, int siteId)
        {
            return _mainDbClient.QueryFirst<ThirdPartySetting>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId });
        }

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            return _mainDbClient.QueryFirst<TpCurrency>("TpCurrency_Get", new { TpId = tpId, CurrencyId = currencyId });
        }

        public IEnumerable<TpGameCasino> GetGameCasino(int tpId)
        {
            return _mainDbClient.Query<TpGameCasino>("Service_GameCasino_Get_V2", commandType: CommandType.StoredProcedure, new { TpId = tpId });
        }

        public int AddTpBetFailureRecord(TpBetFailureRecord tpBetFailureRecord)
        {
            return _mainDbClient.Execute("Service_TpBetFailureRecord_Ins", commandType: CommandType.StoredProcedure, tpBetFailureRecord);
        }

        public IEnumerable<FinancialLog> GetFinancialLogTransactionCheck(string RefTransId, string YYYYMM)
        {
            return _mainDbClient.Query<FinancialLog>("Service_FinancialLog_TransactionCheck_Get", commandType: CommandType.StoredProcedure, new { _RefTransId = RefTransId, _YYYYMM = YYYYMM });
        }

        public IEnumerable<BonusLog> GetBonusLogTransactionCheck(string RefTransId)
        {
            return _mainDbClient.Query<BonusLog>("Service_BonusLog_TransactionCheck_Get", commandType: CommandType.StoredProcedure, new { _RefTransId = RefTransId });
        }

        public ServerInfo GetServerInfoBySiteName(string siteName)
        {
            return _mainDbClient.QueryFirst<ServerInfo>("Service_ServerInfo_SiteName_Get", new { _SiteName = siteName });
        }
    }
}
