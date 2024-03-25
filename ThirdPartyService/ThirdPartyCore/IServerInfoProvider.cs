using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IServerInfoProvider
    {
        string GetSiteName(int siteId);

        DefTpCurrencyId GetDefaultTpCurrencyIdByTpId(int siteId, int tpId);
    }
}