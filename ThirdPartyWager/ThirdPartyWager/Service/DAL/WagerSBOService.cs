using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public interface IWagerSBOService
    {
        Task<int> InsertWager(SboResult bet);
    }

    public class WagerSBOService : IWagerSBOService
    {
        private readonly MySqlClient _sqlClient;

        public WagerSBOService(IEnumerable<MySqlClient> sqlClients)
        {
            _sqlClient = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Wager);
        }

        public async Task<int> InsertWager(SboResult bet)
        {
            var p = new DynamicParameters();
            p.Add("refNo", bet.refNo);
            p.Add("username", bet.username);
            p.Add("sportsType", bet.sportsType);
            p.Add("orderTime", bet.orderTime);
            p.Add("winLostDate", bet.winLostDate);
            p.Add("settleTime", bet.settleTime);
            p.Add("modifyDate", bet.modifyDate);
            p.Add("odds", bet.odds);
            p.Add("oddsStyle", bet.oddsStyle);
            p.Add("stake", bet.stake);
            p.Add("actualStake", bet.actualStake);
            p.Add("currency", bet.currency);
            p.Add("status", bet.status);
            p.Add("winLost", bet.winLost);
            p.Add("turnover", bet.turnover);
            p.Add("isHalfWonLose", bet.isHalfWonLose);
            p.Add("isLive", bet.isLive);
            p.Add("maxWinWithoutActualStake", bet.maxWinWithoutActualStake);
            p.Add("ip", bet.ip);
            p.Add("voidReason", bet.voidReason);
            p.Add("isSystemTagRisky", bet.isSystemTagRisky);
            p.Add("isCustomerTagRisky", bet.isCustomerTagRisky);
            p.Add("topDownline", bet.topDownline);
            p.Add("subBet", JsonConvert.SerializeObject(bet.subBet));
            return await _sqlClient.ExecuteAsync("Service_TpWagerSBO_Ins", p);
        }
    }
}