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
        
        public IEnumerable<int> GetSabaOddsMatchId()
        {
            return _mySqlClient.Query<int>("Service_SabaOddsMatchId_Get_V2");
        }

        /// <summary>
        /// 舊的沙巴賠率取得
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetSabaOddsMatchId_Old()
        {
            return _mySqlClient.Query<int>("Service_SabaOddsMatchId_Get");
        }

        /// <summary>
        /// 舊的沙巴賠率更新
        /// </summary>
        /// <param name="sabaOdds"></param>
        /// <returns></returns>
        public async Task<int> SetSabaOdds_Old(IEnumerable<UpdateOdd_Old> sabaOdds)
        {
            return await _mySqlClient.ExecuteAsync("Service_SabaOdds_Set_V2", sabaOdds);
        }
    }
}
