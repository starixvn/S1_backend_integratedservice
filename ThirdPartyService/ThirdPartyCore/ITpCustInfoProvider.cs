using ThirdPartyCore.Model;

namespace ThirdPartyCore
{
    public interface ITpCustInfoProvider
    {
        void SetCustGotoGame(int custId, int tpId, int gameType);

        TpCountryCode GetTpCurrencyCodeByCurrencyId(int tpId, string tpCurrencyId);
    }
}
