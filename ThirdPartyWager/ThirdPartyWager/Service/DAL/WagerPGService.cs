using System;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerPGService
{
    private readonly MySqlClient _mainDbClient;

    public WagerPGService(IServiceProvider serviceProvider) {
        _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public IEnumerable<BettransData_MaxGame> GetPendingList(TimeCast ticketTime) {
        var spName = "Service_BettransRng_PG_Bet_Pending_Get";
        
        var pendingList = _mainDbClient.Query<BettransData_MaxGame>(spName, parameters: new {
            _BeginTime = ticketTime.startUnix,
            _EndTime = ticketTime.endUnix
        });

        return pendingList;
    }
}
