using Dapper;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerNTConnectService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _reportDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerNTConnectService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _reportDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int InsertNTConnectWager(Wager93Data wager)
        {
            try
            {
                var p = new DynamicParameters();
                p.Add("ticket", wager.ticket);
                p.Add("player", wager.player);
                p.Add("bet_date", wager.bet_date);
                p.Add("gamecode", wager.gamecode);
                p.Add("game", wager.game);
                p.Add("drawno", wager.drawno);
                p.Add("bet_amount", wager.bet_amount);
                p.Add("bet_odd", wager.bet_odd);
                p.Add("bet_typegroup", wager.bet_typegroup);
                p.Add("bet_type", wager.bet_type);
                p.Add("winlosso", wager.winlosso);
                p.Add("status", wager.status);
                p.Add("LocalBetTime", ConvertUTCTime(wager.bet_date));
                var result = _wagerDbClient.Execute("Service_TpWager_93Connect_Ins", p);
                return !result ? 0 : 1;
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }
        }

        private DateTime ConvertUTCTime(string date)
        {
            DateTime dateTime;
            var convert = DateTime.TryParse(date, out dateTime);
            return convert ? new DateTimeOffset(dateTime, new TimeSpan(-4, 0, 0)).UtcDateTime : dateTime;
        }
    }
}