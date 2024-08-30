using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using static ThirdPartySeamlessWallet.Service.BLL.ThirdParty.AMBBLL;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class PG2BLL : IPG2BLL
{
    #region Fields
    public GameType GameType { get; set; }
    private readonly Tp thisTp;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly AuthBLL _authBll;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly ILogger<PG2BLL> _logger;
    private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;

    #endregion

    #region Constructors

    public PG2BLL(IServiceProvider provider)
    {
        thisTp = Tp.PG2;
        GameType = GameType.PG2_RNG;
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _authBll = provider.GetRequiredService<AuthBLL>();
        _bettingService = provider.GetService<BettingService>();
        _logger = provider.GetRequiredService<ILogger<PG2BLL>>();
        _settledService = provider.GetRequiredService<SettledService>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<Dictionary<string, string>>(thisTp.ToString());
    }

    #endregion

    public async Task<Response_PG2> GetBalance(CommonRequest_PG2 request)
    {
        var message = string.Empty;

        if (!CheckRequestForSlotGetBalance(request, out message))
        {
            return new Response_PG2((int)PG2ErrorCode.InvalidRequest, message);
        }

        var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, request.UserName, (int)GameType);
        if (tpCustBalance == null)
        {
            _logger.LogError($"GetBalance Fail: balance not found.");
            return new Response_PG2((int)PG2ErrorCode.ServiceNotAvailable, $"GetBalance Fail: balance not found.");
        }


        if (!CheckAgentId(request.Agent, tpCustBalance.TpCurrencyId))
        {
            return new Response_PG2((int)PG2ErrorCode.InvalidAgent, "Invalid agent id");
        }

        if (!CheckThirdPartyForSlotGetBalance(request.UserName, out message))
        {
            return new Response_PG2((int)PG2ErrorCode.ServiceNotAvailable, message);
        }

        return new Response_PG2((int)PG2ErrorCode.Success, message,tpCustBalance.Balance);
    }

    public Task<Response_PG2> Bet(BetRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    public Task<Response_PG2> Payout(PayoutRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    public Task<Response_PG2> Settlement(SettlementRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    public Task<Response_PG2> Cancel(CancelRequest_PG2 request)
    {
        throw new NotImplementedException();
    }

    #region private
    private bool CheckRequestForSlotGetBalance(CommonRequest_PG2 request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.Agent))
        {
            message = "request.agent is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserName))
        {
            message = "request.username is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }

    private bool CheckAgentId(string agentId, string currencyId)
    {
        try
        {
            //todo site = 7 寫死問題
            var Agent = _siteConfigDic[7]["agent"];

            if (string.IsNullOrWhiteSpace(Agent))
            {
                _logger.LogError("CheckAgentId Fail: SiteConfig.agent is null.");
                return false;
            }

            if (Agent != agentId)
            {
                _logger.LogError($"CheckAgentId Fail: {Agent} is not equal {agentId}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"CheckAgentId Exception:{ex.Message}");
            return false;
        }
    }
    private bool CheckThirdPartyForSlotGetBalance(string username, out string message)
    {
        message = string.Empty;
        try
        {
            var result = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, username);

            if (result == null)
            {
                message = "player not found.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            if (result.Locked)
            {
                message = "player is locked.";
                _logger.LogError($"CheckTpCustInfo Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckTpCustInfo Exception:{ex.ToString()}");
            return false;
        }
    }
    #endregion
    
}