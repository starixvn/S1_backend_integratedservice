using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public class ThirdPartyBLL
    {
        private readonly ThirdPartyService thirdPartyService;
        private readonly IMemoryCache cache;
        private ConcurrentDictionary<string, TpCurrency> TpCurrencyList;
        private static ConcurrentDictionary<int, ThirdPartySetting> ThirdPartySettingContainer = new ConcurrentDictionary<int, ThirdPartySetting>();
        private static ConcurrentDictionary<string, ThirdPartySetting> ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartySetting>();
        public ThirdPartyBLL(IMemoryCache cache, ThirdPartyService thirdPartyService)
        {
            this.thirdPartyService = thirdPartyService;
            this.cache = cache;
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
        }
        public ThirdPartySetting GetThirdPartySetting(int tpId)
        {
            if (!ThirdPartySettingContainer.ContainsKey(tpId))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartySetting(tpId);
                if (thirdPartyModel != null)
                    ThirdPartySettingContainer.TryAdd(tpId, thirdPartyModel);
            }

            return ThirdPartySettingContainer.ContainsKey(tpId) ? ThirdPartySettingContainer[tpId] : null;
        }

        public ThirdPartySetting GetThirdPartyBySite(int tpId, int siteId)
        {
            string sKet = $"{tpId}_{siteId}";
            if (!ThirdPartyModelContainer.ContainsKey(sKet))
            {
                var thirdPartyModel = thirdPartyService.GetThirdPartyBySite(tpId, siteId);
                if (thirdPartyModel != null)
                    ThirdPartyModelContainer.TryAdd(sKet, thirdPartyModel);
            }

            return ThirdPartyModelContainer.ContainsKey(sKet) ? ThirdPartyModelContainer[sKet] : null;
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

        public TpCurrency GetTpCurrency(int tpId, int currencyId)
        {
            string key = $"{tpId}_{currencyId}";
            if (!TpCurrencyList.ContainsKey(key))
            {
                TpCurrencyList.TryAdd(key, thirdPartyService.GetTpCurrency(tpId, currencyId));
            }

            var tpCurrency = TpCurrencyList[key];
            return tpCurrency;
        }
        private Dictionary<int, int> GetBonusCategoryList()
        {
            string key = $"GetBonusCategoryList";
            var dBonusCategoryList = cache.Get<Dictionary<int, int>>(key);
            if (dBonusCategoryList == null)
            {
                dBonusCategoryList = new Dictionary<int, int>();

                var bonusCategoryList = thirdPartyService.GetBonusCategoryList();
                foreach (var bonusCategory in bonusCategoryList.Where(x => x.TpId == 0))
                {
                    dBonusCategoryList.Add(bonusCategory.Type, bonusCategory.Id);
                }

                cache.Set(key, dBonusCategoryList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }

            return dBonusCategoryList;
        }

        public BonusMinBet GetBonusMinBet(int gameType, int currencyId)
        {
            string key = $"BonusMinBetList";
            var dBonusMinBetList = cache.Get<IEnumerable<BonusMinBet>>(key);
            if (dBonusMinBetList == null)
            {
                dBonusMinBetList = thirdPartyService.GetBonusMinBetList();
                cache.Set(key, dBonusMinBetList, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(5)));
            }

            return dBonusMinBetList.FirstOrDefault(x => x.GameType == gameType && x.CurrencyId == currencyId);
        }

        public int GetBonusCategoryId(int gameType)
        {
            if (gameType == 0)
                return 0;

            var bonusCategoryList = GetBonusCategoryList();

            if (bonusCategoryList.ContainsKey(gameType))
                return bonusCategoryList[gameType];
            else
                return 0;
        }
        public GameType GetGameType_PP(string gameId)
        {
            GameType gameType = GameType.PP_RNG;

            if (int.TryParse(gameId, out int tempGameId))
            {
                gameType = GameType.PP_LiveCasino;
            }

            return gameType;
        }
        public GameType GetGameType_SG(string gameId)
        {
            GameType gameType = GameType.SG_RNG;

            if (gameId.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.SG_Fish;
            }

            return gameType;
        }
        public GameType GetGameType(int tpId, string gameId)
        {
            GameType gameType = 0;
            switch (tpId)
            {
                case (int)Tp.One789:
                    if (gameId.StartsWith("0_", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.One789_Lottery;
                    }
                    else if (gameId.StartsWith("1_", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.One789_Lottery_789;
                    }
                    else if (gameId.StartsWith("2_", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.One789_LiveCasino;
                    }
                    break;
                case (int)Tp.Iconic:
                    if (gameId.StartsWith("cmf", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.Iconic_Fish;
                    }
                    else if (gameId.StartsWith("cmt", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.Iconic_Fish;
                    }
                    else if (gameId.StartsWith("cm", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.Iconic_RNG;
                    }
                    break;
                case (int)Tp.CQ9:
                    gameType = GameType.CQ9_RNG;
                    if (gameId == "AB3" || gameId == "AT01" || gameId == "AT04" || gameId == "AT05" || gameId == "AT06")
                    {
                        gameType = GameType.CQ9_Fish;
                    }
                    break;
                case (int)Tp.JILI:
                    gameType = GameType.JILI_RNG;

                    List<int> fish = new List<int> { 1, 20, 42, 32, 60, 74 };
                    if (fish.Contains(Convert.ToInt32(gameId)))
                    {
                        gameType = GameType.JILI_Fish;
                    }
                    break;
                case (int)Tp.PP:
                    gameType = GameType.PP_RNG;
                    if (int.TryParse(gameId, out int tempGameId))
                    {
                        gameType = GameType.PP_LiveCasino;
                    }
                    break;
                case (int)Tp.SG:
                    gameType = GameType.SG_RNG;
                    if (gameId.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.SG_Fish;
                    }
                    break;
                case (int)Tp.Joker:
                    gameType = GameType.Joker_RNG;
                    var jokerfish = new List<string>
                    {
                        "kk8nqm3cfwtng",
                        "st5cmuqnaxycn",
                        "1jeqx59c7ztqg"
                    };
                    if (jokerfish.Contains(gameId))
                    {
                        gameType = GameType.Joker_Fish;
                    }
                    break;
                case (int)Tp.MG:
                    gameType = GameType.MG_RNG;
                    var casino = GetGameCasinoWithCache(tpId);
                    var check = casino.FirstOrDefault(x => x.GameId == gameId);
                    if (check == null)
                    {
                        gameType = GameType.MG_LiveCasino;
                    }
                    break;
                case (int)Tp.EVO:
                    gameType = GameType.EVO_LiveCasino;
                    var netent = GetGameCasinoWithCache((int)Tp.Netent);
                    var chkNetent = netent.FirstOrDefault(x => x.GameId == gameId);
                    if (chkNetent != null && chkNetent.GameId == gameId)
                    {
                        gameType = GameType.Netent_RNG;
                        return gameType;
                    }
                    var redtiger = GetGameCasinoWithCache((int)Tp.RedTiger);
                    var chkRedtiger = redtiger.FirstOrDefault(x => x.GameId == gameId);
                    if (chkRedtiger != null && chkRedtiger.GameId == gameId)
                    {
                        gameType = GameType.RedTiger_RNG;
                        return gameType;
                    }
                    break;
                default:
                    break;
            }
            return gameType;
        }

        public IEnumerable<TpGameCasino> GetGameCasinoWithCache(int tpId)
        {
            string key = $"TpGameCasino_{tpId}";
            var tpGameCasino = cache.Get<IEnumerable<TpGameCasino>>(key);
            if (tpGameCasino == null)
            {
                tpGameCasino = thirdPartyService.GetGameCasino(tpId);
                cache.Set(key, tpGameCasino, new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(30)));
            }

            return tpGameCasino;
        }

        public SettlementType GetGameCasinoSettlementType(int tpId, string gameId)
        {
            SettlementType settlementType = SettlementType.SettledForValid;
            var casino = GetGameCasinoWithCache(tpId);
            var check = casino.FirstOrDefault(x => x.GameId == gameId && x.TypeId == 3);
            if (check != null)
            {
                settlementType = SettlementType.SettledForValidNotRollover;
            }
            return settlementType;
        }

        public int AddTpBetFailureRecord(TpBetFailureRecord tpBetFailureRecord)
        {
            return thirdPartyService.AddTpBetFailureRecord(tpBetFailureRecord);
        }
    }
}
