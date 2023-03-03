using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class BGBLL : IBGBLL
{
    public Tp ThirdParty { get; set; }
    public GameType GameType { get; set; }
    public TpCategory TpCategory { get; set; }

    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly ILogger<BGBLL> _logger;

    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;

    private readonly MySqlClient _mainDbClient;

    public BGBLL(IServiceProvider provider)
    {
        ThirdParty = Tp.BG;
        GameType = GameType.BG_LiveCasino;
        TpCategory = TpCategory.LiveCasino;

        _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _logger = provider.GetService<ILogger<BGBLL>>();

        _bettingService = provider.GetService<BettingService>();
        _settledService = provider.GetRequiredService<SettledService>();

        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public async Task<ResponseModel_BG> Balance(SeamlessWalletRequest request, BalanceParameters_BG parameters)
    {
        var message = string.Empty;

        if (string.IsNullOrWhiteSpace(parameters.loginId))
        {
            message = "loginId is invaild.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, parameters.sn);
        }

        if (!GetTpInfoByUserName(parameters.loginId, out TpCustInfo tpCustInfo, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (!GetBalance(tpCustInfo.TpUserName, out decimal balance, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        return new ResponseModel_BG(request, balance);
    }

    #region Private Method

    private bool GetTpInfoByUserName(string username, out TpCustInfo tpCustInfo, out string message)
    {
        message = string.Empty;
        tpCustInfo = null;
        try
        {
            tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
            if (tpCustInfo == null)
            {
                message = "TpUserInfo is null.";
                _logger.LogError($"GetTpInfoByUserName Fail:{message}");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tpCustInfo.TpUserName))
            {
                message = "TpUserName is null.";
                _logger.LogError($"GetTpInfoByUserName Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetTpInfoByUserName Exception:{ex.Message}");
            return false;
        }
    }
    private bool GetBalance(string username, out decimal balance, out string message)
    {
        message = string.Empty;
        balance = 0;
        try
        {
            var result = _tpCustInfoBLL.GetBalance((int)ThirdParty, username, (int)GameType);

            if (result == null)
            {
                message = "balance not found.";
                _logger.LogError($"GetBalance Fail:{message}");
                return false;
            }
            balance = result.Balance;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetBalance Exception:{ex.Message}");
            return false;
        }
    }

    #endregion
}
