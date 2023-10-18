using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public interface IWagerNextspinService
    {
        Task<int> InsertWager(IList<NextspinBetInfo> wagers);
    }

    public class WagerNextspinService : IWagerNextspinService
    {
        private readonly MySqlClient _sqlClient;

        public WagerNextspinService(IEnumerable<MySqlClient> sqlClients)
        {
            _sqlClient = sqlClients.FirstOrDefault(a => a.DataBase == DataBase.Wager);
        }

        public async Task<int> InsertWager(IList<NextspinBetInfo> wagers)
        {
            return await _sqlClient.ExecuteAsync("Service_TpWagerRngNextspin_Ins",
                new { _NextspinWager = JsonConvert.SerializeObject(wagers) });
        }
    }
}