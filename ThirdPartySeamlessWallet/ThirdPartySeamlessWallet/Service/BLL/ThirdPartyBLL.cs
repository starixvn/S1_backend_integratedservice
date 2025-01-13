using System.Collections.Concurrent;

using Microsoft.Extensions.Caching.Memory;

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
        private ConcurrentDictionary<string, ServerInfo> ServerInfoData;
        private static ConcurrentDictionary<int, ThirdPartySetting> ThirdPartySettingContainer = new ConcurrentDictionary<int, ThirdPartySetting>();
        private static ConcurrentDictionary<string, ThirdPartySetting> ThirdPartyModelContainer = new ConcurrentDictionary<string, ThirdPartySetting>();

        public ThirdPartyBLL(IServiceProvider provider)
        {
            thirdPartyService = provider.GetRequiredService<ThirdPartyService>();
            cache = provider.GetRequiredService<IMemoryCache>();
            TpCurrencyList = new ConcurrentDictionary<string, TpCurrency>();
            ServerInfoData = new ConcurrentDictionary<string, ServerInfo>();
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

        public GameType GetGameType_PP(string gameId)
        {
            GameType gameType = GameType.PP_RNG;

            if (int.TryParse(gameId, out int tempGameId))
            {
                gameType = GameType.PP_LiveCasino;

                // Spaceman
                if (tempGameId == 1301 || tempGameId == 1302)
                {
                    gameType = GameType.PP_RNG;
                }
                return gameType;
            }

            var casino = GetGameCasinoWithCache((int)Tp.PP);
            var check = casino.FirstOrDefault(x => x.GameId == gameId);
            if (check == null)
            {
                gameType = GameType.PP_LiveCasino;
            }

            return gameType;
        }

        public GameType GetGameType_SG(string gameId)
        {
            var gameType = GameType.SG_RNG;

            if (gameId.StartsWith("F-", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.SG_Fish;
            }
            else if (gameId.StartsWith("C-", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.SG_Table;
            }

            return gameType;
        }

        public GameType GetGameType(int tpId, string gameId)
        {
            GameType gameType = 0;
            switch (tpId)
            {
                case (int)Tp.FaChai:
                    gameType = GameType.FaChai_RNG;
                    break;

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

                    if (gameId == "CMCOCKFIGHT01" || gameId == "COCKFIGHT01")
                    {
                        gameType = GameType.CQ9_WCC;
                    }
                    break;

                case (int)Tp.PP:
                    gameType = GameType.PP_RNG;

                    if (gameId.Equals("pragmaticplaycasino", StringComparison.OrdinalIgnoreCase))
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
                    var nlc = GetGameCasinoWithCache((int)Tp.NLC);
                    var chkNLC = nlc.FirstOrDefault(x => x.GameId == gameId);
                    if (chkNLC != null && chkNLC.GameId == gameId)
                    {
                        gameType = GameType.NLC_RNG;
                        return gameType;
                    }
                    break;

                case (int)Tp.Skywind:
                    gameType = GameType.Skywind_RNG;
                    if (gameId == "0")
                    {
                        gameType = GameType.Skywind_LiveCasino;
                    }
                    else if (gameId == "sw_or" || gameId == "sw_fj" || gameId == "sw_cashdaha")
                    {
                        gameType = GameType.Skywind_Fish;
                    }
                    break;

                case (int)Tp.GPI:
                    gameType = GameType.GPI_RNG;
                    if (string.Equals(gameId, "keno", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.GPI_Keno;
                    }
                    else if (string.Equals(gameId, "sode", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.GPI_Sode;
                    }
                    else if (string.Equals(gameId, "thailottery", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.GPI_Thai_Lottery;
                    }
                    else if (string.Equals(gameId, "fast3", StringComparison.OrdinalIgnoreCase))
                    {
                        gameType = GameType.GPI_Fast3;
                    }
                    break;

                case (int)Tp.PlayTech:
                    gameType = GameType.PlayTech_RNG;
                    var casinoPlayTech = GetGameCasinoWithCache(tpId);
                    /*
                     * 對於 BRL 貨幣
                     * 必須使用 PTN 遊戲代碼才能存取遊戲
                     * 測試環境和正式環境使用的遊戲程式碼不同
                     */
                    if (gameId.StartsWith("ptn_", StringComparison.OrdinalIgnoreCase))
                    {
                        // 去除ptn_，比對原gameid是否存在
                        gameId = gameId.Substring(4);
                    }
                    var checkPlayTech = casinoPlayTech.FirstOrDefault(x => x.GameId == gameId);
                    if (checkPlayTech == null)
                    {
                        gameType = GameType.PlayTech_LiveCasino;
                    }
                    break;

                case (int)Tp.MaxGame:
                    gameType = GameType.MaxGame_Poker;
                    break;

                case (int)Tp.SabaPlay:
                    gameType = GameType.SabaPlay_Poker;
                    break;

                case (int)Tp.UuSlots:
                    gameType = GameType.UuSlots_RNG;
                    break;

                case (int)Tp.Live22:
                    gameType = GameType.Live22_RNG;
                    break;

                case (int)Tp.FastSpin:
                    gameType = GameType.FastSpin_RNG;
                    break;

                case (int)Tp.JILI:
                    gameType = GameType.JILI_RNG;

                    //List<int> fish = new List<int> { 1, 20, 42, 32, 60, 74 };
                    //if (fish.Contains(Convert.ToInt32(gameId)))
                    //{
                    //    gameType = GameType.JILI_Fish;
                    //}
                    break;

                case (int)Tp.Allbet:
                    gameType = GameType.Allbet_LiveCasino;
                    break;

                case (int)Tp.RNGKeno:
                    gameType = GameType.RNG_Keno;
                    break;

                case (int)Tp.RNGTableGame:
                    gameType = GameType.TableGame_RNG;
                    break;

                case (int)Tp.Lottery:
                    gameType = GameType.Lottery;
                    break;

                case (int)Tp.AESexy:
                    gameType = GameType.AESexy_LiveCasino;
                    break;

                case (int)Tp.PG:
                    gameType = GameType.PG_RNG;
                    break;

                case (int)Tp.WE:
                    gameType = GameType.WE_LiveCasino;
                    break;
                case (int)Tp.Nextspin:
                    gameType = GameType.Nextspin_RNG;
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

            //Kiss918(21)
            //PNG(54)
            var check = tpId is 21 or 54 ? casino.FirstOrDefault(x => x.AppId == gameId && x.TypeId == 3)
                : casino.FirstOrDefault(x => x.GameId == gameId && x.TypeId == 3);
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

        public IEnumerable<FinancialLog> GetFinancialLogTransactionCheck(string RefTransId, string YYYYMM)
        {
            return thirdPartyService.GetFinancialLogTransactionCheck(RefTransId, YYYYMM);
        }

        public IEnumerable<BonusLog> GetBonusLogTransactionCheck(string RefTransId)
        {
            return thirdPartyService.GetBonusLogTransactionCheck(RefTransId);
        }

        public ServerInfo GetServerInfoBySiteName(string siteName)
        {
            string key = $"ServerInfo_{siteName}";
            if (!ServerInfoData.ContainsKey(key))
            {
                var ServerInfoModel = thirdPartyService.GetServerInfoBySiteName(siteName);
                if (ServerInfoModel != null)
                {
                    ServerInfoData.TryAdd(key, ServerInfoModel);
                }
            }
            return ServerInfoData.ContainsKey(key) ? ServerInfoData[key] : null;
        }
    }
}