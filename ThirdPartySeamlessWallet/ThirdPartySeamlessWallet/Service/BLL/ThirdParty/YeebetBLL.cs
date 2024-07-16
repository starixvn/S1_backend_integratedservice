using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

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

        if (request.type == (int)typeEnum.Betting) //下注需檢查cust是否在線上
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
            bets = request.bets.Replace("\\\"", "\""),
            FromAPIType = BettransLC_YeebetTypeEnum.FromWithdraw,
            TicketType = CheckBettransType(request.type),
            TicketStatus = CheckTicketStatus(request.type)
        };

        if (!GetBalanceByHalfSecCache(tpCustInfo.TpUserName, out balance, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);

        if (balance + data.amount < 0) //餘額不足，這邊會用換算過後的餘額去作比較，ex. IDR(1:1000)= main wallet / 1000 + bet amount / 1000 < 0
            return new DeductResponse_Yeebet(YeebetErrorCode.InsufficientBalance, request.serialnumber, "", "Insufficient balance.");

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        if (!GetExistData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existDatas, out message))
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message, 0);

        if (existDatas.Any()) //已存在指定有效投注
        {
            var exitOrderno = GenOrderNo(partitionTime, existDatas.First().SQ);
            return new DeductResponse_Yeebet(YeebetErrorCode.Success, request.serialnumber, exitOrderno, "already exist this data.", balance);
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!PlaceData(partitionTime, data, trans, out int SQ, out message))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);
        }

        var orderno = GenOrderNo(partitionTime, SQ);

        if (data.amount != 0)
        {
            if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, request.serialnumber, "", message);
            }
        }

        if (request.type == (int)typeEnum.CancelOrderOrBetting   //CancelOrder已派彩注單撤銷
            || request.type == (int)typeEnum.ReEvaluation)       //Re_Payout需要重新結算
            if (!InsertUnsettleData(partitionTime, data.serialnumber, out message, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new DeductResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
            }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
        return new DeductResponse_Yeebet(YeebetErrorCode.Success, request.serialnumber, orderno, "", balance);
    }

    public async Task<DepositResponse_Yeebet> Deposit(DepositRequest_Yeebet request)
    {
        var message = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForDeposit(request, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (!GetTpInfoByUserName(request.username, out TpCustInfo tpCustInfo, out message))
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
            bets = request.bets.Replace("\\\"", "\""),
            FromAPIType = BettransLC_YeebetTypeEnum.FromDeposit,
            TicketType = CheckBettransType(request.type),
            TicketStatus = CheckTicketStatus(request.type)
        };

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        if (request.type == (int)typeEnum.CancelOrderOrBetting)
            if (!IsExistBetData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existBetDatas, out message))
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, message);

        if (!GetExistData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existDatas, out message))
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

        if (existDatas.Any())
        {
            //Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins

            //已存在指定有效投注
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);

            var exitOrderno = GenOrderNo(partitionTime, existDatas.First().SQ);
            return new DepositResponse_Yeebet(YeebetErrorCode.Success, exitOrderno, "already exist this data.", balance);
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!PlaceData(partitionTime, data, trans, out int SQ, out message))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
        }

        var orderno = GenOrderNo(partitionTime, SQ);

        if (data.amount != 0)
        {
            if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
            }
        }
        else
        {
            //lost => 沒有派彩，balance取db資料
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, orderno, message);
        }

        if ((request.type == (int)typeEnum.CancelOrderOrBetting && !IsCancelBet(data.bets))  //取消已派彩注單
            || request.type == (int)typeEnum.Awards
            || request.type == (int)typeEnum.ReEvaluation)  //因為Refund Wager沒辦法抄單，所以不作帳
            if (!InsertUnsettleData(partitionTime, data.serialnumber, out message, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new DepositResponse_Yeebet(YeebetErrorCode.SystemError, "", message);
            }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        //Thread.Sleep(5 * 60 * 1000);  //Test Rollback, sleep 5 mins

        return new DepositResponse_Yeebet(YeebetErrorCode.Success, orderno, "", balance);
    }

    public async Task<RollbackResponse_Yeebet> Rollback(RollbackRequest_Yeebet request)
    {
        var message = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForRollBack(request, out message))
            return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);

        if (!GetTpInfoByUserName(request.username, out TpCustInfo tpCustInfo, out message))
            return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);

        var partitionTime = DateTime.UtcNow;

        var data = new BettingDataLC_Yeebet()
        {

            appid = request.appid,
            username = request.username,
            amount = request.amount,
            notifyid = request.notifyid,
            type = request.type,
            serialnumber = request.serialnumber,
            bets = null,
            errmsg = request.errmsg,
            IsRollback = 1, //true
            FromAPIType = BettransLC_YeebetTypeEnum.FromRollback,
            TicketType = CheckBettransType(request.type),
            TicketStatus = CheckTicketStatus(request.type)
        };

        data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;

        if (!GetExistData(partitionTime, data, out IEnumerable<BettingDataLC_Yeebet> existDatas, out message))
            return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);

        if (!existDatas.Any())
        {
            //不存在指定有效資料，不需rollback
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);

            return new RollbackResponse_Yeebet(YeebetErrorCode.Success, "not exist this data.", balance);
        }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        if (!PlaceData(partitionTime, data, trans, out int SQ, out message))
        {
            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
            return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);
        }

        if (data.amount != 0)
        {
            if (!SetBalance(tpCustInfo, data, tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);
            }
        }
        else
        {
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);
        }

        if ((request.type == (int)typeEnum.CancelOrderOrBetting && !IsCancelBet(existDatas.FirstOrDefault().bets))
            || request.type == (int)typeEnum.Awards
            || request.type == (int)typeEnum.ReEvaluation)
            if (!InsertUnsettleData(partitionTime, data.serialnumber, out message, trans))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new RollbackResponse_Yeebet(YeebetErrorCode.SystemError, message);
            }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        return new RollbackResponse_Yeebet(YeebetErrorCode.Success, "", balance);
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

        if (request.type != (int)typeEnum.Betting
            && request.type != (int)typeEnum.CancelOrderOrBetting
            && request.type != (int)typeEnum.ReEvaluation)
        {
            message = "request.type is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

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

        if (request.type != (int)typeEnum.CancelOrderOrBetting
            && request.type != (int)typeEnum.Awards
            && request.type != (int)typeEnum.ReEvaluation)
        {
            message = "request.type is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.serialnumber))
        {
            message = "request.serialnumber is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        return true;
    }

    private bool CheckRequestForRollBack(RollbackRequest_Yeebet request, out string message)
    {
        message = string.Empty;

        if (request == null)
        {
            message = "request is null.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (request.type != (int)typeEnum.Betting
            && request.type != (int)typeEnum.CancelOrderOrBetting
            && request.type != (int)typeEnum.Awards
            && request.type != (int)typeEnum.ReEvaluation)
        {
            message = "request.type is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.serialnumber))
        {
            message = "request.serialnumber is Invalid.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return false;
        }

        //暫不檢查
        //if (string.IsNullOrWhiteSpace(request.errmsg))
        //{
        //    message = "request.errmsg is Invalid.";
        //    _logger.LogError($"CheckRequest Fail:{message}");
        //    return false;
        //}

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

    private bool IsExistBetData(DateTime partitionTime, BettingDataLC_Yeebet data,
        out IEnumerable<BettingDataLC_Yeebet> existDatas, out string message)
    {
        message = string.Empty;
        existDatas = null;
        try
        {
            existDatas = _settledService.IsExistBet_Yeebet(partitionTime, data);
            if (existDatas.Count() != 1)
            {
                //不存在指定有效投注
                message = "not exist valid bet by serialnumber.";
                _logger.LogError($"IsExistBetData Fail:{message}");
                return false;
            }
            if (existDatas.First().amount != (data.amount * -1))
            {
                //退款金額與已存在投注不符
                message = "amount not equal with exist valid bet by serialnumber.";
                _logger.LogError($"IsExistBetData Fail:{message}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"IsExistBetData Exception:{ex.Message}");
            return false;
        }
    }

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
                    if (data.type == (int)typeEnum.Betting)
                        result = _bettingService.PlaceBet_Yeebet(partitionTime, data, trans, out SQ);
                    else if (data.type == (int)typeEnum.CancelOrderOrBetting)
                        result = _settledService.Refund_Yeebet(partitionTime, data, trans, out SQ);
                    else if (data.type == (int)typeEnum.Awards) //重新派彩(少派彩，減項)
                        result = _settledService.Payout_Yeebet(partitionTime, data, trans, out SQ);
                    break;
                case BettransLC_YeebetTypeEnum.FromDeposit:
                    if (data.type == (int)typeEnum.Awards || data.type == (int)typeEnum.ReEvaluation)//重新派彩(多派彩、加項)
                        result = _settledService.Payout_Yeebet(partitionTime, data, trans, out SQ);
                    else if (data.type == (int)typeEnum.CancelOrderOrBetting)
                        result = _settledService.Refund_Yeebet(partitionTime, data, trans, out SQ);
                    break;
                case BettransLC_YeebetTypeEnum.FromRollback:
                    //下注回滾異常、重新派彩(少派彩，減項)回滾異常、取消下注回滾異常、派彩回滾異常、重新派彩(多派彩、加項)回滾異常
                    result = _settledService.Rollback_Yeebet(partitionTime, data, trans);
                    break;
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
                    if (data.type == (int)typeEnum.Betting)
                        result = _bettingService.SetBalance_Yeebet(tpCustInfo, data, "withdraw",
                             out afterAmount, ref bonusId, conn, trans);
                    else if (data.type == (int)typeEnum.CancelOrderOrBetting  //取消已派彩注單
                        || data.type == (int)typeEnum.ReEvaluation) //重新派彩(少派彩，減項)
                        result = _settledService.SetBalance_Yeebet(tpCustInfo, data,
                                 TransType.Payout, YeebetTypeMemo(data.type), out afterAmount, ref bonusId, conn, trans);
                    break;
                case BettransLC_YeebetTypeEnum.FromDeposit: //7=取消下注/取消已派彩注單、9=派彩、10=重新派彩(多派彩、加項)
                    result = _settledService.SetBalance_Yeebet(tpCustInfo, data,
                             TransType.Payout, YeebetTypeMemo(data.type), out afterAmount, ref bonusId, conn, trans);
                    break;
                case BettransLC_YeebetTypeEnum.FromRollback:
                    if (data.type == (int)typeEnum.Betting) //下注回滾異常
                        result = _bettingService.SetBalance_Yeebet(tpCustInfo, data, $"{YeebetTypeMemo(data.type)}_rollback",
                                 out afterAmount, ref bonusId, conn, trans);
                    else if (data.type == (int)typeEnum.CancelOrderOrBetting
                        || data.type == (int)typeEnum.Awards
                        || data.type == (int)typeEnum.ReEvaluation) //重新派彩(少派彩，減項)回滾異常、重新派彩(多派彩、加項)回滾異常
                        result = _settledService.SetBalance_Yeebet(tpCustInfo, data,
                            TransType.Payout, $"{YeebetTypeMemo(data.type)}_rollback",
                             out afterAmount, ref bonusId, conn, trans);
                    break;
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

    private BettransType CheckBettransType(int type)
    {
        switch (type)
        {
            case 1: return BettransType.Bet;
            case 7: return BettransType.CancelBet;
            case 9:
            case 10: return BettransType.Payout;
            default: throw new YeebetException();
        }
    }

    private TicketStatus CheckTicketStatus(int type)
    {
        switch (type)
        {
            case 1:
            case 9:
            case 10: return TicketStatus.UnSettled;
            case 7: return TicketStatus.Refund;
            default: throw new YeebetException();
        }
    }

    /// <summary>
    /// 是否為取消下注
    /// </summary>
    /// <param name="bets"></param>
    /// <returns>true(取消下注)，false(取消已派彩注單)</returns>
    private bool IsCancelBet(string bets)
    {
        var betsInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(bets);
        if (betsInfo.ContainsKey("gameresult") && string.IsNullOrWhiteSpace(betsInfo["gameresult"]))
            return true;

        return false;
    }

    private string YeebetTypeMemo(int type)
    {
        switch (type)
        {
            case 1: return "Bet";
            case 7: return "Refund";
            case 9: return "Payout";
            case 10: return "Resettle";
            default: return string.Empty;
        }
    }
    #endregion
}
