using System.Collections.Generic;
using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface ITpBetRecordProvider
    {
        TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetail getBetDetail);

        List<GameCasinoOption> GetGameCasinoList(int tpId);

        IReadOnlyDictionary<string, object> WagerDbQueryFirst(string spName, object parameters = null);

        IReadOnlyDictionary<string, object> ReportDbQueryFirst(string spName, object parameters = null);

        IEnumerable<IReadOnlyDictionary<string, object>> WagerDbQuery(string spName, object parameters = null);

        IEnumerable<IReadOnlyDictionary<string, object>> ReportDbQuery(string spName, object parameters = null);
    }
}
