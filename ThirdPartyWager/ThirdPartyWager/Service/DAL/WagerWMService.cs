using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerWMService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerWMService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(BetWM bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerWM_Ins_V2", bet);
        }

        public WagerWMTotal GetWagerTotal(long beginTime, long endTime, string currency, int siteId)
        {
            return _wagerDbClient.QueryFirst<WagerWMTotal>("Service_TpWagerWM_Total_Get_V2", new { BeginTime = beginTime, EndTime = endTime, Currency = currency, SiteId = siteId });
        }
    }
}
