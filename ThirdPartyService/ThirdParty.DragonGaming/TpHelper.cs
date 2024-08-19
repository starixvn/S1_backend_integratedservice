using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Security.AccessControl;
using System.Text;
using System.Text.Json.Nodes;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.DragonGaming;

public class TpHelper : ThirdPartyHelperBase
{
    private const string ContentType = "application/json";
    private const TpEnum Tp = TpEnum.DragonGaming;
    private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
    //private readonly IReadOnlyDictionary<string, string> _gameNameList;

    public TpHelper(IServiceProvider provider)
    {
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(Tp.ToString());
        //_gameNameList = ITpBetRecordProvider.GetGameCasinoList((int)TpEnum.Pegasus).ToDictionary(d => d.AppId, d => d.Name);
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
        var apiKey = config["ApiKey"];
        var apiUrl = config["ApiUrl"];
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

        var loginRequest = new GameLaunchRequest
        {
            api_key = apiKey,
            session_id = isFun ? "0000" : GetSession(tpCustInfo),
            privoder = Enum.GetName(typeof(TpEnum), Tp)?.ToLower() ?? string.Empty,
            game_type = GetGameType(gameType),
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

        if (json is null || json.result is null || string.IsNullOrWhiteSpace(json.result.launch_url))
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
        throw new NotImplementedException();
    }

    public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
    }

    public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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

    private string GetSession(TpCustInfo tpCustInfo)
    {
        var token = ApplyToken(tpCustInfo);
        var sessionKeyArr = new[] { token, tpCustInfo.TpUserName };
        var bytes = Encoding.ASCII.GetBytes(ToJson(sessionKeyArr));
        var sessionKey = Convert.ToBase64String(bytes);

        return sessionKey;
    }


    #endregion
}
