using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerSAService
    {
        private readonly IConfiguration configuration;

        public WagerSAService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetSA bet)
        {
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            var p = new DynamicParameters(new { bet.BetID, bet.HostID, bet.Round, bet.GameID, bet.GameType, bet.Username, bet.BetAmount, bet.Rolling, bet.ResultAmount, bet.Balance, bet.Set, bet.Detail, bet.BetType, bet.BetSource, bet.TransactionID, GameResult = JsonConvert.SerializeObject(bet.GameResult), bet.State, bet.betUnix, bet.payOutUnix });
            return conn.Execute("Service_TpWagerSA_Ins", p, commandType: CommandType.StoredProcedure);
        }

        //public WagerSATotal GetWagerTotal(long beginTime, long endTime)
        //{
        //    using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
        //    return conn.QueryFirstOrDefault<WagerSATotal>("Service_TpWagerAllBetTotal_Get", new { BeginTime = beginTime, EndTime = endTime }, commandType: CommandType.StoredProcedure);
        //}
    }
}
