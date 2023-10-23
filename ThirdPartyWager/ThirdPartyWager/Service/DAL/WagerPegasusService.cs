using Dapper;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerPegasusService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerPegasusService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public bool InsertWager(WagerDataDetail_Pegasus bet)
        {
            var p = new DynamicParameters();
            p.Add("TransactionNo", bet.TransactionNo);
            p.Add("PlayerName", bet.PlayerName);
            p.Add("GameID", bet.GameID);
            p.Add("WinLossAmount", bet.WinLossAmount);
            p.Add("RoundID", bet.RoundID);
            p.Add("BetTime", bet.BetTime);
            p.Add("BetAmount", bet.BetAmount);
            p.Add("PayoutAmount", bet.PayoutAmount);
            p.Add("TaxAmount", bet.TaxAmount);
            p.Add("ValidAmount", bet.ValidAmount);
            p.Add("ExtraInfo", bet.ExtraInfo is null ? "{}" : JsonConvert.SerializeObject(bet.ExtraInfo));
            return _wagerDbClient.Execute("Service_TpWagerPegasus_Ins", p);
        }
    }
}
