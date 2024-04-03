using System.Data;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class AllbetBll : IAllbetBll, IThirdPartyBLL
    {
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly TpRequestFailBLL _tpRequestFailBll;
        private readonly MySqlClient _mainDbClient;
        private readonly ILogger<AllbetBll> _logger;
        private readonly Dictionary<AllbetType, Func<AllbetType, string, Task<AllBetTransferResponse>>> _requestHandler;

        public AllbetBll(IServiceProvider provider)
        {
            ThirdParty = Tp.Allbet;
            GameType = GameType.Allbet_LiveCasino;
            _thirdPartyBll = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _tpRequestFailBll = provider.GetRequiredService<TpRequestFailBLL>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _logger = provider.GetService<ILogger<AllbetBll>>();
            _requestHandler = new Dictionary<AllbetType, Func<AllbetType, string, Task<AllBetTransferResponse>>>
            {
                { AllbetType.Transfer, Transfer},
                { AllbetType.CancelTransfer, CancelTransfer},
                { AllbetType.GetBalance, GetBalance},
            };
        }

        public async Task<AllBetTransferResponse> SeamlessWallet(AllbetType transType, string bodyText)
        {
            try
            {
                return await _requestHandler[transType].Invoke(transType, bodyText);
            }
            catch (AllbetException ex)
            {
                return new AllBetTransferResponse(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private async Task<AllBetTransferResponse> Transfer(AllbetType transType, string bodyText)
        {
            AllBetTransferReq type;
            AllBetTransferRequest<AllBetTransferDetails> request = null;
            AllBetTransferRequest<AllbetTransferEventDetail> eventRequest = null;

            try
            {
                type = JsonConvert.DeserializeObject<AllBetTransferReq>(bodyText);
                switch (type.Type)
                {
                    case AllbetTransferType.Bet:
                    case AllbetTransferType.Settle:
                    case AllbetTransferType.ReSettle:
                        request = JsonConvert.DeserializeObject<AllBetTransferRequest<AllBetTransferDetails>>(bodyText);
                        break;

                    case AllbetTransferType.EventSettle:
                        eventRequest = JsonConvert.DeserializeObject<AllBetTransferRequest<AllbetTransferEventDetail>>(bodyText);
                        break;

                    case AllbetTransferType.TransferIn:
                    case AllbetTransferType.TransferOut:
                    default:
                        throw new AllbetException(AllbetResultCode.InvalidParameters, $"Not Support {type.Type.ToString()} type");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ThirdParty}_Transfer, bodyText={bodyText}");
                return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, ex.Message);
            }

            try
            {
                var valid = true;
                switch (type.Type)
                {
                    case AllbetTransferType.Bet:
                        if (request.Details.Sum(betAmt => betAmt.BetAmount + betAmt.Deposit) != Math.Abs(request.Amount))
                        {
                            valid = false;
                        }
                        break;

                    case AllbetTransferType.Settle:
                    case AllbetTransferType.ReSettle:
                        if (request.Details.Count > 1)
                        {
                            valid = false;
                        }

                        var winLossAmount = Convert.ToDecimal(request.Details.Sum(i => i.WinOrLossAmount ?? 0));
                        if (winLossAmount != request.Amount - request.Details[0].BetAmount - request.Details[0].Deposit)
                        {
                            throw new AllbetException(AllbetResultCode.InvalidParameters,
                                $"WinOrLossAmount ({winLossAmount}) not equal Amount ({request.Amount}) less BetAmount/Deposit ({request.Details[0].BetAmount + request.Details[0].Deposit}).");
                        }

                        break;
                    case AllbetTransferType.EventSettle:

                        if (eventRequest.Details.Count > 1)
                        {
                            valid = false;
                        }
                        else
                        {
                            if (eventRequest.Details.Sum(betAmt => betAmt.Amount) != eventRequest.Amount)
                            {
                                valid = false;
                            }
                        }

                        break;
                }

                if (!valid)
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, "Data is not Correct.");
                }

                var tpUserName = request != null ? request.Player : eventRequest.Player;
                var (tpCustInfo, tpCurrency) = GetTPInfo(tpUserName);

                if (tpCurrency.ExchangeRate > 0)
                {
                    if (request != null)
                    {
                        request.Amount *= tpCurrency.ExchangeRate;
                        if (request.Details != null && request.Details.Any())
                        {
                            foreach (var bet in request.Details)
                            {
                                bet.BetAmount *= tpCurrency.ExchangeRate;
                                bet.Deposit *= tpCurrency.ExchangeRate;
                            }
                        }
                    }

                    if (eventRequest != null)
                    {
                        eventRequest.Amount *= tpCurrency.ExchangeRate;
                        if (eventRequest.Details != null && eventRequest.Details.Any())
                        {
                            foreach (var bet in eventRequest.Details)
                            {
                                bet.Amount *= tpCurrency.ExchangeRate;
                            }
                        }
                    }
                }

                var afterAmount = 0m;
                var result = 0;
                switch (type.Type)
                {
                    case AllbetTransferType.Bet:
                        result = Bet(tpCustInfo, request, out afterAmount);
                        break;
                    case AllbetTransferType.Settle:
                        result = Settle(tpCustInfo, request, out afterAmount);
                        break;
                    case AllbetTransferType.ReSettle:
                        result = ReSettle(tpCustInfo, request, out afterAmount);
                        break;
                    case AllbetTransferType.EventSettle:
                        result = EventSettle(tpCustInfo, eventRequest, out afterAmount);
                        break;
                }

                if (result <= 0 && afterAmount < 0)
                {
                    return new AllBetTransferResponse(AllbetResultCode.InsufficientBalance, GetBalance(tpUserName));
                }

                return result switch
                {
                    0 => new AllBetTransferResponse(AllbetResultCode.TranIdNotExists),
                    -1 => new AllBetTransferResponse(AllbetResultCode.InvalidCode),
                    _ => new AllBetTransferResponse(AllbetResultCode.Success, GetBalance(tpUserName))
                };
            }
            catch (AllbetException ex)
            {
                return new AllBetTransferResponse(ex.ReturnCode, ex.message);
            }
            catch (Exception ex)
            {
                switch (type.Type)
                {
                    case AllbetTransferType.Settle:
                        _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.Allbet, TpRequestFailureCategory.Settle, JsonConvert.SerializeObject(request), ex.Message);
                        break;
                    case AllbetTransferType.ReSettle:
                        _tpRequestFailBll.SetRequestFail(RequestFailureType.Tp, Tp.Allbet, TpRequestFailureCategory.ReSettle, JsonConvert.SerializeObject(request), ex.Message);
                        break;
                }

                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private async Task<AllBetTransferResponse> CancelTransfer(AllbetType transType, string bodyText)
        {
            AllBetCancelTransferRequest request;
            try
            {
                request = JsonConvert.DeserializeObject<AllBetCancelTransferRequest>(bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ThirdParty}_Transfer, bodyText={bodyText}");
                return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, ex.Message);
            }

            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(request.Player);

                if (tpCurrency.ExchangeRate > 0)
                {
                    foreach (var bet in request.OriginalDetails)
                    {
                        bet.BetAmount *= tpCurrency.ExchangeRate;
                        bet.Deposit *= tpCurrency.ExchangeRate;
                    }
                }

                var result = Cancel(tpCustInfo, request);
                if (result == -3)
                {
                    await Task.Delay(2000);
                    result = Cancel(tpCustInfo, request);
                }

                return result switch
                {
                    -3 => new AllBetTransferResponse(AllbetResultCode.TranIdNotExists),
                    _ => new AllBetTransferResponse(AllbetResultCode.Success, GetBalance(request.Player))
                };

            }
            catch (AllbetException ex)
            {
                return new AllBetTransferResponse(ex.ReturnCode, ex.message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private async Task<AllBetTransferResponse> GetBalance(AllbetType transType, string username)
        {
            return new AllBetTransferResponse(AllbetResultCode.Success, GetBalance(username));
        }

        private decimal GetBalance(string tpUserName)
        {
            var (tpCustInfo, _) = GetTPInfo(tpUserName);
            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, (int)GameType);
            return tpCustBalance.Balance;
        }

        public (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new AllbetException(AllbetResultCode.UnknownAccount, $"Customer:{tpUserName} not found.");
            }

            var tpCurrency = _thirdPartyBll.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new AllbetException(AllbetResultCode.InvalidParameters, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }

        private int Cancel(TpCustInfo tpCustInfo, AllBetCancelTransferRequest request)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new
            {
                originalTranId = request.OriginalTranId,
                client = request.Player,
                details = JsonConvert.SerializeObject(
                    JsonConvert.DeserializeObject<List<AllbetDbDetail>>(JsonConvert.SerializeObject(request.OriginalDetails)))
            });
            p.Add("hasCanceled", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_CancelBetLC_Allbet_Set_V2", trans, p);

            if (p.Get<int>("hasCanceled") == 0 && result > 0)
            {
                var amount = request.OriginalDetails.Sum(i => i.BetAmount + i.Deposit);
                if (amount != 0)
                {
                    result = _settledService.PayoutV2(tpCustInfo, amount, (int)ThirdParty, $"T{request.OriginalTranId}",
                        (int)GameType, TpCategory.LiveCasino, request.OriginalTranId.ToString(), TransType.Refunds, null,
                        out _, "CancelBet", out _, conn, trans);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            else if (p.Get<int>("hasCanceled") == 1)
            {
                result = -1;
            }
            else
            {
                result = -3;
            }

            return result;
        }

        private int Bet(TpCustInfo tpCustInfo, AllBetTransferRequest<AllBetTransferDetails> request, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            try
            {
                var bonusId = 0;
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = AllbetInsert(AllbetTransferType.Bet, request, trans, out _, out _);

                if (result > 0 && request.Amount != 0)
                {
                    result = _bettingService.PlaceBet(tpCustInfo, Math.Abs(request.Amount) * -1, (int)ThirdParty, $"T{request.TranId}",
                        (int)GameType, request.TranId.ToString(), out afterAmount, ref bonusId, out decimal useBonus, conn, trans);
                    if (bonusId > 0)
                    {
                        var totalUseBonus = useBonus;
                        foreach (var bet in request.Details)
                        {
                            var betUseBonus = totalUseBonus >= bet.BetAmount ? bet.BetAmount : totalUseBonus;
                            _bettingService.AddTpBonus((int)ThirdParty, bet.BetNum.ToString(), (int)GameType, tpCustInfo.CustId,
                                tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, betUseBonus, bonusId, trans, SetTpSeamlessBonusType.Update);
                            totalUseBonus -= betUseBonus;
                        }
                    }
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        public int Settle(TpCustInfo tpCustInfo, AllBetTransferRequest<AllBetTransferDetails> request, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            try
            {

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = AllbetInsert(AllbetTransferType.Settle, request, trans, out _, out _);

                if (result > 0)
                {
                    if (request.Amount != 0)
                    {
                        result = _settledService.PayoutV2(tpCustInfo, request.Amount, (int)ThirdParty,
                            request.Details[0].BetNum.ToString(), (int)GameType, TpCategory.LiveCasino,
                            request.Details[0].BetNum.ToString(), TransType.Payout, null, out afterAmount, "Payout",
                            out _, conn, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.Execute("Service_BettransLC_Allbet_SettleFlag_Set",
                            trans, new { betNum = request.Details[0].BetNum, SettleFlag = 2 });
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }


        public int ReSettle(TpCustInfo tpCustInfo, AllBetTransferRequest<AllBetTransferDetails> request, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                result = AllbetInsert(AllbetTransferType.ReSettle, request, trans, out var stake, out var settleAmount);

                if (result > 0)
                {
                    if (request.Amount != settleAmount)
                    {
                        var backAmount = request.Amount - (settleAmount ?? 0);

                        if (backAmount != 0)
                        {
                            result = _settledService.PayoutV2(tpCustInfo, backAmount, (int)ThirdParty,
                                request.Details[0].BetNum.ToString(), (int)GameType, TpCategory.LiveCasino,
                                request.Details[0].BetNum.ToString(), TransType.Payout, null, out afterAmount,
                                "Resettle", out _, conn, trans, AllowNegative.Allow);
                        }

                        if (result > 0)
                        {
                            _mainDbClient.Execute("Service_BettransLC_Allbet_SettleFlag_Set", trans,
                                new { betNum = request.Details[0].BetNum, SettleFlag = 2 });
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
                    else
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return result;
        }

        private int AllbetInsert(AllbetTransferType type, AllBetTransferRequest<AllBetTransferDetails> request,
            IDbTransaction trans, out decimal stake, out decimal? winLostAmount)
        {
            stake = 0m;
            winLostAmount = null;
            var p = new DynamicParameters(new
            {
                tranId = request.TranId,
                transferType = (int)request.Type,
                client = request.Player,
                currency = request.Currency,
                amount = request.Amount,
                details = JsonConvert.SerializeObject(
                    JsonConvert.DeserializeObject<List<AllbetDbDetail>>(JsonConvert.SerializeObject(request.Details)))
            });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            var result = _mainDbClient.Execute("Service_BettransLC_Allbet_Ins_V2", trans, p);

            if (type == AllbetTransferType.Settle || type == AllbetTransferType.ReSettle)
            {
                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }

                if (result > 0)
                {
                    stake = p.Get<decimal>("Stake");
                    winLostAmount = p.Get<decimal?>("SettleAmount");
                }
            }

            return result;
        }

        private int EventSettle(TpCustInfo tpCustInfo, AllBetTransferRequest<AllbetTransferEventDetail> request, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            var eventRecordNum = request.Details[0].EventRecordNum.ToString();
            var p = new DynamicParameters(new
            {
                tranId = request.TranId,
                transferType = request.Type,
                client = request.Player,
                currency = request.Currency,
                amount = request.Amount,
                eventRecordNum,
                details = JsonConvert.SerializeObject(request.Details),
                SettleFlag = SettleFlag.Settled
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_RewardLC_Allbet_Ins", trans, p);
                if (result > 0)
                {
                    if (request.Amount != 0)
                    {
                        result = _settledService.PayoutV2(
                            tpCustInfo,
                            request.Amount,
                            (int)ThirdParty,
                            eventRecordNum,
                            (int)GameType,
                            TpCategory.LiveCasino,
                            eventRecordNum,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            $"{eventRecordNum} Prize",
                            out _,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    result = -1;
                }
                else
                {
                    throw;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
    }
}