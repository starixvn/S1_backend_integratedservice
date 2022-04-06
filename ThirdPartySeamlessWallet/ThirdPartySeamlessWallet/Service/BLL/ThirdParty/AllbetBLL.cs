using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    public class AllbetBll: IAllbetBll, IThirdPartyBLL
    {
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
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
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _logger = provider.GetService<ILogger<AllbetBll>>();
            _requestHandler = new Dictionary<AllbetType, Func<AllbetType, string, Task<AllBetTransferResponse>>>
            {
                { AllbetType.Transfer, Transfer},
                // { AllbetType.CancelTransfer, CancelTransfer},
                // { AllbetType.GetBalance, GetBalance},
            };
        }
        
        public async Task<AllBetTransferResponse> SeamlessWallet(AllbetType transType, string bodyText)
        {
            try
            {
                _logger.LogInformation($"{transType} || {bodyText}");
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
            AllBetTransferReq type = null;
            AllBetTransferRequest<AllBetTransferDetails> request = null;
            AllBetTransferRequest<AllbetTransferEventDetail> eventRequest = null;
            AllBetTransferRequest<AllbetTransferEventDetail> transferRequest = null;

            try
            {
                type = JsonConvert.DeserializeObject<AllBetTransferReq>(bodyText);
                switch (type.type)
                {
                    case AllbetTransferType.Bet:
                    case AllbetTransferType.Settle:
                    case AllbetTransferType.ReSettle:
                        request = JsonConvert.DeserializeObject<AllBetTransferRequest<AllBetTransferDetails>>(bodyText);
                        break;

                    case AllbetTransferType.EventSettle:
                        eventRequest =
                            JsonConvert.DeserializeObject<AllBetTransferRequest<AllbetTransferEventDetail>>(bodyText);
                        break;

                    // case AllbetTransferType.TransferIn:
                    // case AllbetTransferType.TransferOut:
                    //     transferRequest = JsonConvert.DeserializeObject<AllBetTransferRequest<null>>(bodyText);
                    //     break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{ThirdParty}_Transfer, bodyText={bodyText}");
                return new AllBetTransferResponse(AllbetResultCode.InvalidParameters);
            }

            try
            {
                var valid = true;
                switch (type.type)
                {
                    case AllbetTransferType.Bet:
                        decimal amountSum = 0;

                        if (request.amount <= 0)
                        {
                            valid = false;
                        }
                        else
                        {
                            foreach (var betAmt in request.details)
                            {
                                if (betAmt.betAmount <= 0)
                                {
                                    valid = false;
                                    break;
                                }

                                amountSum += betAmt.betAmount;
                            }

                            if (amountSum != request.amount)
                            {
                                valid = false;
                            }
                        }

                        break;

                    case AllbetTransferType.Settle:
                    case AllbetTransferType.ReSettle:
                        if (request.details.Count > 1)
                        {
                            valid = false;
                        }
                        else
                        {
                            if (request.details.Sum(betAmt => betAmt.betAmount) != request.amount)
                            {
                                valid = false;
                            }
                        }

                        break;
                    case AllbetTransferType.EventSettle:

                        if (eventRequest.details.Count > 1)
                        {
                            valid = false;
                        }
                        else
                        {
                            if (eventRequest.details.Sum(betAmt => betAmt.amount) != eventRequest.amount)
                            {
                                valid = false;
                            }
                        }

                        break;
                }

                if (!valid)
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters);
                }

                var tpUserName = request != null ? request.player :
                    eventRequest != null ? eventRequest.player : transferRequest.player;
                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
                var tpCurrency = _thirdPartyBll.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

                if (tpCurrency.ExchangeRate > 0)
                {
                    if (request != null)
                    {
                        request.amount *= tpCurrency.ExchangeRate;
                        if (request.details != null && request.details.Any())
                        {
                            foreach (var bet in request.details)
                            {
                                bet.betAmount *= tpCurrency.ExchangeRate;
                            }
                        }
                    }

                    if (eventRequest != null)
                    {
                        eventRequest.amount *= tpCurrency.ExchangeRate;
                        if (eventRequest.details != null && eventRequest.details.Any())
                        {
                            foreach (var bet in eventRequest.details)
                            {
                                bet.amount *= tpCurrency.ExchangeRate;
                            }
                        }
                    }
                }


                var afterAmount = 0m;
                var result = 0;
                switch (type.type)
                {
                    case AllbetTransferType.Bet:
                        result = Bet(tpCustInfo, request, out afterAmount);
                        break;
                    // case AllbetTransferType.Cancel:
                    //     break;
                    case AllbetTransferType.Settle:
                        result = Settle(tpCustInfo, request, out afterAmount);
                        break;
                    case AllbetTransferType.ReSettle:
                        result = ReSettle(tpCustInfo, request, out afterAmount);
                        if (result == -2)
                        {
                            return new AllBetTransferResponse(AllbetResultCode.Success, GetBalance(tpUserName));
                        }
                        break;
                    // case AllbetTransferType.TransferIn:
                    //     break;
                    // case AllbetTransferType.TransferOut:
                    //     break;
                    case AllbetTransferType.EventSettle:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
                {
                    afterAmount /= tpCurrency.ExchangeRate;

                    if (request != null)
                    {
                        request.amount /= tpCurrency.ExchangeRate;
                        foreach (var bet in request.details)
                        {
                            bet.betAmount /= tpCurrency.ExchangeRate;
                        }
                    }

                    if (eventRequest != null)
                    {
                        eventRequest.amount /= tpCurrency.ExchangeRate;
                        foreach (var bet in eventRequest.details)
                        {
                            bet.amount /= tpCurrency.ExchangeRate;
                        }
                    }
                }

                if (result <= 0 && afterAmount < 0)
                {
                    return new AllBetTransferResponse(AllbetResultCode.InsufficientBalance, GetBalance(tpUserName));
                }

                if (((type.type == AllbetTransferType.Settle || type.type == AllbetTransferType.ReSettle) && request.amount <= 0) ||
                    (type.type == AllbetTransferType.EventSettle && eventRequest.amount == 0))
                {
                    afterAmount = GetBalance(tpUserName);
                }

                return result switch
                {
                    0 => new AllBetTransferResponse(AllbetResultCode.TranIdNotExists),
                    -1 => new AllBetTransferResponse(AllbetResultCode.InvalidCode),
                    _ => new AllBetTransferResponse(AllbetResultCode.Success, afterAmount)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private decimal GetBalance(string tpUserName)
        {
            return _tpCustInfoBll.GetBalance((int)ThirdParty, tpUserName, (int)GameType).Balance;
        }

        public int Bet(TpCustInfo tpCustInfo, AllBetTransferRequest<AllBetTransferDetails> request, out decimal afterAmount)
        {
            var bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                request.tranId, 
                transferType = (int)request.type,
                client = request.player,
                request.currency,
                request.amount,
                details = JsonConvert.SerializeObject(request.details)
            });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_Allbet_Ins", trans, p);

                if (result > 0 && request.amount != 0)
                {
                    result = _bettingService.PlaceBetV2(tpCustInfo, Math.Abs(request.amount) * -1, (int)ThirdParty, $"T{request.tranId}",
                        (int)GameType, request.tranId.ToString(), null, TpCategory.LiveCasino, out afterAmount, ref bonusId,
                        conn, trans);
                    if (bonusId > 0)
                    {
                        foreach (var bet in request.details)
                        {
                            _bettingService.AddTpBonus((int)ThirdParty, bet.betNum.ToString(), (int)GameType, tpCustInfo.CustId,
                                tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, bet.betAmount, bonusId,
                                TpCategory.LiveCasino, conn, trans);
                        }
                    }
                }

                if (result > 0)
                {
                    trans.Commit();
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
            var bonusId = 0;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                request.tranId, 
                transferType = (int)request.type, 
                client = request.player,
                request.currency,
                request.amount,
                details = JsonConvert.SerializeObject(request.details)
            });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
        
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_Allbet_Ins", trans, p);
        
                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }
        
                if (result > 0)
                {
                    var stake = p.Get<decimal>("Stake");
                    var payoutAmount = request.amount + stake;
                    if (payoutAmount != 0)
                    {
                        result = _settledService.PayoutV2(tpCustInfo, payoutAmount, (int)ThirdParty,
                            request.details[0].betNum.ToString(), (int)GameType, TpCategory.LiveCasino,
                            request.details[0].betNum.ToString(), TransType.Payout, null, out afterAmount, "Payout",
                            out bonusId, conn, trans);
                    }
        
                    if (result > 0)
                    {
                        _mainDbClient.Execute("Service_BettransLC_Allbet_SettleFlag_Set",
                            trans, new { request.details[0].betNum, SettleFlag = 2 });
                        trans.Commit();
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
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                request.tranId, 
                transferType = (int)request.type, 
                client = request.player,
                request.currency,
                request.amount,
                details = JsonConvert.SerializeObject(request.details)
            });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_Allbet_Ins", trans, p);

                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }

                if (result > 0)
                {
                    var stake = p.Get<decimal>("Stake");
                    var winLostAmount = p.Get<decimal?>("WinLostAmount");

                    if (winLostAmount == null || request.amount != winLostAmount)
                    {
                        decimal backAmount = 0;
                        if (winLostAmount == null)
                        {
                            backAmount = request.amount + stake;
                        }
                        else
                        {
                            backAmount = request.amount - (winLostAmount ?? 0);
                        }

                        if (backAmount != 0)
                        {
                            result = _settledService.PayoutV2(tpCustInfo, backAmount, (int)ThirdParty,
                                request.details[0].betNum.ToString(), (int)GameType, TpCategory.LiveCasino,
                                request.details[0].betNum.ToString(), TransType.Payout, null, out afterAmount,
                                "Resettle", out _, conn, trans);
                        }

                        if (result > 0)
                        {
                            _mainDbClient.Execute("Service_BettransLC_Allbet_SettleFlag_Set", trans,
                                new { request.details[0].betNum, SettleFlag = 2 });
                            trans.Commit();
                        }
                    }
                    else
                    {
                        trans.Commit();
                        result = -2;
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
        
        public int EventSettle(TpCustInfo tpCustInfo, AllBetTransferRequest<AllbetTransferEventDetail> request, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            var eventRecordNum = request.details[0].eventRecordNum.ToString();
            var p = new DynamicParameters(new
            {
                request.tranId,
                transferType = request.type,
                client = request.player,
                request.currency,
                request.amount,
                eventRecordNum,
                details = JsonConvert.SerializeObject(request.details),
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
                    if (request.amount != 0)
                    {
                        result = _settledService.PayoutV2(
                            tpCustInfo,
                            request.amount,
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
                        trans.Commit();
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