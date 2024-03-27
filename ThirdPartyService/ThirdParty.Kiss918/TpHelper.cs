using System.Security.Cryptography;
using System.Text;
using System.Web;

using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.Kiss918
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;

        private readonly new Dictionary<string, int> _langList = new()
        {
            {"en",1 },{ "cs",2 },{ "th",3 },{ "id",5 },{ "vn",7 },
        };

        public TpHelper()
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.Kiss918).ToDictionary(d => d.AppId, d => d.Name);
        }
        private class CreateMemberResponse
        {
            public int playerID { get; set; }
            public string error { get; set; }
            public string description { get; set; }
        }

        private class LoginAccessTokenResponse
        {
            public string actk { get; set; }
            public string status { get; set; }
            public int playerID { get; set; }
            public string description { get; set; }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            string accessToken = inputs.ContainsKey("AccessToken") && !string.IsNullOrEmpty(inputs["AccessToken"]) ? inputs["AccessToken"] : throw new Exception("AccessToken");
            string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
            ApiUrl = $"{ApiUrl}/api/createplayer";

            var parameters = ToJson(new
            {
                accountID = tpCustInfo.TpUserName,
                nickname = tpCustInfo.TpUserName,
                currency = currency
            });
            var header = new Dictionary<string, string>() { { "token", $"{accessToken}" } };
            var result = HttpPostClient(ApiUrl, parameters, "application/json", header);

            ApiResult oApiResult = new ApiResult();
            if (IsJSON(result))
            {
                var json = JsonConvert.DeserializeObject<CreateMemberResponse>(result);
                if (json.error == "0")
                {
                    oApiResult.ErrorCode = ErrorCode.Success;
                    tpCustInfo.TpCustId = json.playerID.ToString(); //Tp CustID (PK)
                }
                else
                {
                    oApiResult.ErrorCode = ErrorCode.Fail;
                    oApiResult.RawMessage = json.description;
                    oApiResult.RawErrorCode = json.error;
                }
            }
            else
            {
                oApiResult.RawMessage = "No Result";
                oApiResult.RawErrorCode = "1";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            TransIdModel transIdModel = new TransIdModel();
            transIdModel.Implemented = false;
            return transIdModel;
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            string currency = tpCustInfo.TpCurrencyId;
            int siteId = tpCustInfo.SiteId;

            string accessToken = inputs.ContainsKey("AccessToken") && !string.IsNullOrEmpty(inputs["AccessToken"]) ? inputs["AccessToken"] : throw new Exception("AccessToken");
            string baseApiUrl = inputs.ContainsKey("GameApiUrl") && !string.IsNullOrEmpty(inputs["GameApiUrl"]) ? inputs["GameApiUrl"] : throw new Exception("GameApiUrl");
            string secretKey = inputs.ContainsKey($"SecretKey_{currency}") && !string.IsNullOrEmpty(inputs[$"SecretKey_{currency}"]) ? inputs[$"SecretKey_{currency}"] : throw new Exception($"SecretKey_{currency}");
            string md5EncryptKey = inputs.ContainsKey($"Md5EncryptKey_{currency}") && !string.IsNullOrEmpty(inputs[$"Md5EncryptKey_{currency}"]) ? inputs[$"Md5EncryptKey_{currency}"] : throw new Exception($"Md5EncryptKey_{currency}");
            string encryptKey = inputs.ContainsKey($"EncryptKey_{currency}") && !string.IsNullOrEmpty(inputs[$"EncryptKey_{currency}"]) ? inputs[$"EncryptKey_{currency}"] : throw new Exception($"EncryptKey_{currency}");
            string delimiter = inputs.ContainsKey("Delimiter") && !string.IsNullOrEmpty(inputs["Delimiter"]) ? inputs["Delimiter"] : throw new Exception("Delimiter");
            string siteName = inputs.ContainsKey($"SiteName_{siteId}") && !string.IsNullOrEmpty(inputs[$"SiteName_{siteId}"]) ? inputs[$"SiteName_{siteId}"] : throw new Exception($"SiteName_{siteId}");
            var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
            var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

            var loginApiUrl = $"{baseApiUrl}/api/Acc/Login";

            var currTime = DateTime.UtcNow.ToString("yyyyMMddHHmmss");

            //vendor建議用字串組，用其他方法會有偶發問題
            var QS = $"key={secretKey}{delimiter}" +
                $"time={currTime}{delimiter}" +
                $"userName={tpCustInfo.TpUserName}@{siteName}{delimiter}" +
                $"password={tpCustInfo.TpPwd}{delimiter}" +
                $"currency={currency}{delimiter}" +
                $"nickName={tpCustInfo.TpUserName}";
            var q = HttpUtility.UrlEncode(DESEncrypt(QS, encryptKey));
            var s = BuildMD5(QS + md5EncryptKey + currTime + secretKey);

            var request = ToJson(new
            {
                q = q,
                s = s,
                accessToken = accessToken
            });

            var result = HttpPostClient(loginApiUrl, request, "application/json");

            ApiResult oApiResult = new ApiResult();

            if (!IsJSON(result))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result"
                };

            var json = JsonConvert.DeserializeObject<LoginAccessTokenResponse>(result);

            if (json.status != "1") //success
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = json.status,
                    RawMessage = json.description
                };

            var lang = 1; //default en
            if (_langList.ContainsKey(TpLangId))
                lang = _langList[TpLangId];

            var joinGameRequest = new Dictionary<string, string>()
            {
                { "tkn" , json.actk },
                { "language" , lang.ToString() }
            };

            string sParam = string.Join("&", joinGameRequest.Select(kp => $"{kp.Key}={HttpUtility.UrlEncode(kp.Value)}").ToArray());

            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success,
                Message = $"{baseApiUrl}/CallGame?{sParam}"
                + $"&user={tpCustInfo.TpUserName}@{siteName}"
                + $"&gName={gameCode}"
                + $"&lobbyUrl={homeUrl}"
            };
        }

        #region Transfer Method

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>();
            oApiResult.ErrorCode = ErrorCode.Fail;
            return oApiResult;
        }

        #endregion

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var roundId = string.Empty;
            var gameId = string.Empty;
            var url = string.Empty;

            var detailList = TpBetRecordProvider.DbQuery("Service_BettransRng_Kiss918_Detail_Get",
                new { YYYYMM = getBetDetail.SettledTime.GetPartitionKey(), getBetDetail.TransId }, DataBase.Report)?.FirstOrDefault();
            roundId = getBetDetail.TransId;

            if (detailList == null)
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail is null.");

            url = detailList.GetValueOrDefault("resultUrl")?.ToString();

            if (string.IsNullOrWhiteSpace(url))
            {
                var gameName = string.IsNullOrWhiteSpace(detailList.GetValueOrDefault("gameID").ToString()) ? "Lobby Jacpot"
                    : GetGameName(detailList.GetValueOrDefault("gameID").ToString());

                var tpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                };

                var transTime = detailList.GetValueOrDefault("TransTime");
                var settleTime = detailList.GetValueOrDefault("SettleTime");

                return new CasinoDetail(getBetDetail.TpGameTypeOption)
                {
                    TransId = getBetDetail.TransId,
                    Detail = ToJson(detailList),
                    TpCurrency = tpCurrency,
                    RefTransId = string.Empty,
                    TpUserName = detailList.GetValueOrDefault("playerID").ToString(),
                    UTCBetTime = transTime != null ? Convert.ToDateTime(transTime) : DateTime.MinValue,
                    UTCSettledTime = settleTime != null ? Convert.ToDateTime(settleTime) : DateTime.MinValue,
                    Stake = (decimal)detailList.GetValueOrDefault("betAmount") * tpCurrency.ExchangeRate,
                    WinlostAmount = (decimal)detailList.GetValueOrDefault("winLossAmount") * tpCurrency.ExchangeRate,
                    GameCode = detailList.GetValueOrDefault("gameID").ToString(),
                    GameName = gameName,
                };
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = url,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detailList = TpBetRecordProvider.DbQuery("Service_TpWagerKiss918_Detail_Get", new { _referenceID = betRecordInfo.TransId }, DataBase.Wager);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detailList.GetFirstStringValue("gameID");
            return tpGameType.CategoryId switch
            {
                TpCategory.Casino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
                {
                    GameID = gameCode,
                    Game = GetGameName(gameCode),
                    RefNo = betRecordInfo.TransId,
                    Title = tpGameType.TypeName,
                    MatchID = betRecordInfo.TransId,
                    BetType = string.Empty,
                    BetChoice = string.Empty,
                    Result = string.Empty,
                    DeviceType = DeviceType.Mobile,
                    IP = string.Empty,
                },
                _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            };
        }

        #region Private Method

        private static string DESEncrypt(string inString, string key)
        {
            byte[] encryptKey = Encoding.ASCII.GetBytes(key);
            MemoryStream ms = new MemoryStream();
            ICryptoTransform createEncryptor = new DESCryptoServiceProvider().CreateEncryptor(encryptKey, encryptKey);
            CryptoStream cs = new CryptoStream(ms, createEncryptor, CryptoStreamMode.Write);
            StreamWriter sw = new StreamWriter(cs);
            sw.Write(inString);
            sw.Flush();
            cs.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        private static string BuildMD5(string inString)
        {
            byte[] hashed = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(inString));
            StringBuilder sb = new StringBuilder(hashed.Length * 2);
            for (int i = 0; i < hashed.Length; i++)
            {
                sb.Append(hashed[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

        #endregion

    }
}
