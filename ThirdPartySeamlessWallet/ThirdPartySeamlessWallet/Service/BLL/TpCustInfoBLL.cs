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
        public TpCustInfoBLL(IMemoryCache cache, IConfiguration configuration, WalletService walletService, TpCustInfoService tpCustInfoService, ThirdPartyBLL thirdPartyBLL, Tools tools)
        {
            this.walletService = walletService;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoService = tpCustInfoService;
            this.cache = cache;
            this.tools = tools;
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
            var tpCustInfo = GetTpCustInfoWithCache(tpId, tpUserName);
            var tpCurrency = thirdPartyBLL.GetTpCurrency(tpId, tpCustInfo.CurrencyId);
            var balance = walletService.GetBalance(tpCustInfo.CustId);
            var bonusCategoryId = thirdPartyBLL.GetBonusCategoryId(gameType);

            if (bonusCategoryId > 0)
            {
                var bonusAmount = walletService.GetBonus(tpCustInfo.CustId, bonusCategoryId);
                balance += bonusAmount;
            }

            tpCustBalance.Balance = balance / tpCurrency.ExchangeRate;
            tpCustBalance.Balance = tools.Floor(tpCustBalance.Balance, 4);
            tpCustBalance.ExchangeRate = tpCurrency.ExchangeRate;
            tpCustBalance.TpCurrencyId = tpCurrency.TpCurrencyId;
            return tpCustBalance;
        }
        public TpCustInfo GetTpCustInfoWithCache(int tpId, string tpUserName)
        {
            string key = $"TpCustInfo_{tpId}_{tpUserName}";
            var tpCustInfo = cache.Get<TpCustInfo>(key);
            if (tpCustInfo == null)
            {
                tpCustInfo = GetTpCustInfo(tpId, tpUserName);
                if (tpCustInfo != null)
                {
                    cache.Set(key, tpCustInfo, new MemoryCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(1)));
                }
            }

            return tpCustInfo;
        }
        public TpCustInfo GetTpCustInfo(int tpId, string tpUserName)
        {
            var tpCustInfo = tpCustInfoService.GetTpCustInfo(tpId, tpUserName);
            return tpCustInfo;
        }
        public int SetCustGotoGame(int custId, int tpId, int gameType)
        {
            return tpCustInfoService.SetCustGotoGame(custId, tpId, gameType);
        }
        public int GetCustGotoGameType(int custId, int tpId)
        {
            return tpCustInfoService.GetCustGotoGameType(custId, tpId);
        }
        public int SetTpPwd(int tpId, string tpUserName, string tpPwd)
        {
            return tpCustInfoService.SetTpPwd(tpId, tpUserName, tpPwd);
        }
        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            tpCustInfoService.AddLoginRecord(custId, siteId, ip, out token);
        }
        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            tpCustInfoService.AddUserLoginLog(custId, userName, siteId, loginIp, fromUrl, userAgent);
        }
    }
}
