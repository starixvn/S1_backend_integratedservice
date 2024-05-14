using Dapper;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerAMBService
{
    private readonly MySqlClient _wagerDbClient;

    public WagerAMBService(IServiceProvider provider)
    {
        _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
    }
    
    public int InsertAMBWager(betRecord_AMB wager,int gameType)
    {
        var result = false;
        try
        {
            switch (gameType)
        {
            case (int)GameType.AMB_Poker:
                var p = new DynamicParameters();
                p.Add("id", wager._id);
                p.Add("username", wager.username);
                p.Add("gamename", wager.gameName);
                p.Add("gameid", wager.gameId);
                p.Add("categories", wager.categories);
                p.Add("timestamp", wager.timestamp);
                p.Add("roundid", wager.roundId);
                p.Add("roomid", wager.roomId);
                p.Add("uuid", wager.uuid);
                p.Add("bet", wager.bet);
                p.Add("turnover", wager.turnover);
                p.Add("winlose", wager.winlose);
                p.Add("commission", wager.commission);
                p.Add("LocalDateTime", ConvertUTCTime(wager.timestamp));
        
                result = _wagerDbClient.Execute("Service_TpWagerPK_AMB_Ins", p);
                return !result ? 0 : 1;
            
            case (int)GameType.AMB_RNG:
                var d = new DynamicParameters();
                d.Add("transactionId", wager._id);
                d.Add("username", wager.username);
                d.Add("gameName", wager.gameName);
                d.Add("gameId", wager.gameId);
                d.Add("categories", wager.categories);
                d.Add("timestamp", wager.timestamp);
                d.Add("product", wager.product);
                d.Add("roundid", wager.roundId);
                d.Add("bet", wager.bet);
                d.Add("turnover", wager.turnover);
                d.Add("winlose", wager.winlose);
                d.Add("isEndRound", wager.isEndRound);
                d.Add("isFreespin", wager.isFreespin);
                d.Add("isBuyFeature", wager.isBuyFeature);
                d.Add("isGamble", wager.isGamble);
                d.Add("LocalDateTime", ConvertUTCTime(wager.timestamp));
        
                result = _wagerDbClient.Execute("Service_TpWagerRng_AMB_Ins", d);
                return !result ? 0 : 1;
            default:
                return 0;
        }
        }
        catch (MySqlException mySqlEx)
        {
            /*Duplicate for key 'PRIMARY'*/
            if (mySqlEx.Number == 1062)
            {
                return 1;
            }
            else
            {
                throw mySqlEx;
            }
        }
    }

    private DateTime ConvertUTCTime(string date)
    {
        DateTime dateTime;
        var convert = DateTime.TryParse(date, out dateTime);
        return convert ? new DateTimeOffset(dateTime, new TimeSpan(-4, 0, 0)).UtcDateTime : dateTime;
    }
    
}