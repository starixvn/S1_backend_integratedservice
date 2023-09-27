using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class ThirdPartyHelper
    {
        private readonly MySqlClient _mainDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _wagerDbClient;

        public ThirdPartyHelper(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public ThirdPartyModel GetThirdPartySetting(int tpId)
        {
            return _reportDbClient.QueryFirst<ThirdPartyModel>("Tp_Get", new { TpId = tpId });
        }

        public ThirdPartyModel GetThirdPartyBySite(int tpId, int siteId)
        {
            return _reportDbClient.QueryFirst<ThirdPartyModel>("Common_Tp_Get", new { TpId = tpId, SiteId = siteId });
        }

        public int SetThirdPartySetting(int tpId, string setting)
        {
            return _mainDbClient.Execute("Service_Tp_Set", new { TpId = tpId, Setting = setting });
        }

        public ThirdPartyTimerModel GetThirdPartyTimer(int tpId, int type)
        {
            return _wagerDbClient.QueryFirst<ThirdPartyTimerModel>("Service_TpWagerTimer_Get", new { TpId = tpId, Type = type });
        }

        public int SetThirdPartyTimer(int tpId, int type, long start)
        {
            return _wagerDbClient.Execute("Service_TpWagerTimer_Ins", new { TpId = tpId, Type = type, StartUnix = start });
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByCurrency(int tpId, int type, string currency, int gameType = 0)
        {
            return _wagerDbClient.QueryFirst<TpWagerTimerReferenceModel>("Service_TpWagerTimerReference_ByCurrency_Get", new { TpId = tpId, Type = type, Currency = currency, GameType = gameType });
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByGameType(int tpId, int type, int gameType)
        {
            return _wagerDbClient.QueryFirst<TpWagerTimerReferenceModel>("Service_TpWagerTimerReference_ByGameType_Get", new { TpId = tpId, Type = type, GameType = gameType });
        }

        public TpWagerTimerReferenceModel GetThirdPartyTimerReferenceByReference(int tpId, int type, int gameType, string reference)
        {
            return _wagerDbClient.QueryFirst<TpWagerTimerReferenceModel>("Service_TpWagerTimerReference_ByReference_Get", new { TpId = tpId, Type = type, GameType = gameType, Reference = reference });
        }

        public int SetThirdPartyTimerByCurrency(int tpId, int type, long start, string currency, int gameType = 0)
        {
            return _wagerDbClient.Execute("Service_TpWagerTimerReference_Currency_Set", new { TpId = tpId, Type = type, StartUnix = start, Currency = currency, GameType = gameType });
        }

        public int SetThirdPartyTimerByGameType(int tpId, int type, long start, int gameType)
        {
            return _wagerDbClient.Execute("Service_TpWagerTimerReference_GameType_Set", new { TpId = tpId, Type = type, StartUnix = start, GameType = gameType });
        }

        public int SetThirdPartyTimerByReference(int tpId, int type, long start, int gameType, string reference)
        {
            return _wagerDbClient.Execute("Service_TpWagerTimerReference_Reference_Set", new { TpId = tpId, Type = type, StartUnix = start, GameType = gameType, Reference = reference });
        }

        public IEnumerable<TpGameCasino> GetGameCasino(int tpId)
        {
            return _reportDbClient.Query<TpGameCasino>("Service_GameCasino_Get_V2", new { TpId = tpId });
        }

        public IEnumerable<TpListModel> GetTpListByTpId(int tpId)
        {
            return _reportDbClient.Query<TpListModel>("Service_TpList_ByTpId_Get", new { _TpId = tpId });
        }

    }
}
