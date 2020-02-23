using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class TpCustInfoBLL
    {
        private readonly WalletService walletService;
        private readonly ThirdPartyService thirdPartyService;
        private readonly IMemoryCache cache;
        private readonly Tools tools;

        public TpCustInfoBLL(IMemoryCache cache, WalletService walletService, ThirdPartyService thirdPartyService, Tools tools)
        {
            this.walletService = walletService;
            this.thirdPartyService = thirdPartyService;
            this.cache = cache;
            this.tools = tools;
        }
        public TpCustBalance GetBalance(int tpId, string tpUserName)
        {
            string key = $"TpCustBalance_{tpId}_{tpUserName}";
            var tpCustBalance = cache.Get<TpCustBalance>(key);
            if (tpCustBalance == null)
            {

                tpCustBalance = walletService.GetBalance(tpId, tpUserName);
                tpCustBalance.Balance = tools.Floor(tpCustBalance.Balance, 2);
                cache.Set(key, tpCustBalance, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.5)));
            }

            return tpCustBalance;
        }
        public TpCheck ThirdPartyCheckWithCache(int tpId, string tpUserName)
        {
            string key = $"TpCheck_{tpId}_{tpUserName}";
            var tpCheck = cache.Get<TpCheck>(key);
            if (tpCheck == null)
            {

                tpCheck = ThirdPartyCheck(tpId, tpUserName);
                cache.Set(key, tpCheck, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(40)));
            }

            return tpCheck;
        }
        public TpCheck ThirdPartyCheck(int tpId, string tpUserName)
        {
            var tpCheck = thirdPartyService.ThirdPartyCheck(tpId, tpUserName);
            return tpCheck;
        }
    }
}
