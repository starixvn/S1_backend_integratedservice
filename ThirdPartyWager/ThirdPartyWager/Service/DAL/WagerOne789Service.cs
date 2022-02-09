using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerOne789Service
    {

        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerOne789Service(
            IServiceProvider provider,
            IConfiguration configuration
        )
        {
            var log = provider.GetRequiredService<ILogger<MySqlClient>>();
            _wagerDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionStringWager"), log);
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), log);
        }

        public bool InsertWagerOne789(BetOne789 bet, int gameType)
        {
            var game = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    game = "LT";
                    break;
                case (int)GameType.One789_Lottery_789:
                    game = "LT_789";
                    break;
                case (int)GameType.One789_LiveCasino:
                    game = "LC";
                    break;
                default:
                    break;
            }
            var sp = $"Service_TpWagerOne789_{game}_Ins";
            return _wagerDbClient.Execute(sp, new DynamicParameters(bet));
        }

        public IEnumerable<BettransDataOne789> GetPendingList(int gameType)
        {
            var game = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    game = "LT_One789";
                    break;
                case (int)GameType.One789_Lottery_789:
                    game = "LT_One789_789";
                    break;
                case (int)GameType.One789_LiveCasino:
                    game = "LC_One789";
                    break;
                default:
                    break;
            }
            var sp = $"Service_Bettrans{game}_Bet_Pending_Get";
            return _mainDbClient.Query<BettransDataOne789>(sp);
        }
    }
}
