using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class SBSWBLL : ISBSWBLL, IThirdPartyBLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<SBSWBLL> _logger;
        public readonly ThirdPartySetting _thirdPartySetting;
        private readonly Dictionary<SBSWTransType, Func<string, string, Task<ReturnObj>>> _requestHandler;


        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public SBSWBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.SBSW;
            LogContext.PushProperty("TP", ThirdParty.ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);
            _requestHandler = new Dictionary<SBSWTransType, Func<string, string, Task<ReturnObj>>>()
            {
                { SBSWTransType.GetBalance, GetBalance},
                { SBSWTransType.PlaceBet, PlaceBet},
                { SBSWTransType.ConfirmBet, ConfirmBet},
                { SBSWTransType.CancelBet, CancelBet},
                { SBSWTransType.Settle, Settle},
                { SBSWTransType.Resettle, Resettle},
                { SBSWTransType.Unsettle, Unsettle},
                { SBSWTransType.PlaceBetParlay, PlaceBetParlay},
                { SBSWTransType.ConfirmBetParlay, ConfirmBetParlay},
                { SBSWTransType.PlaceBet3rd, PlaceBet3rd},
                { SBSWTransType.ConfirmBet3rd, ConfirmBet3rd},
                { SBSWTransType.HealthCheck, HealthCheck},
            };
            _logger = provider.GetRequiredService<ILogger<SBSWBLL>>();
        }

        public async Task<ReturnObj> SeamlessWallet(SBSWTransType transType, string merchant, string bodyText)
        {
            try
            {
                _logger.LogInformation($"{transType}:{bodyText}");
                return await _requestHandler[transType].Invoke(merchant, bodyText);
            }
            catch (SBSWException ex)
            {
                return new ReturnObj(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError);
            }
        }

        private async Task<ReturnObj> GetBalance(string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<GetBalance>>(bodyText);
            CheckKey(merchant, request.key);

            return new BalanceObj(ReturnCode.Success)
            {
                balance = GetTpCustBalance(request.message.userId).Balance,
                balanceTs = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-ddTHH:mm:ss.fffzz:00"),
                userId = request.message.userId,
            };
        }

        private async Task<ReturnObj> PlaceBet(string merchant, string bodyText)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseReq<PlaceBet>>(bodyText);
                CheckKey(merchant, request.key);

                var placeBet = request.message;
                var (tpCustInfo, tpCurrency) = GetTPInfo(placeBet.userId);
                CheckTpUser(tpCustInfo.TpUserName);

                placeBet.betAmount *= tpCurrency.ExchangeRate;
                placeBet.actualAmount *= tpCurrency.ExchangeRate;
                placeBet.creditAmount *= tpCurrency.ExchangeRate;
                placeBet.debitAmount *= tpCurrency.ExchangeRate;

                var (gameType, tpCategory) = GetGameType(placeBet.sportType);

                var p = new DynamicParameters(placeBet);
                p.AddDynamicParams(new { TicketType = (int)SBSWTransType.PlaceBet });

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var sq = await _mainDbClient.QueryFirstAsync<long>("Service_Bettrans_SBSW_PlaceBet", trans, p);
                if (sq < 1)
                {
                    throw new SBSWException(ReturnCode.InvalidParameters);
                }

                var bonusId = 0;
                var result = _bettingService.PlaceBetV2(
                    tpCustInfo,
                    Math.Abs(placeBet.betAmount) * -1,
                    (int)ThirdParty,
                    placeBet.refId,
                    (int)gameType,
                    placeBet.refId,
                    null,
                    tpCategory,
                    out var afterAmount,
                    ref bonusId,
                    conn,
                    trans);

                if (result < 1 || afterAmount < 0)
                {
                    throw new SBSWException(ReturnCode.InsufficientBalance);
                }

                trans.Commit();
                return new PlaceBetObj(ReturnCode.Success)
                {
                    licenseeTxId = sq.ToString(),
                    refId = placeBet.refId,
                };
            }
            catch (SBSWException ex)
            {
                return new ReturnObj(ex);
            }
            catch (MySqlException mySqlEx)
            {
                return mySqlEx.Number == 1062 ? new ReturnObj(ReturnCode.TransactionDuplicated) :
                    new ReturnObj(ReturnCode.DatabaseError, mySqlEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError, ex.Message);
            }
        }

        private async Task<ReturnObj> ConfirmBet(string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<ConfirmBet>>(bodyText);
            CheckKey(merchant, request.key);

            //使用非同步逐筆更新下注結果
            var Tasks = request.message.txns.Select(txn => Task.Run(() => ExecuteConfirmBet(request.message, txn))).ToArray();
            var resultList = await Task.WhenAll(Tasks);
            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new ConfirmBetObj(ReturnCode.Success)
            {
                balance = GetTpCustBalance(request.message.userId).Balance
            };
        }

        private async Task<ExecuteResult> ExecuteConfirmBet(ConfirmBet confirmBet, Txn txn)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(confirmBet.userId);
                txn.actualAmount *= tpCurrency.ExchangeRate;
                txn.debitAmount *= tpCurrency.ExchangeRate;
                txn.creditAmount *= tpCurrency.ExchangeRate;

                var p = new DynamicParameters(txn);
                p.AddDynamicParams(new
                {
                    confirmBet.action,
                    confirmBet.operationId,
                    confirmBet.userId,
                    confirmBet.updateTime,
                    confirmBet.transactionTime,
                    TicketType = (int)SBSWTransType.ConfirmBet,
                });

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_ConfirmBet", trans, p);
                CheckExecuteResult(result.SPResult);
                
                //處理ConfirmBet的差異金額
                var amount = txn.creditAmount - txn.debitAmount;
                if (txn.isOddsChanged && amount != 0)
                {
                    var (gameType, tpCategory) = GetGameType(result.SportType);
                    result.SetBalanceResult = _settledService.PayoutV2(
                                tpCustInfo: tpCustInfo,
                                amount: amount,
                                tpId: (int)ThirdParty,
                                tpTransId: txn.refId,
                                gameType: (int)gameType,
                                tpCategory: tpCategory,
                                refId: txn.refId,
                                TransType.Refunds,
                                betData: null,
                                out decimal afterAmount,
                                "Refund",
                                out int bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow);

                    if (result.SetBalanceResult < 1)
                    {
                        return new ExecuteResult(ReturnCode.InvalidAmount);
                    }
                }

                trans.Commit();
                return result;
            }
            catch (SBSWException ex)
            {
                return new ExecuteResult(ex.ReturnCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ExecuteResult(ReturnCode.UnknownError, ex.Message);
            }
        }

        private async Task<ReturnObj> CancelBet(string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<ConfirmBet>>(bodyText);
            CheckKey(merchant, request.key);

            //使用非同步逐筆更新下注結果
            var Tasks = request.message.txns.Select(txn => Task.Run(() => ExecuteCancelBet(request.message, txn))).ToArray();
            var resultList = await Task.WhenAll(Tasks);
            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new ConfirmBetObj(ReturnCode.Success)
            {
                balance = GetTpCustBalance(request.message.userId).Balance
            };
        }

        private async Task<ExecuteResult> ExecuteCancelBet(ConfirmBet confirmBet, Txn txn)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(confirmBet.userId);

                txn.debitAmount *= tpCurrency.ExchangeRate;
                txn.creditAmount *= tpCurrency.ExchangeRate;

                var p = new DynamicParameters(new
                {
                    confirmBet.action,
                    confirmBet.operationId,
                    confirmBet.userId,
                    confirmBet.updateTime,
                    txn.refId,
                    txn.creditAmount,
                    txn.debitAmount,
                    TicketType = (int)SBSWTransType.CancelBet,
                });

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_CancelBet", trans, p);
                CheckExecuteResult(result.SPResult);

                var amount = txn.creditAmount - txn.debitAmount;
                var (gameType, tpCategory) = GetGameType(result.SportType);
                result.SetBalanceResult = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: amount,
                            tpId: (int)ThirdParty,
                            tpTransId: txn.refId,
                            gameType: (int)gameType,
                            tpCategory: tpCategory,
                            refId: txn.refId,
                            TransType.Refunds,
                            betData: null,
                            out decimal afterAmount,
                            "Cancel",
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow);

                if (result.SetBalanceResult < 1)
                {
                    return new ExecuteResult(ReturnCode.InvalidAmount);
                }

                trans.Commit();
                return result;
            }
            catch (SBSWException ex)
            {
                return new ExecuteResult(ex.ReturnCode, ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ExecuteResult(ReturnCode.UnknownError, ex.Message);
            }
        }

        private async Task<ReturnObj> Settle(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> Resettle(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> Unsettle(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> PlaceBetParlay(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> ConfirmBetParlay(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> PlaceBet3rd(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> ConfirmBet3rd(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> HealthCheck(string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new SBSWException(ReturnCode.UserNotFound, $"Customer:{tpUserName} not found.");
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new SBSWException(ReturnCode.UnSupportedCurrency, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }

        private void CheckTpUser(string tpUserName)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
            if (tpCheck == null)
            {
                throw new SBSWException(ReturnCode.InvalidToken, $"{tpUserName} is logout.");
            }
            if (tpCheck.Locked)
            {
                throw new SBSWException(ReturnCode.UserLocked, $"{tpUserName} is locked.");
            }
        }

        private void CheckKey(string merchant, string key)
        {
            var config = new Config(_thirdPartySetting.TpSetting, merchant);
            if (config.VendorId != key)
            {
                throw new SBSWException(ReturnCode.InvalidVendorId, $"Invalid key | key is:{key}");
            }
        }

        private (GameType gameType, TpCategory tpCategory) GetGameType(int sportType)
        {
            if (sportType > 0 && sportType < 100)
            {
                return (GameType.SBSW_Sports, TpCategory.Sports);
            }

            switch (sportType)
            {
                case 161: //百练赛
                case 164: //快乐5
                case 168: //快乐5系列游戏
                    return (GameType.SBSW_NumberGame, TpCategory.Lottery);
                case 180:
                case 181:
                case 182:
                case 183:
                case 184:
                case 185:
                case 186:
                    return (GameType.SBSW_VirtualSports, TpCategory.VirtualSports);
                case 190:
                case 191:
                case 192:
                case 193:
                case 194:
                case 195:
                case 196:
                case 197:
                case 199:
                    return (GameType.SBSW_VirtualSports2, TpCategory.VirtualSports);
                case 202: //快乐彩
                    return (GameType.SBSW_Keno, TpCategory.Lottery);
                case 220: //彩票(Keno Lottery)
                    return (GameType.SBSW_Lottery, TpCategory.Lottery);
                case 222: //桌面游戏
                    return (GameType.SBSW_TableGame, TpCategory.Lottery);
                default:
                    throw new SBSWException(ReturnCode.InvalidParameters, $"sportType:{sportType} not supported.");
            }
        }

        private TpCustBalance GetTpCustBalance(string tpUserName)
        {
            var (tpCustInfo, _) = GetTPInfo(tpUserName);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return tpCustBalance;
        }

        private void CheckExecuteResult(int SPResult)
        {
            if (SPResult == -1)
            {
                throw new SBSWException(ReturnCode.TransactionNotFound);
            }

            if (SPResult == -2)
            {
                throw new SBSWException(ReturnCode.TransactionDuplicated);
            }

            if (SPResult == -3)
            {
                throw new SBSWException(ReturnCode.InvalidTransactionStatus);
            }
        }
    }
}
