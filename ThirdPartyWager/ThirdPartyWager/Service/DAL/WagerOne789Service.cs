using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerOne789Service
    {
        private readonly IConfiguration configuration;

        public WagerOne789Service(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
        public int InsertWagerOne789(BetOne789 bet, int gameType)
        {
            string game = "";
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
            string sp = $"Service_TpWagerOne789_{game}_Ins";
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute($"{sp}", bet, commandType: CommandType.StoredProcedure);
        }
    }
}
