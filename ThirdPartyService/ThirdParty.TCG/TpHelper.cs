using System.Security.Cryptography;
using System.Text;
using System.Web;

using Microsoft.Extensions.DependencyInjection;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.TCG
{
    public class TpHelper : ThirdPartyHelperBase, IThirdPartyParticular
    {
        private const string ContentType = "application/x-www-form-urlencoded";
        private readonly Dictionary<int, Dictionary<string, string>> siteConfigDic;

        public TpHelper(IServiceProvider provider)
        {
            siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.TCG.ToString());
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            var config = new TCGConfig(siteInputs, tpCustInfo.TpCurrencyId);
            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
            var sParam = new
            {
                method = "cm",
                username = tpCustInfo.TpUserName,
                password = tpCustInfo.TpPwd,
                currency = tpCustInfo.TpCurrencyId
            };

            var json = HttpPost(config.ApiUrl, GetBody(sParam, config), ContentType);
            var result = FromJson<TCGResult>(json);

            if (result.status != "0")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = result.error_desc,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            return new ApiResult<UserBalance>()
            {
                ErrorCode = ErrorCode.Fail,
            };
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.UserNotLogin,
                    Message = "Not support fun game.",
                };
            }

            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            var config = new TCGConfig(siteInputs, tpCustInfo.TpCurrencyId);
            var sParam = new LaunchGameObj(tpCustInfo, appType, TpLangId, inputs, config);

            var json = HttpPost(config.ApiUrl, GetBody(sParam, config), ContentType);
            var result = FromJson<TCGResult>(json);

            if (result.status != "0")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = result.error_desc,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{config.GameUrl}{result.game_url}",
            };
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var siteInputs = siteConfigDic[tpCustInfo.SiteId];
            var config = new TCGConfig(siteInputs, tpCustInfo.TpCurrencyId);
            var sParam = new
            {
                method = "kom",
                username = tpCustInfo.TpUserName,
            };

            var json = HttpPost(config.ApiUrl, GetBody(sParam, config), ContentType);
            var result = FromJson<TCGResult>(json);

            if (result.status != "0")
            {
                return new ApiResult<UserBalance>()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = result.status,
                    RawMessage = result.error_desc,
                };
            }

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
            };
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var tpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            };
            var gameType = (GameType)getBetDetail.TpGameTypeOption.Type;
            var wagerDetail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_TCG_Detail_Get", new { getBetDetail.TransId, GameType = (int)gameType }, DataBase.Wager);
            var detail = ToJson(wagerDetail);
            return getBetDetail.TpGameTypeOption.CategoryId switch
            {
                TpCategory.Lottery => new LotteryDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = detail,
                    TpCurrency = tpCurrency,
                    RefTransId = wagerDetail.GetValueOrDefault("orderNum")?.ToString(),
                    TpUserName = wagerDetail.GetValueOrDefault("username")?.ToString(),
                    UTCBetTime = gameType == GameType.SEALottery
                    ? ToUTC(wagerDetail.GetValueOrDefault("betTime"))
                    : ToUTC(wagerDetail.GetValueOrDefault("bettingTime")),
                    UTCSettledTime = gameType == GameType.SEALottery
                    ? ToUTC(wagerDetail.GetValueOrDefault("settlementTime"))
                    : ToUTC(wagerDetail.GetValueOrDefault("drawTime")),
                    Stake = gameType == GameType.SEALottery
                    ? (decimal)wagerDetail.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate
                    : (decimal)wagerDetail.GetValueOrDefault("actualBetAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = gameType == GameType.SEALottery
                    ? (decimal)wagerDetail.GetValueOrDefault("netPNL") * tpCurrency.ExchangeRate
                    : ((decimal)wagerDetail.GetValueOrDefault("winAmount") - (decimal)wagerDetail.GetValueOrDefault("actualBetAmount")) * tpCurrency.ExchangeRate,
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
            var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWager_TCG_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type }, DataBase.Wager);
            if (detail == null)
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameCode"),
                Game = detail.GetStringValue("remark"),
                RefNo = detail.GetStringValue("orderNum"),
                Title = tpGameType.TypeName,
                MatchID = detail.GetStringValue("numero"),
                BetType = detail.GetStringValue("playName"),
                BetChoice = detail.GetStringValue("bettingContent"),
                Result = detail.GetStringValue("winningNumber"),
                DeviceType = DeviceType.None,
                IP = detail.GetStringValue("clientIp"),
            };
        }

        public class TCGConfig
        {
            public string GameUrl { get; set; }
            public string HomeUrl { get; set; }
            public string ApiUrl { get; set; }
            public string MerchantCode { get; set; }
            public string DesKey { get; set; }
            public string SignKey { get; set; }
            public TCGConfig(Dictionary<string, string> inputs, string tpCurrencyId)
            {
                GameUrl = inputs.GetValue($"GameUrl_{tpCurrencyId}");
                HomeUrl = inputs.GetValue($"HomeUrl");
                ApiUrl = inputs.GetValue("ApiUrl");
                MerchantCode = inputs.GetValue($"MerchantCode_{tpCurrencyId}");
                DesKey = inputs.GetValue($"DESKey_{tpCurrencyId}");
                SignKey = inputs.GetValue($"SHA256Key_{tpCurrencyId}");
            }
        }

        public class TCGResult
        {
            public string status { get; set; }
            public string error_desc { get; set; }
            public string game_url { get; set; }
        }

        public class LaunchGameObj
        {
            private readonly Dictionary<GameType, Action<AppType, string>> _launchGameHandler;
            public string method { get; set; }
            public string username { get; set; }
            public string language { get; set; }
            public int product_type { get; set; }
            public string game_code { get; set; }
            public string game_mode { get; set; }
            public string platform { get; set; }
            public string back_url { get; set; }
            public string lottery_bet_mode { get; set; }
            public string view { get; set; }
            public List<Series> series { get; set; }

            public LaunchGameObj(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs, TCGConfig config)
            {
                _launchGameHandler = new Dictionary<GameType, Action<AppType, string>>()
                {
                    { GameType.SEALottery, SettingSEALottery },
                    { GameType.VNLottery, SettingVNLottery }
                };
                method = "lg";
                username = tpCustInfo.TpUserName;
                language = TpLangId;
                game_mode = "1";
                back_url = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : config.HomeUrl;
                view = "lobby";

                var gameType = GetGameType(tpCustInfo.TpCurrencyId);
                var gameId = inputs.TryGetValue("gameId", out string output) ? output : "";
                _launchGameHandler[gameType].Invoke(appType, gameId);
            }

            private void SettingSEALottery(AppType appType, string gameId)
            {
                product_type = 384;
                platform = "flash";
                game_code = "HNVNC";
                lottery_bet_mode = "SEA_Tradition";
                series = new List<Series>()
                {
                    new Series{ game_group_code = "VNC", prize_mode_id = "1" },
                };
            }
            private void SettingVNLottery(AppType appType, string gameId)
            {
                product_type = 420;
                platform = appType == AppType.Mobile ? "MOBILE" : "WEB";
                game_code = "lobby";
                lottery_bet_mode = "Lott";
                series = new List<Series>()
                {
                    new Series{ game_group_code = "VIETNAM_LOTTO", default_series="97.5" },
                };
            }

            private GameType GetGameType(string tpCurrencyId)
            {
                return tpCurrencyId switch
                {
                    "VNDK" => GameType.VNLottery,
                    _ => GameType.SEALottery,
                };
            }
        }

        public class Series
        {
            public string game_group_code { get; set; }
            public string prize_mode_id { get; set; }
            public string max_series { get; set; }
            public string min_series { get; set; }
            public string max_bet_series { get; set; }
            public string default_series { get; set; }
        }


        public enum GameType
        {
            SEALottery = 54,//東南亞彩票
            VNLottery = 55,//越南彩票
        }

        private string GetBody(object obj, TCGConfig config)
        {
            var dParam = DesEncrypt(ToJson(obj), config.DesKey);
            var sign = SHA256($"{dParam}{config.SignKey}");
            return $"merchant_code={HttpUtility.UrlEncode(config.MerchantCode)}" +
            $"&params={HttpUtility.UrlEncode(dParam)}" +
            $"&sign={HttpUtility.UrlEncode(sign)}";
        }

        /// <summary>
        /// DES加密
        /// </summary>
        public string DesEncrypt(string input, string key)
        {
            var toEncryptArray = Encoding.UTF8.GetBytes(input);
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using var cTransform = des.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        /// <summary>
        /// DES解密
        /// </summary>
        public string DesDecrypt(string pToDecrypt, string key)
        {
            var dataByteArray = Convert.FromBase64String(pToDecrypt);
            var des = new DESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            using MemoryStream ms = new MemoryStream();
            using CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(dataByteArray, 0, dataByteArray.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }
        public string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }

        private DateTime ToUTC(object time)
        {
            if (time is null)
            {
                return default;
            }
            var timeString = time.ToString();
            if (string.IsNullOrEmpty(timeString) || timeString.Equals("0001-01-01 00:00:00"))
            {
                return default;
            }

            return new DateTimeOffset(DateTime.Parse(timeString), new TimeSpan(8, 0, 0)).UtcDateTime;
        }
    }
}
