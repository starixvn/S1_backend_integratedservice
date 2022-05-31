using System.Collections.Concurrent;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyService.Service.DAL;

namespace ThirdPartyService.Service.BLL
{
    public class TpCustInfoBLL : ITpCustInfoProvider
    {
        private readonly TpCustInfoService tpCustInfoService;
        private ConcurrentDictionary<string, TpCountryCode> tpCountryCodeCache = new ConcurrentDictionary<string, TpCountryCode>();


        public TpCustInfoBLL(TpCustInfoService tpCustInfoService)
        {
            this.tpCustInfoService = tpCustInfoService;
        }
        public void SetTpLastLoginTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastLoginTime(custId, tpId);
        }
        public void SetTpLastTradingTime(int custId, int tpId)
        {
            tpCustInfoService.SetTpLastTradingTime(custId, tpId);
        }
        public void SetCustGotoGame(int custId, int tpId, int gameType)
        {
            tpCustInfoService.SetCustGotoGame(custId, tpId, gameType);
        }

        public TpCountryCode GetTpCurrencyCodeByCurrencyId(int tpId, string tpCurrencyId)
        {
            var key = $"GetTpCurrencyCodeByCurrencyId{tpId}{tpCurrencyId}";
            if (tpCountryCodeCache.ContainsKey(key))
            {
                return tpCountryCodeCache[key];
            }
            else
            {
                var tpCountryCode = tpCustInfoService.GetTpCurrencyCodeByCurrencyId(tpId, tpCurrencyId);
                tpCountryCodeCache.TryAdd(key, tpCountryCode);
                return tpCountryCode;
            }
        }
    }
}
