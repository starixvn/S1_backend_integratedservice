using System;
using System.Collections.Generic;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore
{
    public interface ITpBetRecordProvider
    {
        TpBetRecordDetailModel GetTpBetRecordDetail(int TpId, int GameType, string TransId, DateTime SettledTime);

        List<GameCasinoOption> GetGameCasinoList(int tpId);

        string GetTpName(int tpId);

        IReadOnlyDictionary<string, object> DbQueryFirst(string spName, object parameters, DataBase dataBase);

        IEnumerable<IReadOnlyDictionary<string, object>> DbQuery(string spName, object parameters, DataBase dataBase);
    }
}
