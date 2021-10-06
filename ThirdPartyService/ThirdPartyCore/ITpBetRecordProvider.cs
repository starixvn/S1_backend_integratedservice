using System.Collections.Generic;
using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface ITpBetRecordProvider
    {
        TpBetRecordDetailModel GetTpBetRecordDetail(GetBetDetail getBetDetail);

        List<GameCasinoOption> GetGameCasinoList(int tpId);

        IReadOnlyDictionary<string, object> GetWagerDetail(string spName, object parameters = null);

        IReadOnlyDictionary<string, object> GetTpTableData(string spName, object parameters = null);
    }
}
