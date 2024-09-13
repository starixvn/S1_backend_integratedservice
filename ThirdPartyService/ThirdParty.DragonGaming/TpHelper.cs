using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Text;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.DragonGaming;

public class TpHelper : ThirdPartyHelperBase
{
    private const string ContentType = "application/json";
    private const TpEnum Tp = TpEnum.DragonGaming;
    private readonly Dictionary<int, DragonGamingConfig> _siteConfigDic;

    public TpHelper(IServiceProvider provider)
    {
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<DragonGamingConfig>(Tp.ToString());
    }

    public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        ApiResult oApiResult = new ApiResult();
        oApiResult.ErrorCode = ErrorCode.Success;
        return oApiResult;
    }

    public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
    {
        var config = _siteConfigDic[tpCustInfo.SiteId];
        var apiKey = config.ApiKey;
        var apiUrl = config.ApiUrl;
        var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";
        var gameCode = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"]) ? inputs["gameId"] : "";
        var homeUrl = inputs.ContainsKey("homeUrl") && !string.IsNullOrEmpty(inputs["homeUrl"]) ? inputs["homeUrl"] : "";
        var gameType = inputs.ContainsKey("gameType") && !string.IsNullOrEmpty(inputs["gameType"]) ? inputs["gameType"] : "";

        var isFun = IsFun(fun);

        var context = new GameLaunchContext
        {
            id = isFun ? "0" : tpCustInfo.TpUserName,
            username = isFun ? "fun_player" : tpCustInfo.TpUserName,
            currency = tpCustInfo.TpCurrencyId
        };

        var tpGameType = GetGameType(gameType);
        var loginRequest = new GameLaunchRequest
        {
            api_key = apiKey,
            session_id = isFun ? "0000" : GetSession(tpCustInfo, tpGameType),
            privoder = Enum.GetName(typeof(TpEnum), Tp)?.ToLower() ?? string.Empty,
            game_type = tpGameType,
            game_id = gameCode,
            platform = IsDesktop(appType),
            language = TpLangId,
            amount_type = isFun ? "fun" : "real",
            lobby_url = homeUrl,
            deposit_url = "",
            context = context
        };

        var result = HttpPost($"{apiUrl}/games/game-launch/", ToJson(loginRequest), ContentType);

        if (!IsJSON(result))
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result"
            };

        var json = JsonConvert.DeserializeObject<GameLaunchResponse>(result);

        if (json?.error_detail is not null)
        {
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "-1",
                RawMessage = JsonConvert.SerializeObject(json.error_detail),
            };
        }

        if (json is null || json?.result is null || string.IsNullOrWhiteSpace(json?.result?.launch_url))
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "-1",
                RawMessage = json == null ? "No Result" : "No LaunchUrl",
            };

        return new ApiResult()
        {
            ErrorCode = ErrorCode.Success,
            Message = json.result.launch_url
        };
    }

    public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        var config = _siteConfigDic[tpCustInfo.SiteId];
        var apiKey = config.ApiKey;
        var apiUrl = config.ApiUrl;

        var detail = TpBetRecordProvider.DbQuery("Service_BettransRng_DragonGaming_BetDetail_Get", new { _TransId = getBetDetail.TransId }, DataBase.Report);

        var tpCurrency = new TpCurrency()
        {
            ExchangeRate = tpCustInfo.ExchangeRate,
            TpCurrencyId = tpCustInfo.TpCurrencyId,
        };

        var gameId = detail.FirstOrDefault()?.GetValueOrDefault("game_id")?.ToString();
        var playerId = detail.FirstOrDefault()?.GetValueOrDefault("account_id")?.ToString();
        var roundId = detail.FirstOrDefault()?.GetValueOrDefault("round_id")?.ToString();
        var url = $"{apiUrl}/games/game-round-history/";
        var loginRequest = new GameRoundHistoryRequest
        {
            api_key = apiKey,
            game_id = gameId,
            player_id = playerId,
            round_id = roundId,
            language = "en",
        };

        var result = HttpPost(url, ToJson(loginRequest), ContentType);

        if (!IsJSON(result))
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} get bet detail fail. {JsonConvert.SerializeObject(result)}");

        var json = JsonConvert.DeserializeObject<GameRoundHistoryResponse>(result);

        if (json?.error_detail is not null)
        {
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} get bet detail error. {JsonConvert.SerializeObject(json.error_detail)}");
        }

        if (json is null || json.result is null || string.IsNullOrWhiteSpace(json.result.round_info))
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} bet detail not found. {JsonConvert.SerializeObject(result)}");

        return new BaseDetail(getBetDetail.TpGameTypeOption)
        {
            TransId = getBetDetail.TransId,
            DetailUrl = json.result.round_info,
            TpCurrency = tpCurrency
        };
    }

    public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        var detail = TpBetRecordProvider.DbQueryFirst("Service_BettransRng_DragonGaming_Detail_Get", new { betRecordInfo.TransId }, DataBase.Main);
        if (detail == null)
        {
            return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo);
        }

        return new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
        {
            GameID = detail.GetStringValue("game_id"),
            Game = tpGameType.TypeName,
            RefNo = detail.GetStringValue("round_id"),
            Title = tpGameType.TypeName,
            MatchID = betRecordInfo.TransId,
            BetType = string.Empty,
            BetChoice = string.Empty,
            Result = string.Empty,
            DeviceType = DeviceType.None,
            IP = string.Empty
        };
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
        ApiResult<UserBalance> oApiResult = new ApiResult<UserBalance>
        {
            ErrorCode = ErrorCode.Fail
        };
        return oApiResult;
    }

    #endregion

    #region private


    private bool IsFun(string fun) => fun.Equals("1", StringComparison.OrdinalIgnoreCase) ? true : false;

    //apptype與三方apptype mapping
    private string IsDesktop(AppType appType)
    {
        switch (appType)
        {
            case AppType.Desktop:
                return "desktop";
            case AppType.Mobile:
            default:
                return "mobile";
        };
    }

    //gametype與三方gametype mapping
    private string GetGameType(string gameType)
    {
        switch (gameType)
        {
            case "slots":
            default:
                return "slots";
        }
    }

    private string GetSession(TpCustInfo tpCustInfo, string tpGameType)
    {
        var token = ApplyToken(tpCustInfo);
        var sessionKeyArr = new[] { token, tpCustInfo.TpUserName, tpGameType };
        var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
        var sessionKey = Convert.ToBase64String(bytes);

        return sessionKey;
    }


    #endregion
}
