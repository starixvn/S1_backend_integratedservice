using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerV8Service
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerV8Service(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(BetV8 bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_V8_Ins", bet);
        }

        public int InsertWager_V2(BetV8 bet)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_V8_Ins_V2", bet);
        }

        public WagerV8Total GetWagerTotal(long beginTime, long endTime)
        {
            return _wagerDbClient.QueryFirst<WagerV8Total>("Service_TpWagerPK_V8_Total_Get", new { BeginTime = beginTime, EndTime = endTime });
        }
        public WagerV8Total GetWagerTotalV2(long beginTime, long endTime, int agent)
        {
            return _wagerDbClient.QueryFirst<WagerV8Total>("Service_TpWagerPK_V8_Total_Get_V2", new { BeginTime = beginTime, EndTime = endTime, ChannelID = agent });
        }
    }
}
