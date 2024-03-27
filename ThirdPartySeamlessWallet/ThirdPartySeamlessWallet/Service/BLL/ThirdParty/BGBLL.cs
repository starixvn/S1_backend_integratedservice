using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

public class BGBLL : IBGBLL
{
    public Tp ThirdParty { get; set; }
    public GameType GameType { get; set; }
    public TpCategory TpCategory { get; set; }

    //下注闲1平倍 playId:3489660929
    //下注闲2平倍 playId:3489660932
    //下注闲3平倍 playId:3489660944
    //下注闲1翻倍 playId:3489660930
    //下注闲2翻倍 playId:3489660936
    //下注闲3翻倍 playId:3489660960
    private readonly string[] _bullbullDoublePlayIds = new string[] { "3489660930", "3489660936", "3489660960" };

    private readonly Tools _tools;
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

        _tools = provider.GetService<Tools>();
        _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
        _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
        _logger = provider.GetService<ILogger<BGBLL>>();

        _bettingService = provider.GetService<BettingService>();
        _settledService = provider.GetRequiredService<SettledService>();

        _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
    }

    public string GetSign(SignParameters_BG signParameters, bool isSignNeedLoginId = true)
    {
        if (string.IsNullOrWhiteSpace(signParameters.loginId))
            throw new BGException(BGErrorCode.InternalError, $"CheckRequest Fail: loginId is invaild.");

        // Get Cust.SiteId By request.loginId
        var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, signParameters.loginId);
        if (tpCustInfo == null)
            throw new BGException(BGErrorCode.InternalError, $"CheckRequest Fail: TpCustInfo not found.");

        // Get Setting
        var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(_thirdPartyBLL.GetThirdPartyBySite((int)ThirdParty, tpCustInfo.SiteId).Setting);
        var secretKey = setting.ContainsKey($"ApiSecretKey_{tpCustInfo.TpCurrencyId}") && !string.IsNullOrEmpty(setting[$"ApiSecretKey_{tpCustInfo.TpCurrencyId}"])
            ? setting[$"ApiSecretKey_{tpCustInfo.TpCurrencyId}"]
            : throw new BGException(BGErrorCode.InternalError, $"ApiSecretKey_{tpCustInfo.TpCurrencyId} is invaild.");

        return _tools.ToMD5(signParameters.random + signParameters.sn + (isSignNeedLoginId ? signParameters.loginId : "") + signParameters.amount + secretKey);
    }

    public ResponseModel_BG UserBalance(SeamlessWalletRequest request, UserBalanceParameters_BG parameters)
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

    public async Task<ResponseModel_BG> OrderTransfer(SeamlessWalletRequest request, OrderTransferParameters_BG parameters)
    {
        #region Test recalc

        //Thread.Sleep(5 * 60 * 1000); //Sleep 5 mins

        #endregion

        var message = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForOrderTransfer(parameters, out decimal amount, out message))
        {
            _logger.LogError($"CheckRequest Fail:{message}");
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
        }

        if (!GetTpInfoByUserName(parameters.loginId, out TpCustInfo tpCustInfo, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (!CheckThirdParty(tpCustInfo.TpUserName, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (balance + amount < 0) //餘額不足
            return new ResponseModel_BG(request, BGErrorCode.InvalidParams, "Insufficient balance.", parameters.sn);

        var partitionTime = DateTime.UtcNow;

        if (!GetExistData(partitionTime, parameters.random, parameters.orders.Select(x => x.orderId),
            out IEnumerable<BettingDataLC_BG> existDatas, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (existDatas.Any()) //已存在有效投注
            if (existDatas.Count() == parameters.orders.Count())
                return new ResponseModel_BG(request, new TransferModel_BG(parameters.sn, parameters.userId, parameters.tranId, balance));
            else
            {
                var alreadyExistOrders = existDatas.Select(x => x.orderId).ToList();
                _logger.LogInformation($"Check Exist Data: already exist orderIds={string.Join(",", alreadyExistOrders)}");
                parameters.orders = parameters.orders.Where(x => !alreadyExistOrders.Contains(x.orderId.ToString())).OrderBy(x => x.orderId); //remove exist orderIds
            }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        foreach (var order in parameters.orders)
        {
            var balanceAmount = CheckOrderAmount(order.gameId, order.playId, order.amount);

            var data = new BettingDataLC_BG()
            {
                random = parameters.random,
                sn = parameters.sn,
                userId = parameters.userId,
                loginId = parameters.loginId,
                amount = amount,
                moduleId = parameters.moduleId,
                tranId = parameters.tranId ?? "",
                serverTime = parameters.serverTime ?? null,
                orderId = order.orderId.ToString(),
                gameId = order.gameId,
                orderAmount = order.amount, //BullBull double
                issueId = order.issueId,
                playId = order.playId,
                orderFrom = (BGOrderFromEnum)order.orderFrom,
                fromIp = order.fromIp,
                orderTime = order.orderTime ?? null,
                noComm = order.noComm.HasValue ? order.noComm.ToString() : "",
                betContent = order.betContent ?? "",
                FromAPIType = BettransLC_BGTypeEnum.OrderTransfer,
                TicketType = BettransType.Bet,
                TicketStatus = TicketStatus.UnSettled
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;
            data.orderAmount = (tpCurrency.ExchangeRate > 0) ? data.orderAmount * tpCurrency.ExchangeRate : data.orderAmount;
            balanceAmount = (tpCurrency.ExchangeRate > 0) ? balanceAmount * tpCurrency.ExchangeRate : balanceAmount;

            if (!PlaceData(partitionTime, data, trans, out message))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
            }

            data.orderAmount = balanceAmount;

            if (data.orderAmount != 0)
            {
                if (!SetBalance(tpCustInfo, data, 0,
                    tpCurrency.ExchangeRate, out message, out balance, out int bonusId, conn, trans))
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
                }
            }
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        #region Test recalc

        //Thread.Sleep(5 * 60 * 1000); //Sleep 5 mins

        #endregion

        return new ResponseModel_BG(request, new TransferModel_BG(parameters.sn, parameters.userId, parameters.tranId, balance));
    }

    public async Task<ResponseModel_BG> CalcTransfer(SeamlessWalletRequest request, CalcTransferParameters_BG parameters)
    {
        #region Test recalc

        //if (parameters.recalc == "0")
        //    Thread.Sleep(5 * 60 * 1000); //Sleep 5 mins

        #endregion

        var message = string.Empty;
        decimal balance = 0;

        if (!CheckRequestForCalcTransfer(parameters, out decimal amount, out int recalc, out message))
        {
            _logger.LogError($"CheckRequest Fail:{message}");
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
        }

        if (!GetTpInfoByUserName(parameters.loginId, out TpCustInfo tpCustInfo, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (!GetTpCurrency(tpCustInfo.CurrencyId, out TpCurrency tpCurrency, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        var partitionTime = DateTime.UtcNow;

        if (!GetExistData(partitionTime, parameters.random, parameters.orders.Select(x => x.orderId),
            out IEnumerable<BettingDataLC_BG> existDatas, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        if (existDatas.Any()) //已存在有效投注
            if (existDatas.Count() == parameters.orders.Count())
            {
                //已存在指定有效投注
                if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

                var alreadyExistOrders = existDatas.Select(x => x.orderId).ToList();
                _logger.LogInformation($"Check Exist Data: already exist orderIds={string.Join(",", alreadyExistOrders)}");
                return new ResponseModel_BG(request, new TransferModel_BG(parameters.sn, parameters.userId, parameters.tranId, balance));
            }
            else
            {
                var alreadyExistOrders = existDatas.Select(x => x.orderId).ToList();
                _logger.LogInformation($"Check Exist Data: already exist orderIds={string.Join(",", alreadyExistOrders)}");
                parameters.orders = parameters.orders.Where(x => !alreadyExistOrders.Contains(x.orderId.ToString())).OrderBy(x => x.orderId); //remove exist orderIds
            }

        await using var conn = _mainDbClient.GetMySqlConnection();
        await conn.OpenAsync();
        await using var trans = await conn.BeginTransactionAsync();

        foreach (var order in parameters.orders)
        {
            var data = new BettingDataLC_BG()
            {
                random = parameters.random,
                sn = parameters.sn,
                userId = parameters.userId,
                loginId = parameters.loginId,
                amount = amount,
                moduleId = parameters.moduleId,
                tranId = parameters.tranId ?? "",
                gameResult = parameters.gameResult ?? "",
                recalc = recalc,
                serverTime = parameters.serverTime ?? null,
                orderId = order.orderId.ToString(),
                gameId = order.gameId,
                orderAmount = order.orderAmount,
                calcAmount = order.amount,
                validAmount = order.validAmount,
                orderTime = order.lastUpdateTime ?? null,
                orderStatus = (BGOrderStatusEnum)order.orderStatus,
                FromAPIType = BettransLC_BGTypeEnum.CalcTransfer,
                TicketType = ConvertToBettransType((BGOrderStatusEnum)order.orderStatus),
                TicketStatus = ConvertToTicketStatus((BGOrderStatusEnum)order.orderStatus)
            };

            data.amount = (tpCurrency.ExchangeRate > 0) ? data.amount * tpCurrency.ExchangeRate : data.amount;
            data.orderAmount = (tpCurrency.ExchangeRate > 0) ? data.orderAmount * tpCurrency.ExchangeRate : data.orderAmount;
            data.calcAmount = (tpCurrency.ExchangeRate > 0) ? data.calcAmount * tpCurrency.ExchangeRate : data.calcAmount;
            data.validAmount = (tpCurrency.ExchangeRate > 0) ? data.validAmount * tpCurrency.ExchangeRate : data.validAmount;
            decimal payoutAmount = data.calcAmount;

            //Cancel Bet要檢查是否有有效投注
            if (data.TicketStatus == TicketStatus.Refund)
                if (!IsExistBetData(partitionTime, data, out IEnumerable<BettingDataLC_BG> existBetDatas, out payoutAmount, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

            //如果該注單已有結算成功的紀錄，又重新結算，派彩金額會需要經過計算後得知是扣款(-)還是多派(+)
            if (data.recalc == 1 && data.TicketStatus != TicketStatus.Refund)
                if (!IsExistPayoutData(partitionTime, data, out payoutAmount, out message))
                    return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

            if (!PlaceData(partitionTime, data, trans, out message))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
            }

            if (payoutAmount != 0)
            {
                if (!SetBalance(tpCustInfo, data, payoutAmount, tpCurrency.ExchangeRate,
                    out message, out balance, out int bonusId, conn, trans))
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
                }
            }

            switch (data.orderStatus)
            {
                case BGOrderStatusEnum.SettledWin:
                case BGOrderStatusEnum.SettledDraw:
                case BGOrderStatusEnum.SettledLose:
                case BGOrderStatusEnum.Expired:
                    if (!InsertUnsettleData(partitionTime, data.orderId, out message, trans))
                    {
                        await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                        return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
                    }
                    break;
            }
        }

        if (amount == 0)
        {
            //派彩amount=0，balance取db資料
            if (!GetBalance(tpCustInfo.TpUserName, out balance, out message))
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);
            }
        }

        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

        #region Test recalc

        //Thread.Sleep(5 * 60 * 1000); //Sleep 5 mins

        #endregion

        return new ResponseModel_BG(request, new TransferModel_BG(parameters.sn, parameters.userId, parameters.tranId, balance));
    }

    public ResponseModel_BG OrderStatus(SeamlessWalletRequest request, OrderStatusParameters_BG parameters)
    {
        var message = string.Empty;
        var resultList = new List<OrderStatusModel_BG>();

        if (parameters.moduleId != 2)  //目前只有串接视讯模块(2)
        {
            message = "request.moduleId is Invalid. moduleId is not live casino.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, parameters.sn);
        }

        if (parameters.orderIds == null || !parameters.orderIds.Any())
        {
            message = "request.orderIds is Invalid. orderIds is null or empty.";
            _logger.LogError($"CheckRequest Fail:{message}");
            return new ResponseModel_BG(request, BGErrorCode.InvalidParams, message, parameters.sn);
        }

        var partitionTime = DateTime.UtcNow;

        if (!GetExistData(partitionTime, null, parameters.orderIds,
                out IEnumerable<BettingDataLC_BG> existDatas, out message))
            return new ResponseModel_BG(request, BGErrorCode.InternalError, message, parameters.sn);

        foreach (var orderId in parameters.orderIds)
        {
            var data = existDatas.Where(x => x.orderId == orderId.ToString());
            if (!data.Any())
                resultList.Add(new OrderStatusModel_BG(orderId, (int)BGOrderStatusEnum.NotExist));
            else
                resultList.Add(new OrderStatusModel_BG(orderId, (int)data.MaxBy(x => x.SQ).orderStatus));
        }
        return new ResponseModel_BG(request, resultList);
    }

    #region Private Method

    private bool GetTpInfoByUserName(string username, out TpCustInfo tpCustInfo, out string message)
    {
        message = string.Empty;
        tpCustInfo = null;
        try
        {
            tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, username);
            if (tpCustInfo == null || string.IsNullOrWhiteSpace(tpCustInfo.TpUserName))
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
    private bool CheckRequestForOrderTransfer(OrderTransferParameters_BG parameters, out decimal amount, out string message)
    {
        message = string.Empty;
        amount = 0;

        if (parameters == null)
        {
            message = "request is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.random))
        {
            message = "request.random is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.userId))
        {
            message = "request.userId is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.amount))
        {
            message = "request.amount is Invalid. amount > 0.";
            return false;
        }

        if (!decimal.TryParse(parameters.amount, out amount))
        {
            message = "request.amount is Invalid. this value is not decimal type.";
            return false;
        }
        else
        {
            if (amount > 0)
            {
                message = "request.amount is Invalid. amount > 0.";
                return false;
            }

        }

        if (parameters.moduleId != 2)  //目前只有串接视讯模块(2)
        {
            message = "request.moduleId is Invalid. moduleId is not live casino.";
            return false;
        }

        if (parameters.orders == null || !parameters.orders.Any())
        {
            message = "request.orders is Invalid. orders is null or empty.";
            return false;
        }

        return true;
    }
    private bool CheckRequestForCalcTransfer(CalcTransferParameters_BG parameters, out decimal amount, out int recalc, out string message)
    {
        message = string.Empty;
        amount = 0;
        recalc = 0;

        if (parameters == null)
        {
            message = "request is null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.random))
        {
            message = "request.random is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.userId))
        {
            message = "request.userId is Invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parameters.amount))
        {
            message = "request.amount is Invalid. amount > 0.";
            return false;
        }

        if (!decimal.TryParse(parameters.amount, out amount))
        {
            message = "request.amount is Invalid. this value is not decimal type.";
            return false;
        }
        else
        {
            if (amount < 0)
            {
                message = "request.amount is Invalid. amount < 0.";
                return false;
            }

        }

        if (parameters.moduleId != 2)  //目前只有串接视讯模块(2)
        {
            message = "request.moduleId is Invalid. moduleId is not live casino.";
            return false;
        }

        if (!int.TryParse(parameters.recalc, out recalc))
        {
            message = "request.recalc is Invalid. this value is not int type.";
            return false;
        }

        if (parameters.orders == null || !parameters.orders.Any())
        {
            message = "request.orders is Invalid. orders is null or empty.";
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

    /// <summary>
    /// Cancel Bet用(尚未測試)
    /// </summary>
    /// <param name="partitionTime"></param>
    /// <param name="data"></param>
    /// <param name="existDatas"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private bool IsExistBetData(DateTime partitionTime, BettingDataLC_BG data,
        out IEnumerable<BettingDataLC_BG> existDatas, out decimal cancelAmount, out string message)
    {
        message = string.Empty;
        existDatas = null;
        cancelAmount = 0;
        try
        {
            existDatas = _settledService.IsExistBet_BG(partitionTime, data);
            if (existDatas.Count() != 1)
            {
                //不存在指定有效投注
                message = "not exist valid bet by orderId.";
                _logger.LogError($"IsExistBetData Fail:{message}");
                return false;
            }
            if (existDatas.First().orderAmount != data.orderAmount)
            {
                //退款金額與已存在投注不符
                message = "amount not equal with exist valid bet by orderId.";
                _logger.LogError($"IsExistBetData Fail:{message}");
                return false;
            }
            cancelAmount = data.orderAmount * -1;
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"IsExistBetData Exception:{ex.Message}");
            return false;
        }
    }

    private bool IsExistPayoutData(DateTime partitionTime, BettingDataLC_BG data, out decimal payoutAmount, out string message)
    {
        message = string.Empty;
        payoutAmount = data.calcAmount; //default：注單派彩失敗後，重新派彩，派彩金額=結算金額
        try
        {
            var existDatas = _settledService.IsExistLastPayout_BG(partitionTime, data);

            if (existDatas.Count() == 1) //此狀況為該注單派彩成功，因結果改變，必須重新派彩(扣款-/多派+)
                payoutAmount = data.calcAmount - existDatas.First().calcAmount;

            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"IsExistPayoutData Exception:{ex.Message}");
            return false;
        }
    }


    private bool GetExistData(DateTime partitionTime, string random, IEnumerable<long> orderIds,
        out IEnumerable<BettingDataLC_BG> existDatas, out string message)
    {
        message = string.Empty;
        existDatas = null;
        try
        {
            existDatas = _bettingService.IsExist_BG(partitionTime, random, orderIds);
            return true;
        }
        catch (Exception ex)
        {
            message = "exception happen.";
            _logger.LogError(ex, $"IsExistData Exception:{ex.Message}");
            return false;
        }
    }

    private bool PlaceData(DateTime partitionTime, BettingDataLC_BG data, MySqlTransaction trans, out string message)
    {
        message = string.Empty;
        var result = false;
        try
        {
            switch (data.FromAPIType)
            {
                case BettransLC_BGTypeEnum.OrderTransfer:
                    result = _bettingService.PlaceBet_BG(partitionTime, data, trans);
                    break;
                case BettransLC_BGTypeEnum.CalcTransfer: //Payout, ReSettle
                    result = _settledService.Payout_BG(partitionTime, data, trans);
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

    private bool SetBalance(TpCustInfo tpCustInfo, BettingDataLC_BG data, decimal payoutAmount, decimal rate,
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
                case BettransLC_BGTypeEnum.OrderTransfer:
                    result = _bettingService.SetBalance_BG(tpCustInfo, data, "Bet",
                         out afterAmount, ref bonusId, conn, trans);
                    break;
                case BettransLC_BGTypeEnum.CalcTransfer: //Payout、ReSettle
                    result = _settledService.SetBalance_BG(tpCustInfo, data, payoutAmount,
                             TransType.Payout, data.recalc == 0 ? "Payout" : "ReSettle", out afterAmount, ref bonusId, conn, trans);
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

    private bool InsertUnsettleData(DateTime partitionTime, string orderId, out string message, MySqlTransaction trans)
    {
        message = string.Empty;
        try
        {
            var result = _settledService.InsertUnsettle_BG(partitionTime, orderId, trans);

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

    private BettransType ConvertToBettransType(BGOrderStatusEnum orderStatus)
    {
        switch (orderStatus)
        {
            case BGOrderStatusEnum.NotExist:
            case BGOrderStatusEnum.UnSettle:
            default:
                return BettransType.Reserve;

            case BGOrderStatusEnum.SettledWin:
            case BGOrderStatusEnum.SettledDraw:
            case BGOrderStatusEnum.SettledLose:
                return BettransType.Payout;

            case BGOrderStatusEnum.Cancel:
            case BGOrderStatusEnum.Expired:
            case BGOrderStatusEnum.SystemCancel:
                return BettransType.CancelBet;
        }
    }

    private TicketStatus ConvertToTicketStatus(BGOrderStatusEnum orderStatus)
    {
        switch (orderStatus)
        {
            case BGOrderStatusEnum.NotExist:
            case BGOrderStatusEnum.UnSettle:
            default:
                return TicketStatus.UnSettled;

            case BGOrderStatusEnum.SettledWin:
                return TicketStatus.Win;

            case BGOrderStatusEnum.SettledDraw:
                return TicketStatus.Draw;

            case BGOrderStatusEnum.SettledLose:
                return TicketStatus.Lost;

            case BGOrderStatusEnum.Cancel:
            case BGOrderStatusEnum.Expired:
            case BGOrderStatusEnum.SystemCancel:
                return TicketStatus.Refund;
        }
    }

    private decimal CheckOrderAmount(int gameId, string playId, decimal orderAmount)
    {
        if (gameId == 13 && _bullbullDoublePlayIds.Contains(playId)) //牛牛翻倍 BullBull Double
            return orderAmount * 5;
        return orderAmount;
    }

    #endregion
}
