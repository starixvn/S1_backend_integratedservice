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
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerHLService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool InsertHLWager(HLWagerData wager)
        {
            var p = new DynamicParameters();
            p.Add("betID", wager.BetID);
            p.Add("loginID", wager.UserName);
            p.Add("dateTime", ConvertToUtcTime(wager.DateTime));
            p.Add("drawType", wager.DrawType);
            p.Add("drawDate", ConvertToUtcTime(wager.DrawDate));
            p.Add("betNumber", wager.BetNumber);
            p.Add("betPosition", wager.BetPosition);
            p.Add("bet", wager.BetAmount);
            return _wagerDbClient.Execute("Service_TpWager_HL_Ins", p);
        }

        public bool UpdateHLWager(HLWagerData wager)
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
            return _wagerDbClient.Query<HLWagerTrans>("Service_TpWager_HL_UnSettle_Get",
                new
                {
                    TicketKey,
                    LimitCount,
                });
        }
        
        public long GetHaoLongTicketKey(long TicketKey)
        {
            var parameters = new { TicketKey };
            return _reportDbClient.QueryFirst<long>("Service_TpWager_HL_TicketKey_Get", parameters);
        }

        private DateTime ConvertToUtcTime(DateTime dateTime)
        {
            var localDateTimeOffset = new DateTimeOffset(dateTime, TimeZoneInfo.Local.GetUtcOffset(dateTime));
            return localDateTimeOffset.ToUniversalTime().DateTime;
        }
    }
}