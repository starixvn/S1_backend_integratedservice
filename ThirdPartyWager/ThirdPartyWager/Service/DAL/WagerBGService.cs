using System.Text.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerBGService
{
    private readonly MySqlClient _wagerDbClient;

    public WagerBGService(IServiceProvider provider)
    {
        _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
    }

    public Task<int> InsertWager(IEnumerable<WagerBGModel> datas)
    {
        return _wagerDbClient.ExecuteAsync("Service_TpWagerLC_BG_Batch_Ins", new { _Infos = JsonSerializer.Serialize(datas) });
    }
}
