using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerCFService
    {
        private readonly MySqlClient _wagerDbClient;
        
        public WagerCFService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(BetCF bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerCF_Ins", bet);
        }

        public WagerCFModel GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerCFModel>("Service_TpWagerCFTotal_Get", new { BeginTime = beginTime, EndTime = endTime });
        }
    }
}
