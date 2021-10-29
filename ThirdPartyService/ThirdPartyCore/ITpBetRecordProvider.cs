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

        IEnumerable<T> WagerDbQuery<T>(string spName, object parameters = null);

        IEnumerable<T> ReportDbQuery<T>(string spName, object parameters = null);
    }
}
