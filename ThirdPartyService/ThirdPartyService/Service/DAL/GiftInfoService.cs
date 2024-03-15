using Dapper;
using System.Reflection.Metadata;
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

    public IEnumerable<GiftInfo> GetGiftList(int tpid)
    {
        var p = new DynamicParameters(new 
        { 
            _TpId = tpid 
        });
        return _reportDbClient.Query<GiftInfo>("Service_GiftList_Get",p);
    }
}