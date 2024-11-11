using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class SabaMatchService
{
    private readonly MySqlClient _mySqlClient;

    public SabaMatchService(IServiceProvider provider)
    {
        _mySqlClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public async Task<int> InsertMatch(HotMatchRequest request)
    {
        return await _mySqlClient.ExecuteAsync("Service_SabaMatch_Set", request);
    }

}
