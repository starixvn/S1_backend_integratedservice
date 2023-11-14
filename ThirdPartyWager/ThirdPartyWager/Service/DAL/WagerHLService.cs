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

        public bool InsertHLWager(HLBetData bet)
        {
            var p = new DynamicParameters();
            p.Add("betID", bet.BetID);
            p.Add("dateTime", bet.DateTime);
            p.Add("drawType", bet.DrawType);
            p.Add("drawDate", bet.DrawDate);
            p.Add("betNumber", bet.BetNumber);
            p.Add("betPosition", bet.BetPosition);
            p.Add("bet", bet.BetAmount);
            return _wagerDbClient.Execute("Service_TpWagerHL_Ins", p);
        }

        public bool UpdateHLWager(HLBetData bet)
        {
            var p = new DynamicParameters();
            p.Add("betID", bet.BetID);
            p.Add("dateTime", bet.DateTime);
            p.Add("drawType", bet.DrawType);
            p.Add("drawDate", bet.DrawDate);
            p.Add("betNumber", bet.BetNumber);
            p.Add("betPosition", bet.BetPosition);
            p.Add("bet", bet.BetAmount);
            return _wagerDbClient.Execute("Service_TpWagerHL_Set", p);
        }
    }
}