using System.Text.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerKiss918Service
    {
        private readonly MySqlClient _wagerDbClient;

        public WagerKiss918Service(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
        }

        public async Task<IEnumerable<WagerKiss918Model>> GetWager(string referenceID)
        {
            return await _wagerDbClient.QueryAsync<WagerKiss918Model>("Service_TpWagerKiss918_Detail_Get", new { _referenceID = referenceID });
        }

        public int InsertWager(IEnumerable<WagerKiss918Model> datas)
        {
            return _wagerDbClient.Execute("Service_TpWagerKiss918Batch_Ins", new { _Infos = JsonSerializer.Serialize(datas) });
        }
    }
}
