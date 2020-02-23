using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class CustBLL
    {
        private readonly WalletService walletService;
        private readonly IMemoryCache cache;

        public CustBLL(IMemoryCache cache, WalletService walletService)
        {
            this.walletService = walletService;
            this.cache = cache;
        }
        public decimal GetBalance(int custId)
        {
            string key = $"Balance_{custId}";
            var balance = cache.Get<decimal?>(key) ?? -1m;
            if (balance == -1m)
            {
                balance = walletService.GetBalance(custId);
                cache.Set(key, balance, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromSeconds(0.5)));
            }

            return balance;
        }
    }
}
