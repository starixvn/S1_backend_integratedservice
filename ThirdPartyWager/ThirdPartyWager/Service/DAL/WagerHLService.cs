using Dapper;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerHLService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerHLService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool InsertHLWager(HLWagerData wager)
        {
            var p = new DynamicParameters();
            p.Add("betID", wager.BetID);
            p.Add("loginID", wager.UserName);
            p.Add("dateTime", wager.DateTime);
            p.Add("drawType", wager.DrawType);
            p.Add("drawDate", wager.DrawDate);
            p.Add("betNumber", wager.BetNumber);
            p.Add("betPosition", wager.BetPosition);
            p.Add("bet", wager.BetAmount);
            return _wagerDbClient.Execute("Service_TpWager_HL_Ins", p);
        }

        public async Task<bool> UpdateHLWager(HLWagerData wager)
        {
            var p = new DynamicParameters();
            p.Add("betID", wager.BetID);
            p.Add("strikePosition", wager.StrikePosition);
            p.Add("strike", decimal.Parse(wager.SettleAmount));
            p.Add("SettleFlag", wager.SettleFlag);
            return _wagerDbClient.Execute("Service_TpWager_HL_Set", p);
        }
        
        public IEnumerable<HLWagerTrans> GetHLBetUnSettledList(long TicketKey, int LimitCount = 1000)
        {
            return _wagerDbClient.Query<HLWagerTrans>("BetDetail.Service_TpWager_HL_Unsettle_Get_V2",
                new
                {
                    TicketKey,
                    LimitCount,
                });
        }
    }
}