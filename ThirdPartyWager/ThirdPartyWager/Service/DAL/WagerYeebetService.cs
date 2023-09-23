using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;
public class WagerYeebetService
{
    private readonly MySqlClient _wagerDbClient;

    public WagerYeebetService(IServiceProvider provider)
    {
        _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
    }

    public Task<int> InsertWager(WagerYeebetModel data)
    {
        return _wagerDbClient.ExecuteAsync("Service_TpWagerLC_Yeebet_Ins", data);
    }
}
