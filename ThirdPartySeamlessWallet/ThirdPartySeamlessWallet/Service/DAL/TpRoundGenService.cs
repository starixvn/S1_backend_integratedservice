using MySql.Data.MySqlClient;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL;

public class TpRoundGenService
{
    private readonly MySqlClient _mainDbClient;

    public TpRoundGenService(IServiceProvider provider) =>
        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);

    public IEnumerable<TpRoundIdGen> GetExistRoundId(Tp tpId, GameType gameType, string tpRoundId)
    {
        return _mainDbClient.Query<TpRoundIdGen>("Service_TpRoundIdGenNewId_Get", parameters: new { _TpId = (int)tpId, _TpGameType = (int)gameType, _tpRoundId = tpRoundId });
    }

    public int GenNewRoundId(TpRoundIdGen data, MySqlTransaction trans)
    {
        var query = new { _TpId = (int)data.TpId, _TpGameType = (int)data.GameType, _tpRoundId = data.TpRoundId };
        return _mainDbClient.QueryFirstAsync<int>("Service_TpRoundIdGenNewId_Ins", trans, query).Result;
    }
}
