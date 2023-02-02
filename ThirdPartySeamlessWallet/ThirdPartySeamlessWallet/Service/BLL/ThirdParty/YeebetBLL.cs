using MySql.Data.MySqlClient;
using System.Net.Sockets;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class YeebetBLL : IYeebetBLL
{
    public Tp ThirdParty { get; set; }
    public GameType GameType { get; set; }
    public TpCategory TpCategory { get; set; }

    private readonly ThirdPartyBLL _thirdPartyBLL;
    private readonly TpCustInfoBLL _tpCustInfoBLL;
    private readonly ILogger<YeebetBLL> _logger;

    private readonly BettingService _bettingService;
    private readonly SettledService _settledService;

    private readonly MySqlClient _mainDbClient;

    public YeebetBLL(IServiceProvider provider)
    {
        ThirdParty = Tp.Yeebet;
        GameType = GameType.Yeebet_LiveCasino;
        TpCategory = TpCategory.LiveCasino;

        _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _logger = provider.GetService<ILogger<YeebetBLL>>();

        _bettingService = provider.GetService<BettingService>();
        _settledService = provider.GetRequiredService<SettledService>();

        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public async Task<BalanceResponse_Yeebet> Balance(string username)
    {
        var message = string.Empty;

        if (string.IsNullOrWhiteSpace(username))
        {
            message = "username is InVaild.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return new BalanceResponse_Yeebet(YeebetErrorCode.SystemError, message);
        }

        if (!GetTpInfoByUserName(username, out TpCustInfo tpCustInfo, out message))
            return new BalanceResponse_Yeebet(YeebetErrorCode.SystemError, message);

        if (!GetBalance(tpCustInfo.TpUserName, out decimal balance, out message))
            return new BalanceResponse_Yeebet(YeebetErrorCode.SystemError, message);

        return new BalanceResponse_Yeebet(YeebetErrorCode.Success, "", balance);
    }

    public async Task<DeductResponse_Yeebet> Deduct(DeductRequest_Yeebet request)
    {
        var message = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForDeduct(request, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        if (!GetTpInfoByUserName(request.username, out TpCustInfo tpCustInfo, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        var partitionTime = DateTime.UtcNow;

        var data = new BettingDataLC_Yeebet()
        {
            appid = request.appid,
            username = request.username,
            amount = request.amount,
            notifyid = request.notifyid,
            type = request.type,
            serialnumber = request.serialnumber,
            sign = request.sign,
            bets = request.bets.Replace("\\\"", "\""),
            FromAPIType = BettransLC_YeebetTypeEnum.FromWithdraw,
            TicketType = BettransType.Bet,
            TicketStatus = TicketStatus.UnSettled
        };

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        if (!GetBalanceByHalfSecCache(tpCustInfo.TpUserName, out balance, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        if (balance + data.amount < 0) //餘額不足
            return new DeductResponse_Yeebet(YeebetErrorCode.InsufficientBalance, request.serialnumber, "", "Insufficient balance.");

        if (!GetExistData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existDatas, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message, 0);

        if (existDatas.Any()) //已存在指定有效投注
            return new DeductResponse_Yeebet(YeebetErrorCode.Success, request.serialnumber, existDatas.First().SQ.ToString(), "already exist this data.", 0);

        await using var conn = _mainDbClient.GetMySqlConnection();
        conn.Open();
        await using var trans = await conn.BeginTransactionAsync();

        if (!PlaceData(partitionTime, data, trans, out int SQ, out message))
        {
            trans.Rollback();
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);
        }

        var orderno = GenOrderNo(partitionTime, SQ);

        if (data.amount != 0)
        {
            if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
            {
                trans.Rollback();
                return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);
            }
        }

        trans.Commit();
        return new DeductResponse_Yeebet(YeebetErrorCode.Success, request.serialnumber, orderno, "", balance);
    }

    public async Task<DepositResponse_Yeebet> Deposit(DepositRequest_Yeebet request)
    {
        var message = string.Empty;
        string newRoundId = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForDeposit(request, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (!GetTpInfoByUserName(request.username, out TpCustInfo tpCustInfo, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        var partitionTime = DateTime.UtcNow;

        var data = new BettingDataLC_Yeebet()
        {
            appid = request.appid,
            username = request.username,
            amount = request.amount,
            notifyid = request.notifyid,
            type = request.type,
            serialnumber = request.serialnumber,
            sign = request.sign,
            bets = request.bets.Replace("\\\"", "\""),
            FromAPIType = BettransLC_YeebetTypeEnum.FromDeposit,
            TicketType = BettransType.Payout,
            TicketStatus = TicketStatus.UnSettled
        };

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        if (!GetExistData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existDatas, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (existDatas.Any())
        {
            //已存在指定有效投注
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", "already exist this data.", balance);
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        conn.Open();
        await using var trans = await conn.BeginTransactionAsync();

        if (!PlaceData(partitionTime, data, trans, out int SQ, out message))
        {
            trans.Rollback();
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
        }

        var orderno = GenOrderNo(partitionTime, SQ);

        if (data.amount != 0)
        {
            if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
            {
                trans.Rollback();
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
            }
        }
        else
        {
            //lost => 沒有派彩，balance取db資料
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new DepositResponse_Yeebet(YeebetErrorCode.Success, orderno, message);
        }

        if (!InsertUnsettleData(partitionTime, data.serialnumber, out message, trans))
        {
            trans.Rollback();
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
        }

        trans.Commit();
        return new DepositResponse_Yeebet(YeebetErrorCode.Success, orderno, "", balance);
    }

    public async Task<RollbackResponse_Yeebet> Rollback(RollbackRequest_Yeebet request)
    {
        throw new NotImplementedException();
    }

    #region Private Methods

    private bool CheckRequestForDeduct(DeductRequest_Yeebet request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (request.amount > 0)
        {
            message = "request.amount is Invalid. amount > 0.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        //暫不簡查，稍後測試再看要怎麼檢核
        //if (request.type <= 0)
        //{
        //    message = "request.type is Invalid.";
        //    _logger.LogError($"CheckRequest Fail:{message}");
        //    return false;
        //}

        if (string.IsNullOrWhiteSpace(request.serialnumber))
        {
            message = "request.serialnumber is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }
        return true;
    }

    private bool CheckRequestForDeposit(DepositRequest_Yeebet request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (request.amount < 0)
        {
            message = "request.amount is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        //暫不簡查，稍後測試再看要怎麼檢核
        //if (request.type <= 0)
        //{
        //    message = "request.type is Invalid.";
        //    _logger.LogError($"CheckRequest Fail:{message}");
        //    return false;
        //}

        if (string.IsNullOrWhiteSpace(request.serialnumber))
        {
            message = "request.serialnumber is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }

    private bool CheckRequestForRollBack(BetRequest_Kiss918 request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (request.playerID == 0)
        {
            message = "request.playerID is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.gameID))
        {
            message = "request.gameID is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.playSessionID))
        {
            message = "request.playSessionID is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.referenceID))
        {
            message = "request.referenceID is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }

    private bool CheckThirdParty(string username, out string message)
    {
        message = string.Empty;
        try
        {
            var result = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, username);

            if (result == null)
            {
                message = "username not found.";
                _logger.LogError($"CheckThirdParty Fail:{message}");
                return false;
            }

            if (result.Locked)
            {
                message = "username is locked.";
                _logger.LogError($"CheckThirdParty Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"CheckThirdParty Exception:{ex.ToString()}");
            return false;
        }
    }

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

    private bool GetTpCurrency(int currencyId, out TpCurrency tpCurrency, out string message)
    {
        message = string.Empty;
        tpCurrency = null;
        try
        {
            tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, currencyId);
            if (tpCurrency == null)
            {
                message = "tpCurrency is null.";
                _logger.LogError($"GetTpCurrency Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"GetTpCurrency Exception:{ex.Message}");
            return false;
        }
    }

    private bool GetBalanceByHalfSecCache(string username, out decimal balance, out string message)
    {
        message = string.Empty;
        balance = 0;
        try
        {
            var result = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, username, (int)GameType);

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

    //private bool IsExistBetData(DateTime partitionTime, BettingDataLC_Yeebet data,
    //    out IEnumerable<BettingDataLC_Yeebet> existDatas, out string message)
    //{
    //    message = string.Empty;
    //    existDatas = null;
    //    try
    //    {

    //        existDatas = _settledService.IsExistBet_Kiss918(partitionTime, data);
    //        if (existDatas.Count() != 1)
    //        {
    //            //不存在指定有效投注
    //            message = "not exist valid bet by playSessionID.";
    //            _logger.LogError($"IsExistBetData Fail:{message}");
    //            return false;
    //        }
    //        if (existDatas.First().amount != data.amount)
    //        {
    //            //退款金額與已存在投注不符
    //            message = "amount not equal with exist valid bet by playSessionID.";
    //            _logger.LogError($"IsExistBetData Fail:{message}");
    //            return false;
    //        }

    //        return true;
    //    }
    //    catch (Exception ex)
    //    {
    //        message = "exception happen.";
    //        _logger.LogError(ex, $"IsExistBetData Exception:{ex.Message}");
    //        return false;
    //    }
    //}

    private bool GetExistData(DateTime partitionTime, BettingDataLC_Yeebet data,
        out IEnumerable<BettingDataLC_Yeebet> existDatas, out string message)
    {
        message = string.Empty;
        existDatas = null;
        try
        {
            existDatas = _bettingService.IsExist_Yeebet(partitionTime, data);
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"IsExistData Exception:{ex.Message}");
            return false;
        }
    }

    private bool PlaceData(DateTime partitionTime, BettingDataLC_Yeebet data, MySqlTransaction trans, out int SQ, out string message)
    {
        SQ = 0;
        message = string.Empty;
        var result = false;
        try
        {
            switch (data.FromAPIType)
            {
                case BettransLC_YeebetTypeEnum.FromWithdraw:
                    result = _bettingService.PlaceBet_Yeebet(partitionTime, data, trans, out SQ);
                    break;
                case BettransLC_YeebetTypeEnum.FromDeposit:
                    result = _settledService.Payout_Yeebet(partitionTime, data, trans, out SQ);
                    break;
                    //case BettransLC_YeebetTypeEnum.FromRollback:
                    //    result = _settledService.Refund_Kiss918(partitionTime, data, trans);
                    //    break;
            }

            if (!result)
            {
                message = "place data fail.";
                _logger.LogError($"PlaceData Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"PlaceData Exception:{ex.Message}");
            return false;
        }
    }

    private bool SetBalance(TpCustInfo tpCustInfo, BettingDataLC_Yeebet data, decimal rate,
                            out string message, out decimal afterAmount, out int bonusId,
                            MySqlConnection conn, MySqlTransaction trans)
    {
        message = string.Empty;
        afterAmount = 0;
        bonusId = 0;
        var result = false;
        try
        {
            switch (data.FromAPIType)
            {
                case BettransLC_YeebetTypeEnum.FromWithdraw:
                    result = _bettingService.SetBalance_Yeebet(tpCustInfo, data,
                             out afterAmount, ref bonusId, conn, trans);
                    break;
                case BettransLC_YeebetTypeEnum.FromDeposit:
                    result = _settledService.SetBalance_Yeebet(tpCustInfo, data,
                             TransType.Payout, "Payout", out afterAmount, ref bonusId, conn, trans);
                    break;
                    //case BettransLC_YeebetTypeEnum.FromRollback:
                    //    result = _settledService.SetBalance_Kiss918(tpCustInfo, data,
                    //             TransType.Payout, "Refund", out afterAmount, ref bonusId, conn, trans);
                    //    break;
            }

            if (!result)
            {
                message = "set balance fail.";
                _logger.LogError($"SetBalance Fail:{message}");
                return false;
            }

            afterAmount = (rate > 0) ? afterAmount / rate : afterAmount;

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"SetBalance Exception:{ex.Message}");
            return false;
        }
    }

    private bool InsertUnsettleData(DateTime partitionTime, string serialnumber, out string message, MySqlTransaction trans)
    {
        message = string.Empty;
        try
        {
            var result = _settledService.InsertUnsettle_Yeebet(partitionTime, serialnumber, trans);

            if (!result)
            {
                message = "insert unsettle data fail.";
                _logger.LogError($"InsertUnsettle Fail:{message}");
                return false;
            }
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"InsertUnsettle Exception:{ex.Message}");
            return false;
        }
    }

    private string GenOrderNo(DateTime partitionTime, int SQ)
    => $"{partitionTime.ToString("yyyyMM")}-{SQ}";
    #endregion
}
