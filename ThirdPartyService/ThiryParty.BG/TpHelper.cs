using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.BLL;
using ThirdPartyCore.Extensions;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.BG;

public class TpHelper : ThirdPartyHelperBase
{
    private readonly Dictionary<int, BGConfig> _siteConfig;
    public TpHelper(IServiceProvider provider)
    {
        _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<BGConfig>(TpEnum.BG.ToString());
    }

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

    private class JsonRpcResponse<T>
    {
        public string? id { get; set; }
        public T? result { get; set; }
        public JsonRpcErrorResponse? error { get; set; }
        public string? jsonrpc { get; set; }
        public object? request { get; set; }
    }

    private class JsonRpcErrorResponse
    {
        public string? code { get; set; }
        public string? sn { get; set; }
        public string? message { get; set; }
        public string? reason { get; set; }
        public string? action { get; set; }
    }
    public class BGConfig : TpSiteConfig
    {
        public Dictionary<string, CurrencyDataBG> CurrencyData { get; set; }
    }
    public class CurrencyDataBG
    {
        public string Sn { get; set; }
        public string AgentId { get; set; }
        public string AgentLoginId { get; set; }
        public string ApiSecretKey { get; set; }
        public string AgentPassword { get; set; }
    }
    #endregion

    public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        string method = "open.user.create";
        string currency = tpCustInfo.TpCurrencyId;
        var config = _siteConfig[tpCustInfo.SiteId];
        string ApiUrl = config.ApiUrl;
        string sn = config.CurrencyData[currency].Sn;
        string agnetLoginId = config.CurrencyData[currency].AgentLoginId;
        string agnetPassword = config.CurrencyData[currency].AgentPassword;

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

        var jsonResponse = JsonConvert.DeserializeObject<JsonRpcResponse<CreateMemberResult>>(response);

        if (jsonResponse == null)
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = "1",
                RawMessage = "No Result.Response is null."
            };

        if (jsonResponse.error != null)
            return new ApiResult()
            {
                ErrorCode = ErrorCode.Fail,
                RawErrorCode = jsonResponse.error.code ?? "",
                RawMessage = $"{jsonResponse.error.message ?? ""}" + " | " + $"{jsonResponse.error.reason ?? ""}"
            };

        if (jsonResponse.result != null
            && jsonResponse.result.nickname == tpCustInfo.TpUserName)
        {
            tpCustInfo.TpCustId = jsonResponse.result.userId.ToString();
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
        bool isGetUrlSuccess = false;
        string url = string.Empty;
        string method = "open.video.game.url";
        string currency = tpCustInfo.TpCurrencyId;
        var config = _siteConfig[tpCustInfo.SiteId];
        string apiUrl = config.ApiUrl;
        string sn = config.CurrencyData[currency].Sn;
        string agnetPassword = config.CurrencyData[currency].AgentPassword;
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

        while (!isGetUrlSuccess)
        {
            var response = HttpPostClient(apiUrl, parameters, "application/json");

            if (!IsJSON(response))
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "1",
                    RawMessage = "No Result. Response is not json."
                };

            var jsonResponse = JsonConvert.DeserializeObject<JsonRpcResponse<string>>(response);

            if (jsonResponse == null)
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = "-1",
                    RawMessage = "No Result. Response is null."
                };

            if (jsonResponse.error != null)
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    RawErrorCode = jsonResponse.error.code ?? "",
                    RawMessage = $"{jsonResponse.error.message ?? ""}" + " | " + $"{jsonResponse.error.reason ?? ""}"
                };
            if (!string.IsNullOrWhiteSpace(jsonResponse.result))
            {
                url = jsonResponse.result;
                isGetUrlSuccess = true;
            }

        }
        return new ApiResult()
        {
            ErrorCode = ErrorCode.Success,
            Message = url
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
        // 测试环境仅是提供测试调用，仅有正式环境才可正常查看注单详情内容
        string method = "open.sn.video.order.detail.url.get";
        string currency = tpCustInfo.TpCurrencyId;
        var config = _siteConfig[tpCustInfo.SiteId];
        string apiUrl = config.ApiUrl;
        string sn = config.CurrencyData[currency].Sn;
        string apiSecretKey = config.CurrencyData[currency].ApiSecretKey;

        var id = Guid.NewGuid();

        var parameters = ToJson(new
        {
            id = id,
            method = method,
            @params = new
            {
                random = id,
                sign = ToMD5(id + sn + getBetDetail.TransId + apiSecretKey),
                sn = sn,
                orderId = getBetDetail.TransId,
                locale = "en"
            },
            jsonrpc = "2.0"
        });

        var response = HttpPostClient(apiUrl, parameters, "application/json");

        if (!IsJSON(response))
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse is not json.\r\n Response={response}");

        var jsonResponse = JsonConvert.DeserializeObject<JsonRpcResponse<string>>(response);

        if (jsonResponse == null)
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse to json is null.\r\n Response={response}");

        if (jsonResponse.error != null)
            throw new BetDetailNotFoundException($"{getBetDetail.TransId} BG Ressponse return error.\r\n Ressponse={JsonConvert.SerializeObject(jsonResponse.error)}.");

        return new BaseDetail(getBetDetail.TpGameTypeOption)
        {
            TransId = getBetDetail.TransId,
            DetailUrl = jsonResponse.result,
            TpCurrency = new TpCurrency()
            {
                ExchangeRate = tpCustInfo.ExchangeRate,
                TpCurrencyId = tpCustInfo.TpCurrencyId,
            },
        };
    }

    public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
    {
        var detail = TpBetRecordProvider.DbQueryFirst("Service_TpWagerLC_BG_Detail_Get", new { betRecordInfo.TransId }, DataBase.Wager);

        return tpGameType.CategoryId switch
        {
            TpCategory.LiveCasino => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
            {
                GameID = detail.GetStringValue("gameId"),
                Game = detail.GetStringValue("gameNameEn"),
                RefNo = detail.GetStringValue("orderId"),
                Title = tpGameType.TypeName,
                MatchID = $"{detail.GetStringValue("tableId")}_{detail.GetStringValue("issueId")}",
                BetChoice = $"{detail.GetStringValue("playNameEn")} {detail.GetStringValue("betContent")}",
                IP = detail.GetStringValue("fromIp")
            },
            _ => new TpBetRecordExtend(betRecordInfo, tpGameType, tpCustInfo)
        };
    }
}