using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.KY;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKYService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerKYService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public int InsertWager(BetKY bet)
        {
            // TODO: 請參考 Service_TpWagerPK_V8_Ins_V2
            return _wagerDbClient.Execute("Service_TpWagerPK_KY_Ins", bet);
        }

        public WagerKYTotal GetWagerTotal(long beginTime, long endTime, int agent)
        {
            // TODO: 請參考 Service_TpWagerPK_V8_Total_Get_V2
            return _wagerDbClient.QueryFirst<WagerKYTotal>("Service_TpWagerPK_KY_Total_Get", new { BeginTime = beginTime, EndTime = endTime, ChannelID = agent });
        }
    }
}
