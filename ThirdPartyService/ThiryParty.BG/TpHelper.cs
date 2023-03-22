using Newtonsoft.Json;
using System.Security.AccessControl;
using System.Web;
using ThirdPartyCore;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordDetail.Response;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.BG;

public class TpHelper : ThirdPartyHelperBase
{
    private readonly Dictionary<string, string> _gameNameList = new()
        { { "4", "Baccarat" }, { "5", "Roulette" }, { "10", "Dragon Tiger" } };
    private readonly Dictionary<int, string> _PokerDic = new()
        { {  0, "NoCard" },
          {  1, "[D A]" }, {  2, "[C A]" },{  3, "[H A]" },{  4, "[S A]" },
          {  5, "[D 2]" }, {  6, "[C 2]" },{  7, "[H 2]" },{  8, "[S 2]" },
          {  9, "[D 3]" }, { 10, "[C 3]" },{ 11, "[H 3]" },{ 12, "[S 3]" },
          { 13, "[D 4]" }, { 14, "[C 4]" },{ 15, "[H 4]" },{ 16, "[S 4]" },
          { 17, "[D 5]" }, { 18, "[C 5]" },{ 19, "[H 5]" },{ 20, "[S 5]" },
          { 21, "[D 6]" }, { 22, "[C 6]" },{ 23, "[H 6]" },{ 24, "[S 6]" },
          { 25, "[D 7]" }, { 26, "[C 7]" },{ 27, "[H 7]" },{ 28, "[S 7]" },
          { 29, "[D 8]" }, { 30, "[C 8]" },{ 31, "[H 8]" },{ 32, "[S 8]" },
          { 33, "[D 9]" }, { 34, "[C 9]" },{ 35, "[H 9]" },{ 36, "[S 9]" },
          { 37, "[D10]" }, { 38, "[C10]" },{ 39, "[H10]" },{ 40, "[S10]" },
          { 41, "[D J]" }, { 42, "[C J]" },{ 43, "[H J]" },{ 44, "[S J]" },
          { 45, "[D Q]" }, { 46, "[C Q]" },{ 47, "[H Q]" },{ 48, "[S Q]" },
          { 49, "[D K]" }, { 50, "[C K]" },{ 51, "[H K]" },{ 52, "[S K]" }
        };

    #region Model

    private class CreateMemberResult
    {
        /// <summary>
        /// 注册是否成功. True:成功; false:失败;
        /// </summary>
        public bool success { get; set; } = false;
        /// <summary>
        /// 系统分配的内部ID
        /// </summary>
        public long userId { get; set; }
        /// <summary>
        /// 会员昵称
        /// </summary>
        public string? nickname { get; set; }
        /// <summary>
        /// 注册用户的类型
        /// </summary>
        public string? regType { get; set; }

    }

    private class LoginResult
    {
        /// <summary>
        /// 视讯游戏链接地址，在返回的入口地址后加桌台后可以直接进入指定的游戏桌台，示例: &table=B06
        /// </summary>
        public string result { get; set; } = string.Empty;
    }


    private class JsonRpcResponse
    {
        public string? id { get; set; }
        public object? result { get; set; }
        public object? error { get; set; }
        public string? jsonrpc { get; set; }
        public object? request { get; set; }

    }
    #endregion

    public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        string method = "open.user.create";
        string currency = tpCustInfo.TpCurrencyId;
        string ApiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
        string sn = inputs.ContainsKey($"Sn_{currency}") && !string.IsNullOrEmpty(inputs[$"Sn_{currency}"]) ? inputs[$"Sn_{currency}"] : throw new Exception($"Sn_{currency}");
        string agnetLoginId = inputs.ContainsKey($"AgentLoginId_{currency}") && !string.IsNullOrEmpty(inputs[$"AgentLoginId_{currency}"]) ? inputs[$"AgentLoginId_{currency}"] : throw new Exception($"AgentLoginId_{currency}");
        string agnetPassword = inputs.ContainsKey($"AgentPassword_{currency}") && !string.IsNullOrEmpty(inputs[$"AgentPassword_{currency}"]) ? inputs[$"AgentPassword_{currency}"] : throw new Exception($"AgentPassword_{currency}");

        var id = Guid.NewGuid();

