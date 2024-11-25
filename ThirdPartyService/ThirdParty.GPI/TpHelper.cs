using Microsoft.Extensions.DependencyInjection;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.GPI
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly GPIConfig _siteConfig;

        public TpHelper(IServiceProvider provider)
        {
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<GPIConfig>(TpEnum.GPI.ToString());
        }

        private readonly Dictionary<int, string> ErrorMassage = new()
        {
            { -1, "Unknown error" },
            { -2, "Customer not found" },
            { -7, "Customer is blocked" },
            { -27, "Data type mismatch" },
            { -29, "Currency mismatch" },
            { -33, "Invalid amount" },
            { -48, "Invalid password" },
            { -119, "Missing input parameter" },
            { -201, "Transaction not exist" },
            { -202, "Operator not exist" }
        };

        private class LoginResult
        {
            public int error_code { get; set; }
            public string error_msg { get; set; }
            public string cust_id { get; set; }
            public string cust_name { get; set; }
            public string currency_code { get; set; }
            public string language { get; set; }
            public string country { get; set; }
            public string ip { get; set; }
            public string date_of_birth { get; set; }
            public bool test_cust { get; set; }
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult oApiResult = new ApiResult();
            oApiResult.ErrorCode = ErrorCode.Success;
            return oApiResult;
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var config = _siteConfig;

            string RslotUrl = config.RslotUrl;
            string LiveCasinoUrl = config.LiveCasinoUrl;
            string LotteryUrl = config.LotteryUrl;
            string P2PUrl = config.P2PUrl;
            string OperatorId = config.OperatorId;

            string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
            string cashierUrl = inputs.ContainsKey("CashierUrl") && !string.IsNullOrEmpty(inputs["CashierUrl"]) ? inputs["CashierUrl"] : "";
            string lobbyUrl = inputs.ContainsKey("LobbyUrl") && !string.IsNullOrEmpty(inputs["LobbyUrl"]) ? inputs["LobbyUrl"] : "";
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            int tpGameType = inputs.ContainsKey("tpGameType") && !string.IsNullOrEmpty(inputs["tpGameType"]) ? Convert.ToInt32(inputs["tpGameType"]) : (int)TpGameType.GPI_RNG;

            ApiResult oApiResult = new ApiResult();
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{RslotUrl}{gameId}?fun={fun}&op={OperatorId}&lang={TpLangId}&fundsURL={cashierUrl}&homeURL={homeUrl}&lobbyURL={lobbyUrl}";
                return oApiResult;
            }

            var applyTokenInputs = FromJson<Dictionary<string, string>>(ToJson(tpCustInfo));
            applyTokenInputs.Add("TpLangId", TpLangId);
            applyTokenInputs.Add("ClientIP", inputs["ClientIP"]);

            string token = ApplyToken(applyTokenInputs);

            oApiResult.ErrorCode = ErrorCode.Success;
            switch (tpGameType)
            {
                case (int)TpGameType.GPI_RNG:
                    oApiResult.Message = $"{RslotUrl}{gameId}?token={token}&fun={fun}&op={OperatorId}&lang={TpLangId}&fundsURL={cashierUrl}&homeURL={homeUrl}&lobbyURL={lobbyUrl}&hidelobby=1";
                    break;
                case (int)TpGameType.GPI_Keno:
                case (int)TpGameType.GPI_Sode:
                case (int)TpGameType.GPI_Thai_Lottery:
                case (int)TpGameType.GPI_Fast3:
                    oApiResult.Message = $"{LotteryUrl}Login/?lang={TpLangId}&vendor={OperatorId}&ticket={token}&game={gameId}";
                    break;
                    //case "livecasino":
                    //    if (appType == AppType.Native)
                    //    {
                    //        oApiResult.Message = $"{LiveCasinoUrl}v2?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    //    }
                    //    else if (appType == AppType.Mobile)
                    //    {
                    //        oApiResult.Message = $"{LiveCasinoUrl}html5/mobile?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    //    }
                    //    else
                    //    {
                    //        oApiResult.Message = $"{LiveCasinoUrl}html5/casino?token=​{token}​&op={OperatorId}&lang={TpLangId}&sys=CUSTOM";
                    //    }
                    //    break;
                    //case "p2p":
                    //    oApiResult.Message = $"{P2PUrl}{gameId}/?op={OperatorId}&lang={TpLangId}&fun={fun}&token={token}";
                    //    break;
            }
            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };

            var spName = (getBetDetail.TpGameTypeOption.CategoryId == TpCategory.Casino) ? "Service_BettransRng_GPI_Detail_Get" : "Service_TpWagerLT_GPI_Detail_Get";
            var db = (getBetDetail.TpGameTypeOption.CategoryId == TpCategory.Casino) ? DataBase.Report : DataBase.Wager;

            var betList = TpBetRecordProvider.DbQuery(spName, new { getBetDetail.TransId }, db);
            if (betList == null)
            {
                return new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    TpCurrency = tpCurrency,
                };
            }

            var detailList = (getBetDetail.TpGameTypeOption.CategoryId == TpCategory.Casino) ? betList.Where(i => (sbyte)i.GetValueOrDefault("type") != 2) : betList;
            var detail = ToJson(detailList);
            string gameCode = detailList.FirstOrDefault()?.GetValueOrDefault("game_id")?.ToString();

            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Casino => new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = string.Empty,
                    TpUserName = detailList.FirstOrDefault()?.GetValueOrDefault("cust_id")?.ToString(),
                    UTCBetTime = detailList.Min(i => (DateTime)i.GetValueOrDefault("CreateTime", DateTime.MinValue)),
                    UTCSettledTime = detailList.Max(i => (DateTime)i.GetValueOrDefault("CreateTime", DateTime.MinValue)),
                    Stake = detailList.Where(i => (sbyte)i.GetValueOrDefault("type") == 1)
                        .Sum(i => Math.Abs((decimal)i.GetValueOrDefault("amount"))) * tpCurrency.ExchangeRate,
                    WinlostAmount = detailList.Sum(i => (decimal)i.GetValueOrDefault("amount") + (decimal)i.GetValueOrDefault("jackpot_win")) * tpCurrency.ExchangeRate,
                    GameCode = gameCode,
                    GameName = GetGameName(gameCode),
                },
                TpCategory.Lottery => new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = detailList.FirstOrDefault()?.GetValueOrDefault("game_code")?.ToString(),
                    TpUserName = detailList.FirstOrDefault()?.GetValueOrDefault("cust_id")?.ToString(),
                    UTCBetTime = detailList.Min(i => (DateTime)i.GetValueOrDefault("trans_date", DateTime.MinValue)),
                    UTCSettledTime = detailList.Max(i => (DateTime)i.GetValueOrDefault("trans_date", DateTime.MaxValue)),
                    Stake = detailList.Sum(i => (decimal)i.GetValueOrDefault("bet")) * tpCurrency.ExchangeRate,
                    WinlostAmount = detailList.Where(i => (sbyte)i.GetValueOrDefault("status") == 1).Sum(i => (decimal)i.GetValueOrDefault("winlose")) * tpCurrency.ExchangeRate,
                },
                _ => new BaseDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                }
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var spName = (tpGameType.CategoryId == TpCategory.Casino) ? "Service_BettransRng_GPI_Detail_Get" : "Service_TpWagerLT_GPI_Detail_Get";
            var db = (tpGameType.CategoryId == TpCategory.Casino) ? DataBase.Main : DataBase.Wager;
            var detailList = TpBetRecordProvider.DbQuery(spName, new { betRecordInfo.TransId }, db);
            var gameCode = detailList.GetFirstStringValue("game_id");
            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = detailList.GetFirstStringValue("round_id"),
                    Title = tpGameType.TypeName,
                    MatchID = detailList.GetFirstStringValue("round_id"),
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = GetDeviceType(detailList.GetFirstStringValue("platform")),
                    IP = string.Empty,
                },
                TpCategory.Lottery => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = detailList.GetFirstStringValue("game_id"),
                    Game = detailList.GetFirstStringValue("game_type"),
                    RefNo = detailList.GetFirstStringValue("game_code"),
                    Title = tpGameType.TypeName,
                    MatchID = detailList.GetFirstStringValue("table_id"),
                    BetType = string.Empty,
                    BetChoice = ToJson(detailList.Select(s => s.GetStringValue("player_hand"))),
                    Result = detailList.GetFirstStringValue("game_result"),
                    DeviceType = GetDeviceType(detailList.GetFirstStringValue("platform")),
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        private static string GetGameName(string gameCode)
        {
            return gameCode switch
            {
                "200008" => "Lantern Festival",
                "200007" => "Legend of Nezha",
                "200015" => "Golden Eggs",
                "200011" => "Zeus",
                "200017" => "World Of Warlords",
                "200018" => "Pharaoh",
                "200022" => "Qixi",
                "200021" => "Samurai Sushi",
                "200023" => "Fortune Cat",
                "200025" => "Dimsumlicious",
                "200028" => "God Of Gamblers",
                "200030" => "Seven Wonders",
                "200001" => "Underwater World",
                "200004" => "Baseball",
                "200002" => "Monkey King",
                "200003" => "Bikini Beach",
                "200005" => "Little Monsters",
                "200006" => "Fruitilicious",
                "200009" => "Desert Oasis",
                "200012" => "Mafia",
                "200013" => "Forbidden Chamber",
                "200016" => "Boxing",
                "200014" => "God Of Fortune",
                "200020" => "Roman Empire",
                "200019" => "Casino Royale",
                "200024" => "Queen Bee",
                "200031" => "Tokyo Hunter",
                "200032" => "Fortune Koi",
                "200034" => "Fortune Tree",
                "200035" => "Seven Brothers",
                "200033" => "Golden Wheel",
                "200045" => "Panda",
                "200044" => "Klassik",
                "200046" => "Four Guardians",
                "200047" => "Phoenix",
                "200055" => "Fu Lu Shou",
                "200059" => "Flora Secret",
                "200058" => "Nutcracker",
                "200056" => "Red Chamber",
                "200050" => "Candylicious",
                "200053" => "Fortune Dice",
                "200048" => "Soccer",
                "200043" => "Three Kingdoms",
                "200049" => "Four Beauties",
                "200065" => "Sweet Treats",
                "200051" => "Cleopatra",
                "200063" => "Lucky Bomber",
                "200067" => "Lucky Royale",
                "200069" => "Sky Strikers",
                "200068" => "Monsters Cash",
                "200071" => "Lion Dance",
                "200070" => "Trick Or Treat",
                "200073" => "Winter Wonderland",
                "200064" => "Wilds and the Beanstalk",
                "200075" => "Zodiac",
                "200036" => "Lady Fortune",
                "200072" => "Blossom Garden",
                "200074" => "Hula Girl",
                "200076" => "Wuxia Princess Mega Reels",
                "200077" => "Phantom Thief",
                "200080" => "Genies Luck",
                "200081" => "World Soccer Slot 2",
                "200079" => "Kungfu Furry",
                "200086" => "Gem Forest",
                "200083" => "Frost Dragon",
                "200084" => "Jewel Land",
                "200085" => "Money Monkey",
                "200088" => "Moon Rabbit",
                "200087" => "Alchemists Spell",
                "200089" => "Chess Royale",
                "200090" => "Vikings Mega Reels",
                "200092" => "Space Neon",
                "200091" => "Jazz It Up",
                "200094" => "Fountain of Fortune",
                "200095" => "Fa Fa Zhu",
                "200097" => "Magic Paper",
                "2000100" => "Panda Warrior",
                "200096" => "Three Beauties",
                "2000102" => "Strip' n Roll",
                "2000103" => "Captain Rabbit",
                "2000107" => "Fortune Hong Bao",
                "200098" => "Nuwa and the Five Elements",
                "2000105" => "Xuan Wu Blessing",
                "2000104" => "Cosmic Boost",
                "2000109" => "Wind Chimes",
                "2000108" => "Lunar Legends",
                "2000110" => "Nation - Double Play",
                "2000111" => "Leap of Fortune",
                "2000119" => "Golden Ink Rat",
                "2000120" => "Cash and Kisses",
                "2000101" => "Dino Age",
                "2000112" => "Lucky Tarot",
                "2000113" => "Chilli Surprise",
                "2000118" => "Fortune Dragon",
                "2000121" => "Legend of Thanh Giong",
                "2000122" => "Songkran: Summer Splash",
                "2000123" => "Bai Ye Xing Fu",
                "2000124" => "Virus Invasion",
                "2000117" => "God of Flames",
                "2000126" => "Angry Bees",
                "2000127" => "Lord Bao Bao",
                "2000125" => "Double Greatness",
                "2000128" => "Forest Hunter",
                "2000131" => "Elf Surprise Jackpot",
                "2000132" => "Mystical Bali",
                "2000116" => "UFO Frenzy",
                "2000129" => "Sacred Clash",
                "2000130" => "Steampunk Bandits",
                "2000133" => "Mine of Riches",
                "2000134" => "Alpha and the Red Hood",
                "2000137" => "Sweet Baker",
                _ => string.Empty,
            };
        }

        private static DeviceType GetDeviceType(string device)
        {
            return device switch
            {
                "1" or "2" or "3" => DeviceType.Desktop,
                "4" => DeviceType.Mobile,
                "5" => DeviceType.Android,
                "6" => DeviceType.iOS,
                _ => DeviceType.None,
            };
        }
    }
}
