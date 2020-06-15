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
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoService tpCustInfoService;
        private readonly IMemoryCache cache;
        private readonly Tools tools;

        public TpCustInfoBLL(IMemoryCache cache, WalletService walletService, TpCustInfoService tpCustInfoService, ThirdPartyBLL thirdPartyBLL, Tools tools)
        {
            this.walletService = walletService;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoService = tpCustInfoService;
            this.cache = cache;
            this.tools = tools;
        }
        public TpCustBalance GetBalanceByHalfSecCache(int tpId, string tpUserName)
        {
            string key = $"TpCustBalance_{tpId}_{tpUserName}";
            var tpCustBalance = cache.Get<TpCustBalance>(key);
            if (tpCustBalance == null)
            {
                var tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    tpCustBalance = new TpCustBalance();
                    var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
                    var balance = walletService.GetBalance(tpCustInfo.CustId);
                    tpCustBalance.Balance = balance / tpCurrency.ExchangeRate;
                    tpCustBalance.Balance = tools.Floor(tpCustBalance.Balance, 2);
                    tpCustBalance.ExchangeRate = tpCurrency.ExchangeRate;
                    tpCustBalance.TpCurrencyId = tpCurrency.TpCurrencyId;
                }

                cache.Set(key, tpCustBalance, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.5)));
            }

            return tpCustBalance;
        }

        public TpCustBalance GetBalance(int tpId, string tpUserName)
        {
            var tpCustBalance = new TpCustBalance();
            var tpCustInfo = GetTpCustInfo(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var balance = walletService.GetBalance(tpCustInfo.CustId);
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
