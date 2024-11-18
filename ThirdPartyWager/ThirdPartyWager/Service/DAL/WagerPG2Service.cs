using Dapper;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerPG2Service
{
    private readonly MySqlClient _maimDbClient;

    public WagerPG2Service(IServiceProvider provider)
    {
        _maimDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public IEnumerable<string> GetUserListByDate(string reportTimeStartTime)
    {
        return _maimDbClient.Query<string>("Service_BettransRng_PG2_UserList_Get", new DynamicParameters(new { _startTime = reportTimeStartTime }));
    }
}