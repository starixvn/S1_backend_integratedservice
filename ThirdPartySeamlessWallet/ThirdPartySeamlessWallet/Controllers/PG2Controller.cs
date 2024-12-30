using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers;

[ApiController]
public class PG2Controller : ControllerBase
{
    #region Fields

    private readonly Tp _thisTp;
    private readonly Log2File _log2File;
    private readonly IPG2BLL _PG2Bll;
    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly Dictionary<int, PG2Config> _siteConfigDic;

    #endregion

    #region Constructors

    public PG2Controller(IServiceProvider provider)
    {
        _thisTp = Tp.PG2;
        _log2File = provider.GetRequiredService<Log2File>();
        _PG2Bll = provider.GetService<IPG2BLL>();
        _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PG2Config>(_thisTp.ToString());
    }

    #endregion

    /// <summary>
    /// 測試API有通
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("[controller]/ping")]
    public string PingTest() => "Ping Ok.";

    /// <summary>
    /// GetBalance API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/balance")]
    public BalanceResponse_PG2 GetBalanceAsync(CommonRequest_PG2 request)
    {
        string logName = $"{_thisTp}_balance";
        Request.Headers.TryGetValue("x-amb-signature", out var header);
        string message;

        if (request == null)
        {
            message = "request is null.";
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (string.IsNullOrWhiteSpace(request.Agent))
        {
            message = "request.agent is Invalid.";
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            message = "request.username is Invalid.";
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName, logName))
        {
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidAgent,
                    message = PG2ErrorCode.InvalidAgent.GetEnumDescription()
                }
            };
        }
        var jsonParam = JsonConvert.SerializeObject(request);
        if (!CheckHeader(jsonParam, request.UserName, header, logName))
        {
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = PG2ErrorCode.InvalidRequest.GetEnumDescription()
                }
            };
        }

        if (!CheckThirdParty(request.UserName, logName))
        {
            return new BalanceResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.ServiceNotAvailable,
                    message = PG2ErrorCode.ServiceNotAvailable.GetEnumDescription()
                }
            };
        }

        return _PG2Bll.GetBalance(request);
    }

    /// <summary>
    /// Bet API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/bet")]
    public async Task<BetResponse_PG2> BetAsync(BetRequest_PG2 request)
    {
        string logName = $"{_thisTp}_bet";
        string message;

        if (request == null)
        {
            message = "request is null.";
            return new BetResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        var verify = JsonConvert.DeserializeObject<VerifyReq_PG2>(JsonConvert.SerializeObject(request));
        if (!CheckRequest(verify, out message))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName, logName))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidAgent,
                    message = PG2ErrorCode.InvalidAgent.GetEnumDescription()
                }
            };
        }

        if (!CheckThirdParty(request.UserName, logName))
        {
            return new BetResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        return await _PG2Bll.Bet(request);
    }

    /// <summary>
    /// Payout API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/payout")]
    public async Task<PayoutResponse_PG2> PayoutAsync(PayoutRequest_PG2 request)
    {
        string logName = $"{_thisTp}_payout";
        string message;

        if (request == null)
        {
            message = "request is null.";
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        var verify = JsonConvert.DeserializeObject<VerifyReq_PG2>(JsonConvert.SerializeObject(request));
        if (!CheckRequest(verify, out message))
        {
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName, logName))
        {
            return new PayoutResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidAgent,
                    message = PG2ErrorCode.InvalidAgent.GetEnumDescription()
                }
            };
        }

        return await _PG2Bll.Payout(request);
    }

    /// <summary>
    /// Cancel API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/cancel")]
    public async Task<CancelResponse_PG2> CancelAsync(CancelRequest_PG2 request)
    {
        string logName = $"{_thisTp}_cancel";
        string message;

        if (request == null)
        {
            message = "request is null.";
            return new CancelResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        var verify = JsonConvert.DeserializeObject<VerifyReq_PG2>(JsonConvert.SerializeObject(request));
        if (!CheckRequest(verify, out message))
        {
            return new CancelResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName, logName))
        {
            return new CancelResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidAgent,
                    message = PG2ErrorCode.InvalidAgent.GetEnumDescription()
                }
            };
        }

        return await _PG2Bll.Cancel(request);
    }

    /// <summary>
    /// Settle API
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    [Route("[controller]/settlement")]
    public async Task<SettleResponse_PG2> SettlementAsync(SettlementRequest_PG2 request)
    {
        string logName = $"{_thisTp}_settlement";
        Request.Headers.TryGetValue("x-amb-signature", out var header);
        string message;

        if (request == null)
        {
            message = "request is null.";
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = message
                }
            };
        }

        if (!CheckAgentId(request.Agent, request.UserName, logName))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidAgent,
                    message = PG2ErrorCode.InvalidAgent.GetEnumDescription()
                }
            };
        }
        
        var jsonParam = JsonParamForSign(request);
        var jsonParamQA = JsonParamForSignQA(request);
        if (!CheckHeader(jsonParam,request.UserName,header,logName) &&
            !CheckHeader(jsonParamQA, request.UserName, header, logName))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidRequest,
                    message = PG2ErrorCode.InvalidRequest.GetEnumDescription()
                }
            };
        }

        if (!CheckThirdParty(request.UserName, logName))
        {
            return new SettleResponse_PG2()
            {
                status = new Status_PG2()
                {
                    code = PG2ErrorCode.InvalidUsernameOrToken,
                    message = PG2ErrorCode.InvalidUsernameOrToken.GetEnumDescription()
                }
            };
        }

        return await _PG2Bll.Settlement(request);
    }

    private static string JsonParamForSign(SettlementRequest_PG2 request)
    {
        //對方UAT環境送的參數與排序
        var sign = new
        {
            agent = request.Agent,
            username = request.UserName,
            product = request.Product,
            gameName = request.GameName,
            category = request.Category,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = request.Timestamp,
            isFeatureBuy = request.IsFeatureBuy,
            type = request.Type,
            amount = request.Amount,
            betAmount = request.BetAmount,
            isEndRound = request.IsEndRound,
            parentRoundId = request.ParentRoundId,
            isOnlyPayout = request.IsOnlyPayout,
            //isFreespin = request.IsFreeSpin,
        };

        var jObject = JObject.FromObject(sign);

        // 處理 amount 和 betAmount 格式
        foreach (var property in jObject.Properties())
        {
            if ((property.Name == "amount" || property.Name == "betAmount") && property.Value.Type == JTokenType.Float)
            {
                var value = property.Value.ToObject<double>();
                if (value % 1 == 0) // 如果是整數
                {
                    property.Value = (int)value; // 顯示為整數
                }
            }
        }

        var jsonParam = jObject.ToString(Formatting.None);
        return jsonParam;
    }
    private static string JsonParamForSignQA(SettlementRequest_PG2 request)
    {
        //對方QA測試時的參數與排序
        var sign = new
        {
            username = request.UserName,
            agent = request.Agent,
            type = request.Type,
            amount = request.Amount,
            product = request.Product,
            category = request.Category,
            gameName = request.GameName,
            roundId = request.RoundId,
            refId = request.RefId,
            timestamp = request.Timestamp,
            isFeatureBuy = request.IsFeatureBuy,
            isFreespin = request.IsFreeSpin,
            betAmount = request.BetAmount,
            isEndRound = request.IsEndRound,
            parentRoundId = request.ParentRoundId,
            isOnlyPayout = request.IsOnlyPayout,
        };

        var jObject = JObject.FromObject(sign);

        // 處理 amount 和 betAmount 格式
        foreach (var property in jObject.Properties())
        {
            if ((property.Name == "amount" || property.Name == "betAmount") && property.Value.Type == JTokenType.Float)
            {
                var value = property.Value.ToObject<double>();
                if (value % 1 == 0) // 如果是整數
                {
                    property.Value = (int)value; // 顯示為整數
                }
            }
        }

        var jsonParam = jObject.ToString(Formatting.None);
        return jsonParam;
    }

    private bool CheckAgentId(string agentId, string username, string logName)
    {
        try
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)_thisTp, username);
            if (tpCustInfo == null || tpCustInfo.CustId == 0)
            {
                _log2File.Error($"CheckAgentId Fail: tpCustInfo error", logName);
                return false;
            }

            var agent = _siteConfigDic[tpCustInfo.SiteId].CurrencyData[tpCustInfo.TpCurrencyId].Agent;
            if (agent != agentId)
            {
                _log2File.Error($"CheckAgentId Fail: {agent} is not equal {agentId}", logName);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log2File.Error($"CheckAgentId Exception:{ex.Message}", logName, ex);
            return false;
        }
    }

    private static bool CheckRequest(VerifyReq_PG2 request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            message = "request.userName is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Agent))
        {
            message = "request.agent is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            message = "request.type is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Product))
        {
            message = "request.product is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Category))
        {
            message = "request.category is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.GameName))
        {
            message = "request.game is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RoundId))
        {
            message = "request.roundId is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RefId))
        {
            message = "request.refId is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Timestamp))
        {
            message = "request.timestamp is Invalid.";
            return false;
        }

        return true;
    }

    private bool CheckThirdParty(string username, string logName)
    {
        var message = string.Empty;
        try
        {
            var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)_thisTp, username);

            if (result == null)
            {
                message = "player not found.";
                _log2File.Error($"CheckThirdParty Fail: {message}", logName);
                return false;
            }

            if (result.Locked)
            {
                message = "player is locked.";
                _log2File.Error($"CheckThirdParty Fail: {message}", logName);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _log2File.Error($"CheckThirdParty Exception:{ex.Message}", logName, ex);
            return false;
        }
    }

    private bool CheckHeader(string input, string username, string header, string logName)
    {
        var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)_thisTp, username);
        if (tpCustInfo == null || tpCustInfo.CustId == 0)
        {
            _log2File.Error($"CheckHeader Fail: tpCustInfo error", logName);
            return false;
        }
        var key = _siteConfigDic[tpCustInfo.SiteId].CurrencyData[tpCustInfo.TpCurrencyId].ApiKey;

        var salt = Encoding.UTF8.GetBytes(key);
        var HASH_SIZE = 64;
        var ITERATIONS = 1000;

        // Generate the hash
        var pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS, HashAlgorithmName.SHA512);
        var data = pbkdf2.GetBytes(HASH_SIZE);
        return Convert.ToBase64String(data) == header;
    }
}
