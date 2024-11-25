using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.CQ9
{
    public class TpHelper : ThirdPartyHelperBase
    {
        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private readonly Dictionary<int, Dictionary<string, string>> configDic;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList((int)TpEnum.CQ9).ToDictionary(d => d.AppId, d => d.Name);
            configDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(TpEnum.CQ9.ToString());
        }

        #region Request
        public class LoginReq
        {
            public string account { get; set; }
            public string gamehall { get; set; }
            public string gamecode { get; set; }
            public string gameplat { get; set; }
            public string lang { get; set; }
            public string session { get; set; }
        }
        #endregion

        #region Response
        public class LoginResult
        {
            public Data data { get; set; }
            public Status status { get; set; }
        }
        public class Data
        {
            public string token { get; set; }
            public string url { get; set; }
        }
        public class Status
        {
            public string code { get; set; }
            public string message { get; set; }
            public string datetime { get; set; }
            public string traceCode { get; set; }
        }
        #endregion

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
            var config = configDic[tpCustInfo.SiteId];
            var testHierarchyCustId = JsonConvert.DeserializeObject<List<long>>(config["TestHierarchyCustId"]);

            string ApiUrl = config.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(config["ApiUrl"]) ? config["ApiUrl"] : throw new Exception("ApiUrl");
            string gameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : throw new Exception("gameId");
            string fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            ApiResult oApiResult = new ApiResult();

            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                string FunGameUrl = config.ContainsKey("FunGameUrl") && !string.IsNullOrEmpty(config["FunGameUrl"]) ? config["FunGameUrl"] : throw new Exception("FunGameUrl");

                if (!char.IsDigit(gameId[0]))
                {
                    throw new Exception("the free game not support");
                }
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = $"{FunGameUrl}/{gameId}?language={TpLangId}&token=guest";
                return oApiResult;
            }

            var paramToken = $"API_Token_{tpCustInfo.TpCurrencyId}";
            string API_Token = config.ContainsKey($"{paramToken}") && !string.IsNullOrEmpty(config[$"{paramToken}"]) ? config[$"{paramToken}"] : throw new Exception($"{paramToken}");

            if (testHierarchyCustId.Contains(tpCustInfo.CustId))
            {
                API_Token = config.ContainsKey("API_Token_TEST") && !string.IsNullOrEmpty(config["API_Token_TEST"]) ? config["API_Token_TEST"] : throw new Exception("API_Token_TEST");
            }

            // Header
            Dictionary<string, string> headerReq = new Dictionary<string, string>();
            headerReq.Add("Authorization", API_Token);
            // Token
            inputs.TryGetValue("Token", out string token);

            var mobileParam = "web";
            if (appType != AppType.Desktop)
            {
                mobileParam = "mobile";
            }

            // Login params
            LoginReq loginReq = new LoginReq
            {
                account = tpCustInfo.TpUserName,
                gamehall = "cq9",
                gamecode = gameId,
                gameplat = mobileParam,
                lang = TpLangId,
                session = HttpUtility.UrlEncode(token)
            };

            // Call
            var dicP = FromJson<Dictionary<string, string>>(ToJson(loginReq));
            var sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());
            var result = HttpPost(
                $"{ApiUrl}/gameboy/player/sw/gamelink",
                sParam,
                "application/x-www-form-urlencoded",
                headerReq
            );

            // Response
            var tpData = JsonConvert.DeserializeObject<LoginResult>(result);
            if (tpData.status.code == "0")
            {
                oApiResult.ErrorCode = ErrorCode.Success;
                oApiResult.Message = tpData.data.url;
            }
            else
            {
                oApiResult.RawMessage = tpData.status.message;
                oApiResult.RawErrorCode = tpData.status.code;
                oApiResult.Message = $"{tpData.status.code}-{tpData.status.message}";
                oApiResult.ErrorCode = ErrorCode.Fail;
            }

            return oApiResult;
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var config = configDic[tpCustInfo.SiteId];
            var testHierarchyCustId = JsonConvert.DeserializeObject<List<long>>(config["TestHierarchyCustId"]);

            var apiUrl = config.GetValueOrDefault("ApiUrl") ?? throw new Exception("ApiUrl");
            var token = config.GetValueOrDefault($"API_Token_{tpCustInfo.TpCurrencyId}") ?? throw new Exception($"API_Token_{tpCustInfo.TpCurrencyId}");
            if (testHierarchyCustId.Contains(tpCustInfo.CustId))
            {
                token = config.GetValueOrDefault("API_Token_TEST") ?? throw new Exception("API_Token_TEST");
            }

            var url = $"{apiUrl}/gameboy/order/detail/v2";
            var sParam = $"roundid={getBetDetail.TransId}&account={tpCustInfo.TpUserName}&lang=en";
            var header = new Dictionary<string, string>()
            {
                { "Authorization", token }
            };

            var json = HttpGet(url, sParam, null, header);
            var result = FromJson<DetailResult>(json);
            if (result.status.code != "0")
            {
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {result.status.message}");
            }

            return new BaseDetail(getBetDetail.TpGameTypeOption)
            {
                TransId = getBetDetail.TransId,
                DetailUrl = result.data,
                TpCurrency = new TpCurrency()
                {
                    ExchangeRate = tpCustInfo.ExchangeRate,
                    TpCurrencyId = tpCustInfo.TpCurrencyId,
                },
            };
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var detailList = TpBetRecordProvider.DbQuery("Service_Bettrans_CQ9_Detail_Get", new { betRecordInfo.TransId, GameType = tpGameType.Type }, DataBase.Main);
            if (!detailList.Any())
            {
                return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
            }

            var gameCode = detailList.GetFirstStringValue("gamecode");

            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = gameCode,
                Game = GetGameName(gameCode),
                RefNo = detailList.GetFirstStringValue("roundid"),
                Title = tpGameType.TypeName,
                MatchID = detailList.GetFirstStringValue("roundid"),
                BetType = detailList.GetFirstStringValue("gametype"),
                BetChoice = string.Empty,
                Result = string.Empty,
                DeviceType = DeviceType.None,
                IP = string.Empty,
            };
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

        private class DetailResult
        {
            public string data { get; set; }
            public Status status { get; set; }
        }

    }
}
