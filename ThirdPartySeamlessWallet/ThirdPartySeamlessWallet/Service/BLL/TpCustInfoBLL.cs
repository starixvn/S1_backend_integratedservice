using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
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
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly IMemoryCache cache;
        private readonly Tools tools;
        private readonly int[] hasBonusWallet;

        public TpCustInfoBLL(IMemoryCache cache, IConfiguration configuration, WalletService walletService, TpCustInfoService tpCustInfoService, ThirdPartyBLL thirdPartyBLL, Tools tools)
        {
            this.walletService = walletService;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoService = tpCustInfoService;
            this.cache = cache;
            this.tools = tools;
            hasBonusWallet = configuration.GetSection("HasBonusWallet").Get<int[]>();
        }
        public TpCustBalance GetBalanceByHalfSecCache(int tpId, string tpUserName, int gameType = 0)
        {
            string key = $"TpCustBalance_{tpId}_{tpUserName}";
            var tpCustBalance = cache.Get<TpCustBalance>(key);
            if (tpCustBalance == null)
            {
                tpCustBalance = GetBalance(tpId, tpUserName, gameType);
                if (tpCustBalance != null)
                    cache.Set(key, tpCustBalance, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.5)));
            }

            return tpCustBalance;
        }

        public TpCustBalance GetBalance(int tpId, string tpUserName, int gameType = 0)
        {
            var tpCustBalance = new TpCustBalance();
            var tpCustInfo = GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var balance = walletService.GetBalance(tpCustInfo.CustId);
            var bonusCategoryId = thirdPartyBLL.GetBonusCategoryId(gameType);

            if (hasBonusWallet.Any(x => x == tpCustInfo.SiteId) && bonusCategoryId > 0)
            {
                balance += walletService.GetBonus(tpCustInfo.CustId, bonusCategoryId);
            }

            tpCustBalance.Balance = balance / tpCurrency.ExchangeRate;
            tpCustBalance.Balance = tools.Floor(tpCustBalance.Balance, 2);
            tpCustBalance.ExchangeRate = tpCurrency.ExchangeRate;
            tpCustBalance.TpCurrencyId = tpCurrency.TpCurrencyId;
            return tpCustBalance;
        }
        public TpCustInfo GetTpCustInfo(int tpId, string tpUserName)
        {
            string key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = tpCustInfoService.GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return tpCustInfo;
        }
    }
}
