using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class SabaOddsService
    {
        private readonly MySqlClient _mySqlClient;

        public SabaOddsService(IServiceProvider provider)
        {
            _mySqlClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public async Task<int> SetSabaOdds(IEnumerable<UpdateOdd> sabaOdds)
        {
            return await _mySqlClient.ExecuteAsync("Service_SabaOdds_Set_V3", sabaOdds);
        }

        public async Task<int> InsertMatch(IEnumerable<HotEventInfo> hotEventInfos)
        {
            return await _mySqlClient.ExecuteAsync("Service_HotEventInfo_Insert_V3", hotEventInfos);
        }

        public IEnumerable<HotEventInfo> GetSabaOddsMatchId()
        {
            return _mySqlClient.Query<HotEventInfo>("Service_SabaOddsMatchId_Get_V3");
        }
    }
}
