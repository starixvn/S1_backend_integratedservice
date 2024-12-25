using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerOne789Service
    {

        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerOne789Service(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool InsertWagerOne789(BetOne789 bet, int gameType)
        {
            var sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_TpWagerOne789_LT_Ins_V2";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_TpWagerOne789_LT_789_Ins_V2";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_TpWagerOne789_LC_Ins_V2";
                    break;
                default:
                    break;
            }
            return _wagerDbClient.Execute(sp, new DynamicParameters(bet));
        }

        public IEnumerable<BettransDataOne789> GetPendingList(TimeCast ticketTime, int gameType)
        {
            var sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_BettransLT_One789_Bet_Pending_Get";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_BettransLT_One789_789_Bet_Pending_Get";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_BettransLC_One789_Bet_Pending_Get";
                    break;
                default:
                    break;
            }
            return _mainDbClient.Query<BettransDataOne789>(sp, parameters: new { _BeginTime = ticketTime.startUnix, _EndTime = ticketTime.endUnix });
        }

        public BettransDataOne789 GetBettransData(int gameType, string tx)
        {
            var sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_BettransLT_One789_Get";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_BettransLT_One789_789_Get";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_BettransLC_One789_Get";
                    break;
                default:
                    break;
            }
            return _mainDbClient.QueryFirst<BettransDataOne789>(sp, new DynamicParameters(new { Source = tx }));
        }
    }
}
