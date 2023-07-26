using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerIMService
{
    private readonly MySqlClient _wagerDbClient;

    public WagerIMService(IServiceProvider provider)
    {
        _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
    }

    public int InsertSlotWager(IEnumerable<IMSlotWager> wagers)
    {
        return _wagerDbClient.Execute("Service_TpWagerRng_IM_Batch_Ins",
            new { _Info = JsonConvert.SerializeObject(wagers) });
    }
    
    public int InsertSportsWager(IEnumerable<IMSportsWager> wagers)
    {
        return _wagerDbClient.Execute("Service_TpWagerSports_IM_Batch_Ins",
            new { _Info = JsonConvert.SerializeObject(wagers) });
    }
    
    public int InsertESportsWager(IEnumerable<IMESportsWager> wagers)
    {
        return _wagerDbClient.Execute("Service_TpWagerESports_IM_Batch_Ins",
            new { _Info = JsonConvert.SerializeObject(wagers) });
    }
}