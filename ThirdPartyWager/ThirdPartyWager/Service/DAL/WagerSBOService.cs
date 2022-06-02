using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
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
            return await _sqlClient.ExecuteAsync("Service_TpWagerSBO_Ins", new DynamicParameters(bet));
        }
    }
}