        var parameters = ToJson(new
        {
            id = id,
            method = method,
            @params = new
            {
                random = id,
                digest = ToMD5(id + sn + SHA1(agnetPassword)),
                sn = sn,
                loginId = tpCustInfo.TpUserName,
                nickname = tpCustInfo.TpUserName,
                agentLoginId = agnetLoginId
            },
            jsonrpc = "2.0"
        });

        var response = HttpPostClient(ApiUrl, parameters, "application/json");

        if (!IsJSON(response))
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result. Response is not json."
            };

        var jsonResponse = JsonConvert.DeserializeObject<JsonRpcResponse>(response);

        if (jsonResponse == null)
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result.Response is null."
            };

        if (jsonResponse.error != null)
        {
            var errorJson = JsonConvert.SerializeObject(jsonResponse.error);
            if (errorJson != null)
            {
                var errorDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorJson);

                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = errorDic != null && errorDic.ContainsKey("code") && !string.IsNullOrEmpty(errorDic["code"]) ? errorDic["code"] : "",
                    RawMessage = $"{(errorDic != null && errorDic.ContainsKey("message") && !string.IsNullOrEmpty(errorDic["message"]) ? errorDic["message"] : "")}" + " | "
                                      + $"{(errorDic != null && errorDic.ContainsKey("reason") && !string.IsNullOrEmpty(errorDic["reason"]) ? errorDic["reason"] : "")}"
                };
            }
        }

        var result = JsonConvert.DeserializeObject<CreateMemberResult>(JsonConvert.SerializeObject(jsonResponse.result));

        if (result != null && result.nickname == tpCustInfo.TpUserName)
        {
            tpCustInfo.TpCustId = result.userId.ToString();
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Success
            };
        }
        else
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result"
            };
    }

    public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
    {
        string method = "open.video.game.url";
        string currency = tpCustInfo.TpCurrencyId;
        string apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
        string sn = inputs.ContainsKey($"Sn_{currency}") && !string.IsNullOrEmpty(inputs[$"Sn_{currency}"]) ? inputs[$"Sn_{currency}"] : throw new Exception($"Sn_{currency}");
        string agnetPassword = inputs.ContainsKey($"AgentPassword_{currency}") && !string.IsNullOrEmpty(inputs[$"AgentPassword_{currency}"]) ? inputs[$"AgentPassword_{currency}"] : throw new Exception($"AgentPassword_{currency}");
        string homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";

        var id = Guid.NewGuid();

        var parameters = ToJson(new
        {
            id = id,
            method = method,
            @params = new
            {
                random = id,
                digest = ToMD5(id + sn + tpCustInfo.TpUserName + SHA1(agnetPassword)),
                sn = sn,
                loginId = tpCustInfo.TpUserName,
                locale = TpLangId,
                isMobileUrl = appType == AppType.Mobile ? 1 : 0,
                returnUrl = HttpUtility.UrlEncode(homeUrl),
            },
            jsonrpc = "2.0"
        });

        var result = HttpPostClient(apiUrl, parameters, "application/json");

        if (!IsJSON(result))
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result. Response is not json."
            };

        var json = JsonConvert.DeserializeObject<JsonRpcResponse>(result);

        if (json == null)
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "-1",
                RawMessage = "No Result. Response is null."
            };

        if (json.error != null)
        {
            var errorJson = JsonConvert.SerializeObject(json.error);
            if (errorJson != null)
            {
                var errorDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(errorJson);

                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = errorDic != null && errorDic.ContainsKey("code") && !string.IsNullOrEmpty(errorDic["code"]) ? errorDic["code"] : "",
                    RawMessage = $"{(errorDic != null && errorDic.ContainsKey("message") && !string.IsNullOrEmpty(errorDic["message"]) ? errorDic["message"] : "")}" + " | "
                                      + $"{(errorDic != null && errorDic.ContainsKey("reason") && !string.IsNullOrEmpty(errorDic["reason"]) ? errorDic["reason"] : "")}"
                };
            }
        }

        return new ApiResult()
        {
            ErrorCode = ErrorCode.Success,
            Message = json.result?.ToString()
        };
    }

    public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        TransIdModel transIdModel = new TransIdModel();
        transIdModel.Implemented = false;
        return transIdModel;
    }

    #region Transfer Methods

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
        string method = "open.video.game.url";
        string currency = tpCustInfo.TpCurrencyId;
        string apiUrl = inputs.ContainsKey("ApiUrl") && !string.IsNullOrEmpty(inputs["ApiUrl"]) ? inputs["ApiUrl"] : throw new Exception("ApiUrl");
        string sn = inputs.ContainsKey($"Sn_{currency}") && !string.IsNullOrEmpty(inputs[$"Sn_{currency}"]) ? inputs[$"Sn_{currency}"] : throw new Exception($"Sn_{currency}");
        string apiSecretKey = inputs.ContainsKey($"ApiSecretKey_{currency}") && !string.IsNullOrEmpty(inputs[$"ApiSecretKey_{currency}"]) ? inputs[$"ApiSecretKey_{currency}"] : throw new Exception($"ApiSecretKey_{currency}");

        var id = Guid.NewGuid();

        var parameters = ToJson(new
        {
            id = id,
            method = method,
            @params = new
            {
                random = id,
                sign = ToMD5(id + sn + getBetDetail.TransId + SHA1(apiSecretKey)),
                sn = sn,
                orderId = getBetDetail.TransId,
                locale = "en"
            },
            jsonrpc = "2.0"
        });

        var result = HttpPostClient(apiUrl, parameters, "application/json");

        if (!IsJSON(result))
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse is not json.\r\n Response={result}");

        var json = JsonConvert.DeserializeObject<JsonRpcResponse>(result);

        if (json == null)
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse to json is null.\r\n Response={result}");

        if (json.error != null)
        {
            var errorJson = JsonConvert.SerializeObject(json.error);
            if (errorJson != null)
                throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse return error.\r\n Ressponse={result}.");
        }

        return new BaseDetail(getBetDetail.TpGameTypeOption)
        {
            TransId = getBetDetail.TransId,
            DetailUrl = json.result?.ToString(),
            TpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            },
        };
    }

    public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_Yeebet_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

        var gametype = detail.GetStringValue("gametype");

        return tpGameType.CategoryId switch
        {
            TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameid"),
                Game = ConvertGameName(gametype),
                RefNo = detail.GetStringValue("id"),
                Title = tpGameType.TypeName,
                MatchID = $"{detail.GetStringValue("gameno")}_{detail.GetStringValue("gameroundid")}",
                BetType = ConvertBetType(gametype, detail.GetStringValue("bettype")),
                BetChoice = detail.GetStringValue("betpoint"),
                Result = ConvertResult(gametype, detail.GetStringValue("gameresult")),
                Odds = decimal.TryParse(detail.GetStringValue("betodds"), out var odds) ? odds : 0
            },
            _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
        };
    }

    #region Private Method

    /// <summary>
    /// 參考tp文件 3.3 Game Type
    /// </summary>
    /// <param name="gameid"></param>
    /// <returns></returns>
    private string ConvertGameName(string gametype)
    {
        if (!_gameNameList.TryGetValue(gametype, out var name))
            return string.Empty;
        return name;
    }

    /// <summary>
    /// 參考tp文件 3.4 Game Play Description
    /// </summary>
    /// <param name="gameid"></param>
    /// <param name="bettype"></param>
    /// <returns></returns>
    private string ConvertBetType(string gametype, string bettype)
    {
        if (gametype != "5")
            return "Default";

        switch (bettype)
        {
            case "1": return "Bet on Number";
            case "2": return "Bet on Big Small / Odd Even / Red Black";
            case "3": return "Bet Low,Middle,High";
            case "4": return "Bet Line1,Line2,Line3";
            case "5": return "Bet Split";
            case "6": return "Bet 3 Number";
            case "7": return "Bet 4 Number";
            case "8": return "Bet 6 Number";
            default: return "Default";
        }
    }
    /// <summary>
    /// 參考tp文件 3.5 Game Result Code
    /// </summary>
    /// <param name="gameid"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    private string ConvertResult(string gametype, string result)
    {
        switch (gametype)
        {
            case "4":
                {
                    var resultList = result?.Split(";")?.Select(Int32.Parse)?.ToList();
                    return $"PLAYER = {_PokerDic[resultList[0]]}_{_PokerDic[resultList[2]]}_{_PokerDic[resultList[4]]} " +
                        $"/ BANKER = {_PokerDic[resultList[1]]}_{_PokerDic[resultList[3]]}_{_PokerDic[resultList[5]]}";
                }
            case "10":
                {
                    var resultList = result?.Split(";")?.Select(Int32.Parse)?.ToList();
                    return $"Dragon = {_PokerDic[resultList[0]]} / Tiger = {_PokerDic[resultList[1]]}";
                }
            case "5":
            default: return result;
        }
    }

    #endregion

}