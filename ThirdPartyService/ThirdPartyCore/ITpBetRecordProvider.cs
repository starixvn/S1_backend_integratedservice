using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;

namespace ThirdPartyCore
{
    public interface ITpBetRecordProvider
    {
        TpBetRecordDetailModel GetTpBetRecordDetail(int TpId, int GameType, string TransId, DateTime SettledTime);

        List<GameCasinoOption> GetGameCasinoList(int tpId);

        string GetTpName(int tpId);

        IReadOnlyDictionary<string, object> DbQueryFirst(string spName, object parameters, DataBase dataBase);

        IEnumerable<IReadOnlyDictionary<string, object>> DbQuery(string spName, object parameters, DataBase dataBase);

        GameNameInfo GetGameNameInfo(int tpId, string gameId, string bettype, string lang);

        GameNameInfo GetGameNameInfo(int tpId, string gameId, string lang);
    }
}
