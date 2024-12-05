using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class SabaGameDetailService
    {
        private readonly MySqlClient _mainDbClient;

        public SabaGameDetailService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        
        public List<HotEventInfo> GetHotEventInfo(int siteId)
        {
            return _mainDbClient.Query<HotEventInfo>("Service_HotEventInfoByBattleDateTime_Get", new { _SiteId = siteId }).ToList();
        }
        
        public int SetHotEventMatchIdScore(int homeScore, int awayScore ,int matchId)
        {
            return _mainDbClient.Execute("Service_HotEventMatchIdScore_Set", new { _HomeScore = homeScore , _AwayScore = awayScore, _MatchId = matchId });
        }
        
    }
}
