using Dapper;
using MySql.Data.MySqlClient;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerMarblesService
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerMarblesService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<int> InsertWager(MBSBetsData data)
        {
            try
            {
                var p = new DynamicParameters(new
                {
                    transNo = data.transNo,
                    gameNo = data.gameNo,
                    account = GetTpUserName(data.account),
                    currency = data.currency,
                    agent = data.agent,
                    lineCode = data.lineCode,
                    gameCode = data.gameCode,
                    gameInfo = data.gameInfo ?? "NA",
                    bet = data.bet,
                    effecBet = data.effecBet,
                    commission = data.commission,
                    profit = data.profit,
                    transBefore = data.transBefore,
                    transAfter = data.transAfter,
                    status = data.status,
                    version = data.version,
                    startTimeUnix = data.startTime,
                    endTimeUnix = data.endTime,
                    startTime = DateTimeOffset.FromUnixTimeMilliseconds(data.startTime).UtcDateTime,
                    endTime = DateTimeOffset.FromUnixTimeMilliseconds(data.endTime).UtcDateTime
                });

                return await _wagerDbClient.ExecuteAsync("Service_TpWager_Marbles_Ins", p);
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return 1;
                }

                throw;
            }
        }

        private string GetTpUserName(string marblesAccount)
        {
            return marblesAccount.Substring(7);
        }
    }
}