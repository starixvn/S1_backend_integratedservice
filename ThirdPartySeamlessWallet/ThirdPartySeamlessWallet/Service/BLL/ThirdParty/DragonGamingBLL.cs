using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Tls;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class DragonGamingBLL : IDragonGamingBLL
{
    #region Fields

    private readonly Tp thisTp;
    private readonly TpCustInfoBLL _tpCustInfoBll;
    private readonly AuthBLL _authBll;
    private readonly ThirdPartyBLL _thirdPartyBll;
    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;
    private readonly ILogger<DragonGamingBLL> _logger;

    #endregion

    #region Constructors

    public DragonGamingBLL(IServiceProvider provider)
    {
        thisTp = Tp.DragonGaming;
        _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
        _authBll = provider.GetRequiredService<AuthBLL>();
        _bettingService = provider.GetService<BettingService>();
        _logger = provider.GetRequiredService<ILogger<DragonGamingBLL>>();
        _settledService = provider.GetRequiredService<SettledService>();
    }

    #endregion

    public async Task<SsessionResponse_DragonGaming> GetSession(SessionRequest_DragonGaming request)
    {
        try
        {
            var (token, tpUserName, tpGameType) = GetToken(request.token);

            if (!_authBll.Authenticate(token, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new SsessionResponse_DragonGaming(ErrorCodeOfDragonGaming.PlayerInvalid, request.token);
            }

            var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
            if (tpCheck == null || tpCheck.Locked)
            {
                return new SsessionResponse_DragonGaming(ErrorCodeOfDragonGaming.PlayerFrozen, request.token, tpCustInfoAuth.TpUserName);
            }

            var gameType = _thirdPartyBll.GetGameType((int)thisTp, tpGameType);
            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, (int)gameType);
            return new SsessionResponse_DragonGaming(ErrorCodeOfDragonGaming.Success, request.token, tpCustInfoAuth.TpUserName, tpCustBalance.Balance, tpCustBalance.TpCurrencyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DragonGaming GetSession Exception:{ex.Message}");
            return new SsessionResponse_DragonGaming(ErrorCodeOfDragonGaming.InternalError, request.token);
        }
    }

    public async Task<BalanceResponse_DragonGaming> GetBalance(BalanceRequest_DragonGaming request)
    {
        try
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request.account_id);
            if (tpCustInfo is null)
            {
                return new BalanceResponse_DragonGaming(ErrorCodeOfDragonGaming.PlayerInvalid, request.token);
            }

            var (token, tpUserName, tpGameType) = GetToken(request.token);
            var gameType = _thirdPartyBll.GetGameType((int)thisTp, tpGameType);
            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, tpCustInfo.TpUserName, (int)gameType);

            return new BalanceResponse_DragonGaming(ErrorCodeOfDragonGaming.Success, request.account_id, request.token, tpCustBalance.Balance, tpCustBalance.TpCurrencyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DragonGaming GetBalance Exception:{ex.Message}");
            return new BalanceResponse_DragonGaming(ErrorCodeOfDragonGaming.InternalError, request.token);
        }
    }

    public async Task<DebitCreditRefundResponse_DragonGaming> Debit(DebitRequest_DragonGaming request)
    {
        try
        {
            var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)thisTp, request.account_id);
            if (tpCheck == null)
            {
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.PlayerInvalid, request.token);
            }
            if (tpCheck.Locked)
            {
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.PlayerFrozen, request.token);
            }

            var gameType = GetGameType(request.game_type);
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request.account_id);
            int result = PlaceBet(request, tpCustInfo, gameType, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.Success, request.token, tpCustInfo.TpUserName, tpAffterAmount, tpCustInfo.TpCurrencyId, request.transaction_id);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.DuplicateSuccess, request.token, tpCustInfo.TpUserName, tpCustBalance.Balance, tpCustInfo.TpCurrencyId, request.transaction_id);
            }
            else if (tpAffterAmount < 0)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.Insufficient, request.token, tpCustInfo.TpUserName, tpCustBalance.Balance, tpCustInfo.TpCurrencyId, request.transaction_id);
            }

            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.DebitInternalError, request.token, tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, request.transaction_id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DragonGaming Debit Exception:{ex.Message}");
            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.InternalError, request.token);
        }
    }

    public async Task<DebitCreditRefundResponse_DragonGaming> Credit(CreditRequest_DragonGaming request)
    {
        try
        {
            var gameType = GetGameType(request.game_type);
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request.account_id);
            int result = 0;
            decimal tpAffterAmount = 0m;

            if (request.amount is 0)
            {
                result = 1;
            }
            else
            {
                result = Payout(request, tpCustInfo, out tpAffterAmount);
            }

            if (result > 0)
            {
                if (tpAffterAmount is 0)
                {
                    var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.Success, request.token, tpCustInfo.TpUserName, tpAffterAmount, tpCustInfo.TpCurrencyId, request.transaction_id);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.DuplicateSuccess, request.token, tpCustInfo.TpUserName, tpCustBalance.Balance, tpCustInfo.TpCurrencyId, request.transaction_id);
            }
            else if (result == -2)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.NoExistVaildBet, request.token, tpCustInfo.TpUserName, tpCustBalance.Balance, tpCustInfo.TpCurrencyId, request.transaction_id);
            }

            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.CreditInternalError, request.token, tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, request.transaction_id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DragonGaming Credit Exception:{ex.Message}");
            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.InternalError, request.token);
        }
    }

    public async Task<DebitCreditRefundResponse_DragonGaming> Refund(RefundRequest_DragonGaming request)
    {
        try
        {
            var gameType = GetGameType(request.game_type);
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)thisTp, request.account_id);

            int result = CancelBet(request, tpCustInfo, out decimal tpAffterAmount);
            if (result > 0)
            {
                if (tpAffterAmount == 0)
                {
                    var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                    tpAffterAmount = tpCustBalance.Balance;
                }
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.Success, request.token, tpCustInfo.TpUserName, tpAffterAmount, tpCustInfo.TpCurrencyId, request.transaction_id);
            }
            else if (result == -1)
            {
                var tpCustBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
                return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.DuplicateSuccess, request.token, tpCustInfo.TpUserName, tpCustBalance.Balance, tpCustInfo.TpCurrencyId, request.transaction_id);
            }

            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.RefundInternalError, request.token, tpCustInfo.TpUserName, tpCustInfo.TpCurrencyId, request.transaction_id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"DragonGaming Refund Exception:{ex.Message}");
            return new DebitCreditRefundResponse_DragonGaming(ErrorCodeOfDragonGaming.InternalError, request.token);
        }
    }

    #region Private Method


    private (string Token, string tpUserName, string tpGameType) GetToken(string token)
    {
        try
        {
            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(token));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            return (sessionKeyArr[0], sessionKeyArr[1], sessionKeyArr[2]);
        }
        catch
        {
            return (null, null, null);
        }
    }

    private GameType GetGameType(string tpGameType)
    {
        var gameType = GameType.DragonGaming_RNG;
        if (string.Equals(tpGameType, "slots", StringComparison.OrdinalIgnoreCase))
        {
            gameType = GameType.DragonGaming_RNG;
        }
        return gameType;
    }

    private int PlaceBet(DebitRequest_DragonGaming request, TpCustInfo tpCustInfo, GameType gameType, out decimal afterAmount)
    {
        var tpCurrency = _thirdPartyBll.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);
        var custBalance = _tpCustInfoBll.GetBalance((int)thisTp, tpCustInfo.TpUserName, (int)gameType);
        var bettingData = new BettingData_DragonGaming(request, (int)BettransType.Bet);

        if (tpCurrency.ExchangeRate != 0)
        {
            custBalance.Balance *= tpCurrency.ExchangeRate;
            bettingData.amount *= tpCurrency.ExchangeRate;
        }

        if (custBalance.Balance < 0)
        {
            afterAmount = custBalance.Balance / tpCurrency.ExchangeRate;
            return 0;
        }

        var checkBalance = custBalance.Balance - Math.Abs(request.amount);
        if (checkBalance < 0)
        {
            afterAmount = checkBalance / tpCurrency.ExchangeRate;
            return 0;
        }

        var result = _bettingService.PlaceBetRNG_DragonGaming(tpCustInfo, bettingData, out afterAmount);
        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }

    private int Payout(CreditRequest_DragonGaming request, TpCustInfo tpCustInfo, out decimal afterAmount)
    {
        var tpCurrency = _thirdPartyBll.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);
        var payoutData = new BettingData_DragonGaming(request, (int)BettransType.Payout);
        if (tpCurrency.ExchangeRate != 0)
        {
            payoutData.amount *= tpCurrency.ExchangeRate;
        }

        var result = _settledService.PayoutRNG_DragonGaming(tpCustInfo, payoutData, out afterAmount);
        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }

    private int CancelBet(RefundRequest_DragonGaming request, TpCustInfo tpCustInfo, out decimal afterAmount)
    {
        var tpCurrency = _thirdPartyBll.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);
        var refundData = new BettingData_DragonGaming(request, (int)BettransType.CancelBet);

        if (tpCurrency.ExchangeRate != 0)
        {
            refundData.amount *= tpCurrency.ExchangeRate;
        }

        var result = _settledService.CancelBetRNG_DragonGaming(tpCustInfo, refundData, out afterAmount);
        if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
        {
            afterAmount /= tpCurrency.ExchangeRate;
        }
        return result;
    }

    #endregion

}