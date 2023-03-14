using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface IServerInfoProvider
    {
        string getSiteName(int siteId);

        DefTpCurrencyId GetDefaultTpCurrencyIdByTpId(int siteId, int tpId);
    }
}