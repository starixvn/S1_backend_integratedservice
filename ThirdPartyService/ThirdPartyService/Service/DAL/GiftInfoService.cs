using Dapper;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;
using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.DAL;

public class GiftInfoService
{
    private readonly MySqlClient _mainDbClient;
    private readonly MySqlClient _reportDbClient;

    public GiftInfoService(IServiceProvider serviceProvider)
    {
        _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _reportDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Report);
    }

    public IEnumerable<GiftInfo> GetGiftList(string tpid)
    {
        var p = new DynamicParameters(new 
        { 
            _TpId = tpid 
        });
        return _reportDbClient.Query<GiftInfo>("Service_GiftList_Get_V2", p);
    }

    public IEnumerable<WorkerPeriodTask> GetWorkList(string workerName)
    {
        var p = new DynamicParameters(new
        {
            _workerName = workerName
        });
        return _reportDbClient.Query<WorkerPeriodTask>("Service_WorkerPeriod_Get", p);
    }

    public int SetTaskDone(int periodTaskSiteId, int periodTaskCurrencyId, string workerName)
    {
        var p = new DynamicParameters(new
        {
            _SiteId = periodTaskSiteId,
            _CurrencyId = periodTaskCurrencyId,
            _WorkerName = workerName
        });
        return _mainDbClient.Execute("Service_WorkerPeriod_TaskDone_Set",p);
    }

    public int SetTpBonusCode(int siteId, int tpId,string gameId,string TpBonusCodeId,string bonusCode, DateTime expirationDate)
    {
        var p = new DynamicParameters(new
        {
            _SiteId = siteId,
            _TpId = tpId,
            _GameId = gameId,
            _BonusCode = bonusCode,
            _TpBonusCodeId = TpBonusCodeId,
            _ExpirationDate = expirationDate
        });
        return _mainDbClient.Execute("Service_TpBonusCode_Set", p);
    }
}