using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL;

public class WagerIMService
{
    private readonly MySqlClient _wagerDbClient;
    private readonly ILogger<WagerIMService> _logger;

    public WagerIMService(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<WagerIMService>>();
        _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
    }

    public int InsertSlotWager(IEnumerable<IMSlotWager> wagers)
    {
        var json = JsonConvert.SerializeObject(wagers);
        _logger.LogInformation(json);

        return _wagerDbClient.Execute("Service_TpWagerRng_SPRIBE_Batch_Ins",
            new { _Infos = json });
    }

    public int InsertSportsWager(IEnumerable<IMSportsWager> wagers)
    {
        var json = JsonConvert.SerializeObject(wagers);
        _logger.LogInformation(json);

        return _wagerDbClient.Execute("Service_TpWagerSports_IM_Batch_Ins",
            new { _Infos = json });
    }

    public int InsertESportsWager(IEnumerable<IMESportsWager> wagers)
    {
        var json = JsonConvert.SerializeObject(wagers);
        _logger.LogInformation(json);

        return _wagerDbClient.Execute("Service_TpWagerESports_IM_Batch_Ins",
            new { _Infos = json });
    }
}