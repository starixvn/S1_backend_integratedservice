using System.Data;
using System.Diagnostics;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.Options;
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
        private readonly WalletService _walletService;
        private readonly ILogger<SBSWBLL> _logger;
        private readonly Dictionary<SBSWTransType, Func<SBSWTransType, string, string, Task<ReturnObj>>> _requestHandler;
        private readonly Dictionary<int, SabaSportTypeOption> _sportTypeConfig;
        private readonly Dictionary<string, SabaConfig> _siteSabaConfigs;
        private readonly SBSWPayloadLogService _payloadLogService;

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
            _walletService = provider.GetRequiredService<WalletService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _requestHandler = new Dictionary<SBSWTransType, Func<SBSWTransType, string, string, Task<ReturnObj>>>()
            {
                { SBSWTransType.GetBalance, GetBalance},
                { SBSWTransType.PlaceBet, PlaceBet},
                { SBSWTransType.PlaceBetParlay, PlaceBetParlay},
                { SBSWTransType.PlaceBet3rd, PlaceBet3rd},
                { SBSWTransType.ConfirmBet, ConfirmBet},
                { SBSWTransType.ConfirmBetParlay, ConfirmBet},
                { SBSWTransType.ConfirmBet3rd, ConfirmBet},
                { SBSWTransType.CancelBet, CancelBet},
                { SBSWTransType.Settle, Settle},
                { SBSWTransType.Resettle, Settle},
                { SBSWTransType.Unsettle, Settle},
                { SBSWTransType.HealthCheck, HealthCheck},
                { SBSWTransType.PlaceBetEnt, PlaceBetEnt},
                { SBSWTransType.SettleEnt, SettleEnt},
                { SBSWTransType.CancelBetEnt, CancelBetEnt},
                { SBSWTransType.GetTicketInfo, GetTicketInfo},
                { SBSWTransType.AdjustBalance, AdjustBalance},
            };
            _logger = provider.GetRequiredService<ILogger<SBSWBLL>>();
            _sportTypeConfig = provider.GetRequiredService<IEnumerable<SabaSportTypeOption>>()
                .ToDictionary(item => item.SportType, item => item);

            _siteSabaConfigs = provider.GetRequiredService<ConfigBLL>()
                .GetConfigValuesBySiteId<SabaConfig>(ThirdParty.ToString())
                .Where(item => item.Key != 0 && item.Value.OperatorId != null)
                .ToDictionary(item => item.Value.OperatorId, item => item.Value);
            _payloadLogService = provider.GetRequiredService<SBSWPayloadLogService>();
        }

        public async Task<ReturnObj> SeamlessWallet(string merchant, SBSWTransType transType, string bodyText)
        {
            var timmer = new Stopwatch();
            timmer.Start();
            ReturnObj response = default;
            try
            {
                LogContext.PushProperty("Merchant", merchant);
                LogContext.PushProperty("Action", transType.ToString());
                _logger.LogInformation(bodyText);

                response= await _requestHandler[transType].Invoke(transType, merchant, bodyText);
                return response;
            }
            catch (SBSWException ex)
            {
                response= new ReturnObj(ex);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                response = new ReturnObj(ReturnCode.UnknownError);
                return response;
            }
            finally
            {
                timmer.Stop();
                var logMeta = new PayloadLogMeta()
                {
                    Action = transType.ToString()
                };
                var seq = Guid.NewGuid().ToString();
                _payloadLogService.LogDetailedPayload(seq, bodyText, logMeta);
                _payloadLogService.UpdateDetailedPayLoadWithResponse(seq, response, timmer.Elapsed.TotalMilliseconds, logMeta);
                _logger.LogInformation("[SBSW] Action: {Action}, Request Payload: {Payload}, Response: {Response}",
                    logMeta.Action, bodyText, JsonConvert.SerializeObject(response));
            }
        }

        private async Task<ReturnObj> GetBalance(SBSWTransType transType, string merchant, string bodyText)
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

        private async Task<ReturnObj> PlaceBet(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<Bet>>(bodyText);
            CheckKey(merchant, request.key);
            var result = await ExecuteBet(transType, request.message);
            if (result.ReturnCode != ReturnCode.Success)
            {
                return new ReturnObj(result);
            }

            return new PlaceBetObj(ReturnCode.Success)
            {
                licenseeTxId = result.licenseeTxId.ToString(),
                refId = result.refId,
            };
        }

        private async Task<ReturnObj> PlaceBetParlay(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<PlaceBetParlay>>(bodyText);
            CheckKey(merchant, request.key);
            CheckBalance(request.message.userId, request.message.txns.Sum(x => x.debitAmount));
            var resultList = new List<ExecuteResult>();
            foreach (var ticket in request.message.txns)
            {
                resultList.Add(await ExecuteBet(transType, new Bet(request.message, ticket)));
            }

            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new PlaceBetParlayObj(ReturnCode.Success)
            {
                txns = resultList.Select(i => new BetObj()
                {
                    refId = i.refId,
                    licenseeTxId = i.licenseeTxId.ToString(),
                }).ToList()
            };
        }

        private async Task<ReturnObj> PlaceBet3rd(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<PlaceBet3rd>>(bodyText);
            CheckKey(merchant, request.key);
            CheckBalance(request.message.userId, request.message.ticketList.Sum(x => x.debitAmount));
            var resultList = new List<ExecuteResult>();
            foreach (var ticket in request.message.ticketList)
            {
                resultList.Add(await ExecuteBet(transType, new Bet(request.message, ticket)));
            }

            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new PlaceBet3rdObj(ReturnCode.Success)
            {
                balance = GetTpCustBalance(request.message.userId).Balance,
                txns = resultList.Select(i => new BetObj()
                {
                    refId = i.refId,
                    licenseeTxId = i.licenseeTxId.ToString(),
                }).ToList()
            };
        }

        private async Task<ReturnObj> PlaceBetEnt(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<PlaceBetEnt>>(bodyText);
            CheckKey(merchant, request.key);
            CheckBalance(request.message.userId, request.message.debitAmount);
            var resultList = new List<ExecuteResult>();
            foreach (var ticket in request.message.ticketList)
            {
                resultList.Add(await ExecuteBet(transType, new Bet(request.message, ticket)));
            }
            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new PlaceBetEntObj(ReturnCode.Success)
            {
                userId = request.message.userId,
                balance = GetTpCustBalance(request.message.userId).Balance,
                ticketList = resultList.Select(i => new BetObj()
                {
                    refId = i.refId,
                    licenseeTxId = i.licenseeTxId.ToString(),
                }).ToList()
            };
        }

        private async Task<ExecuteResult> ExecuteBet(SBSWTransType transType, Bet bet)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(bet.userId);
                CheckTpUser(tpCustInfo.TpUserName);

                bet.betAmount *= tpCurrency.ExchangeRate;
                bet.actualAmount *= tpCurrency.ExchangeRate;
                bet.debitAmount *= tpCurrency.ExchangeRate;
                bet.creditAmount *= tpCurrency.ExchangeRate;

                var (gameType, tpCategory) = GetGameType(bet.sportType);

                var p = new DynamicParameters(bet);
                p.AddDynamicParams(new { TicketType = (int)transType });

                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_Bet", trans, p);
                if (result.SPResult == -4)
                {
                    throw new SBSWException(ReturnCode.TransactionDuplicated, $"refId: {bet.refId} Duplicated.");
                }

                if (result.licenseeTxId < 1)
                {
                    throw new SBSWException(ReturnCode.InvalidParameters);
                }

                var bonusId = 0;
                var afterAmount = 0m;

                if (bet.sportType == 157) // 主播打賞
                {
                    result.SetBalanceResult = _walletService.SetBalance(
                           tpCustInfo.CustId,
                           tpCustInfo.SiteId,
                           tpCustInfo.UserName,
                           Math.Abs(bet.debitAmount) * -1,
                           (int)ThirdParty,
                           TransType.Tip,
                           $"Tip: {Math.Abs(bet.debitAmount):#,##0.##}",
                           bet.refId,
                           out afterAmount,
                           conn,
                           trans,
                           AllowNegative.NotAllow
                       );
                }
                else
                {
                    result.SetBalanceResult = _bettingService.PlaceBet(
                        tpCustInfo,
                        Math.Abs(bet.debitAmount) * -1,
                        (int)ThirdParty,
                        bet.refId,
                        (int)gameType,
                        bet.refId,
                        out afterAmount,
                        ref bonusId,
                        out _,
                        conn,
                        trans);
                }


                if (result.SetBalanceResult < 1 || afterAmount < 0)
                {
                    throw new SBSWException(ReturnCode.InsufficientBalance);
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return result;
            }
            catch (SBSWException ex)
            {
                return new ExecuteResult(ex.ReturnCode, ex.Message);
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    return new ExecuteResult(ReturnCode.TransactionDuplicated, $"refId: {bet.refId} Duplicated.");
                }
                _logger.LogError(mySqlEx, mySqlEx.Message);
                return new ExecuteResult(ReturnCode.DatabaseError, mySqlEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ExecuteResult(ReturnCode.UnknownError, ex.Message);
            }
        }

        private async Task<ReturnObj> ConfirmBet(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<ConfirmBet>>(bodyText);
            CheckKey(merchant, request.key);

            var resultList = new List<ExecuteResult>();
            foreach (var txn in request.message.txns)
            {
                resultList.Add(await ExecuteConfirmBet(transType, new Confirm(request.message, txn)));
            }

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

        private async Task<ExecuteResult> ExecuteConfirmBet(SBSWTransType transType, Confirm confirm)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(confirm.userId);
                confirm.actualAmount *= tpCurrency.ExchangeRate;
                confirm.debitAmount *= tpCurrency.ExchangeRate;
                confirm.creditAmount *= tpCurrency.ExchangeRate;

                var p = new DynamicParameters(confirm);
                p.AddDynamicParams(new { TicketType = (int)transType });

                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();
                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_ConfirmBet", trans, p);

                if (result.SPResult == -1)
                {
                    throw new SBSWException(ReturnCode.TransactionNotFound, $"licenseeTxId:{confirm.licenseeTxId} refId:{confirm.refId} Not Found");
                }

                if (result.SPResult == -2)
                {
                    throw new SBSWException(ReturnCode.Success);//已完成視為成功
                }

                if (result.SPResult == -3)
                {
                    throw new SBSWException(ReturnCode.InvalidTransactionStatus);
                }

                //處理ConfirmBet的差異金額
                var amount = confirm.creditAmount - confirm.debitAmount;
                if (confirm.isOddsChanged || amount != 0)
                {
                    var (gameType, tpCategory) = GetGameType(result.SportType);
                    result.SetBalanceResult = _settledService.PayoutV2(
                                tpCustInfo: tpCustInfo,
                                amount: amount,
                                tpId: (int)ThirdParty,
                                tpTransId: confirm.refId,
                                gameType: (int)gameType,
                                tpCategory: tpCategory,
                                refId: confirm.refId,
                                TransType.Refunds,
                                betData: null,
                                out decimal afterAmount,
                                "Refund",
                                out int bonusId,
                                conn,
                                trans,
                                AllowNegative.NotAllow);

                    if (result.SetBalanceResult < 1)
                    {
                        return new ExecuteResult(ReturnCode.InvalidAmount);
                    }
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
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

        private async Task<ReturnObj> CancelBet(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<ConfirmBet>>(bodyText);
            CheckKey(merchant, request.key);
            var resultList = new List<ExecuteResult>();
            foreach (var txn in request.message.txns)
            {
                resultList.Add(await ExecuteCancelBet(request.message, txn, false));
            }
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

        private async Task<ReturnObj> CancelBetEnt(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<SettleEnt>>(bodyText);
            CheckKey(merchant, request.key);
            var now = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-4, 0, 0)).DateTime;
            var result = await ExecuteCancelBet(
                confirmBet: new ConfirmBet()
                {
                    action = request.message.action,
                    operationId = DateTime.UtcNow.Ticks.ToString(),
                    userId = request.message.userId,
                    updateTime = now,
                    transactionTime = now
                },
                detail: new ConfirmDetail()
                {
                    refId = request.message.refId,
                    txId = request.message.txIds?.FirstOrDefault() ?? 0,
                    winlostDate = request.message.winLostDate,
                    creditAmount = request.message.creditAmount,
                    debitAmount = request.message.debitAmount,
                }, true);
            return new ReturnObj(result);
        }

        private async Task<ExecuteResult> ExecuteCancelBet(ConfirmBet confirmBet, ConfirmDetail detail, bool hasTxId)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(confirmBet.userId);

                detail.debitAmount *= tpCurrency.ExchangeRate;
                detail.creditAmount *= tpCurrency.ExchangeRate;

                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();
                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>(hasTxId ? "Service_Bettrans_SBSW_CancelBetEnt" : "Service_Bettrans_SBSW_CancelBet", trans, hasTxId ? new
                {
                    confirmBet.action,
                    confirmBet.operationId,
                    confirmBet.userId,
                    confirmBet.updateTime,
                    detail.refId,
                    detail.creditAmount,
                    detail.debitAmount,
                    detail.txId,
                    TicketType = (int)SBSWTransType.CancelBet,
                } : new
                {
                    confirmBet.action,
                    confirmBet.operationId,
                    confirmBet.userId,
                    confirmBet.updateTime,
                    detail.refId,
                    detail.creditAmount,
                    detail.debitAmount,
                    TicketType = (int)SBSWTransType.CancelBet,
                });
                if (result.SPResult == -1) //查無此交易
                {
                    throw new SBSWException(ReturnCode.TransactionNotFound);
                }

                if (result.SPResult == -2) //已完成Cancel視為成功
                {
                    throw new SBSWException(ReturnCode.Success);
                }

                if (result.SPResult == -3)
                {
                    throw new SBSWException(ReturnCode.InvalidTransactionStatus);
                }

                var amount = detail.creditAmount - detail.debitAmount;
                var (gameType, tpCategory) = GetGameType(result.SportType);
                result.SetBalanceResult = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: amount,
                            tpId: (int)ThirdParty,
                            tpTransId: detail.refId,
                            gameType: (int)gameType,
                            tpCategory: tpCategory,
                            refId: detail.refId,
                            TransType.Refunds,
                            betData: null,
                            out decimal afterAmount,
                            "Cancel",
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.NotAllow);

                if (result.SetBalanceResult < 1)
                {
                    return new ExecuteResult(ReturnCode.InvalidAmount);
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
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

        private async Task<ReturnObj> Settle(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<Settle>>(bodyText);
            CheckKey(merchant, request.key);
            var resultList = new List<ExecuteResult>();
            foreach (var txn in request.message.txns)
            {
                resultList.Add(await ExecuteSettle(transType, request.message, txn));
            }
            var error = resultList.FirstOrDefault(result => result.ReturnCode != ReturnCode.Success);
            if (error != null)
            {
                return new ReturnObj(error);
            }

            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> SettleEnt(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<SettleEnt>>(bodyText);
            CheckKey(merchant, request.key);
            var result = await ExecuteSettle(transType,
                settle: new Settle()
                {
                    action = request.message.action,
                    operationId = DateTime.UtcNow.Ticks.ToString(),
                },
                detail: new SettleDetail()
                {
                    userId = request.message.userId,
                    refId = request.message.refId,
                    txId = request.message.txIds.First(),
                    updateTime = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-4, 0, 0)).DateTime,
                    winlostDate = request.message.winLostDate,
                    status = request.message.status,
                    payout = 0m,
                    creditAmount = request.message.creditAmount,
                    debitAmount = request.message.debitAmount,
                });
            return new ReturnObj(result);
        }

        private async Task<ExecuteResult> ExecuteSettle(SBSWTransType transType, Settle settle, SettleDetail detail)
        {
            try
            {
                var (tpCustInfo, tpCurrency) = GetTPInfo(detail.userId);

                detail.payout *= tpCurrency.ExchangeRate;
                detail.debitAmount *= tpCurrency.ExchangeRate;
                detail.creditAmount *= tpCurrency.ExchangeRate;

                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_Settle", trans, new
                {
                    settle.action,
                    settle.operationId,
                    detail.userId,
                    detail.refId,
                    detail.txId,
                    detail.updateTime,
                    detail.winlostDate,
                    detail.status,
                    detail.payout,
                    detail.creditAmount,
                    detail.debitAmount,
                    extraStatus = (detail.extraStatus ?? detail.extra_status) ?? "",
                    TicketType = (int)transType,
                });
                if (result.SPResult == -4)
                {
                    throw new SBSWException(ReturnCode.TransactionDuplicated, $"refId: {detail.refId} Duplicated.");
                }

                var amount = detail.creditAmount - detail.debitAmount;
                if (amount != 0)
                {
                    var (gameType, tpCategory) = GetGameType(result.SportType);

                    var memoHead = transType switch
                    {
                        SBSWTransType.Settle => "Payout",
                        SBSWTransType.SettleEnt => "Payout",
                        SBSWTransType.Unsettle => "Unsettle",
                        SBSWTransType.Resettle => "Resettle",
                    };
                    result.SetBalanceResult = _settledService.PayoutV2(
                                tpCustInfo: tpCustInfo,
                                amount: amount,
                                tpId: (int)ThirdParty,
                                tpTransId: detail.refId,
                                gameType: (int)gameType,
                                tpCategory: tpCategory,
                                refId: detail.refId,
                                transType == SBSWTransType.Unsettle ? TransType.Undo : TransType.Payout,
                                betData: null,
                                out decimal afterAmount,
                                memoHead,
                                out int bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow);

                    if (result.SetBalanceResult < 1)
                    {
                        return new ExecuteResult(ReturnCode.InvalidAmount);
                    }
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return result;
            }
            catch (SBSWException ex)
            {
                return new ExecuteResult(ex.ReturnCode, ex.Message);
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    return new ExecuteResult(ReturnCode.TransactionDuplicated, $"refId: {detail.refId} Duplicated.");
                }
                _logger.LogError(mySqlEx, mySqlEx.Message);
                return new ExecuteResult(ReturnCode.DatabaseError, mySqlEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ExecuteResult(ReturnCode.UnknownError, ex.Message);
            }
        }

        private async Task<ReturnObj> HealthCheck(SBSWTransType transType, string merchant, string bodyText)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> GetTicketInfo(SBSWTransType transType, string merchant, string bodyText)
        {
            var request = JsonConvert.DeserializeObject<BaseReq<GetTicketInfo>>(bodyText);
            CheckKey(merchant, request.key);
            var (_, tpCurrency) = GetTPInfo(request.message.userId);
            var (betList, settleList) = await GetTicketInfo(request.message.refId);
            if (!betList.Any())
            {
                return new GetTicketInfoObj(ReturnCode.TransactionNotFound);
            }

            //判斷TicketInfo
            var ticketStatus = "running";
            if (betList.Any(i => i.TicketType == (int)SBSWTransType.CancelBet))
            {
                ticketStatus = "refund";
            }
            if (settleList.Any())
            {
                ticketStatus = settleList.MaxBy(i => i.SQ).status;
            }

            var actualStake = betList.Sum(i => i.actualAmount);
            var winlostAmount = ticketStatus == "running" ? 0
                : settleList.Sum(i => i.creditAmount) - settleList.Sum(i => i.debitAmount) - actualStake;

            //將平台匯率轉回第三方匯率
            return new GetTicketInfoObj(ReturnCode.Success)
            {
                ticketStatus = ticketStatus,
                actualStake = actualStake / tpCurrency.ExchangeRate,
                winlostAmount = winlostAmount / tpCurrency.ExchangeRate,
            };
        }

        private async Task<ReturnObj> AdjustBalance(SBSWTransType transType, string merchant, string bodyText)
        {
            try
            {
                var request = JsonConvert.DeserializeObject<BaseReq<AdjustBalance>>(bodyText);
                CheckKey(merchant, request.key);

                var adjust = request.message;
                var (tpCustInfo, tpCurrency) = GetTPInfo(adjust.userId);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var creditAmount = adjust.balanceInfo.creditAmount * tpCurrency.ExchangeRate;
                var debitAmount = adjust.balanceInfo.debitAmount * tpCurrency.ExchangeRate;

                var result = await _mainDbClient.QueryFirstAsync<ExecuteResult>("Service_Bettrans_SBSW_Adjust", trans, new
                {
                    adjust.action,
                    adjust.time,
                    adjust.userId,
                    adjust.currency,
                    adjust.txId,
                    adjust.refId,
                    adjust.operationId,
                    adjust.betType,
                    adjust.betTypeName,
                    adjust.winlostDate,
                    creditAmount,
                    debitAmount,
                });
                if (result.SPResult == -4)
                {
                    throw new SBSWException(ReturnCode.TransactionDuplicated, $"refId: {adjust.refId} Duplicated.");
                }

                if (result.licenseeTxId < 1)//新增Adjust失敗
                {
                    throw new SBSWException(ReturnCode.InvalidParameters);
                }

                var amount = creditAmount - debitAmount;
                result.SetBalanceResult = _settledService.PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: amount,
                            tpId: (int)ThirdParty,
                            tpTransId: adjust.refId,
                            gameType: (int)GameType.SBSW_Sports,//Prize直接歸類到體育下
                            tpCategory: TpCategory.Sports,
                            refId: adjust.refId,
                            TransType.Prize,
                            betData: null,
                            out decimal afterAmount,
                            "SBSW Prize",
                            out int bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow);

                if (result.SetBalanceResult < 1)
                {
                    return new ReturnObj(ReturnCode.InvalidAmount);
                }

                trans.Commit();

                return new ReturnObj(ReturnCode.Success);
            }
            catch (SBSWException ex)
            {
                return new ReturnObj(ex.ReturnCode, ex.Message);
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    return new ReturnObj(ReturnCode.TransactionDuplicated, $"Transaction Duplicated.");
                }
                _logger.LogError(mySqlEx, mySqlEx.Message);
                return new ReturnObj(ReturnCode.DatabaseError, mySqlEx.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError, ex.Message);
            }
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
            if (!_siteSabaConfigs.TryGetValue(merchant, out var sabaConfig))
            {
                throw new SBSWException(ReturnCode.OperatorNotFound, $"Invalid merchant | merchant is:{merchant}");
            }

            if (sabaConfig.VendorId != key)
            {
                throw new SBSWException(ReturnCode.InvalidVendorId, $"Invalid key | key is:{key}");
            }
        }

        private void CheckBalance(string tpUserName, decimal totalBetAmount)
        {
            var balance = GetTpCustBalance(tpUserName);
            if (balance.Balance < totalBetAmount)
            {
                throw new SBSWException(ReturnCode.InsufficientBalance, $"CheckBalance | Insufficient. Balance={balance.Balance}. totalBetAmount={totalBetAmount}.");
            }
        }

        private (GameType gameType, TpCategory tpCategory) GetGameType(int sportType)
        {
            if (!_sportTypeConfig.TryGetValue(sportType, out var sabaSportType))
            {
                throw new SBSWException(ReturnCode.InvalidParameters, $"sportType:{sportType} not supported.");
            }

            return ((GameType)sabaSportType.TpGameType, (TpCategory)sabaSportType.TpCategoryId);
        }

        private TpCustBalance GetTpCustBalance(string tpUserName)
        {
            var (tpCustInfo, _) = GetTPInfo(tpUserName);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return tpCustBalance;
        }

        private async Task<(IEnumerable<BetInfo> betList, IEnumerable<SettleInfo> settleList)> GetTicketInfo(string refId)
        {
            return await _mainDbClient.QueryMultiple("Service_Bettrans_SBSW_Get",
                (SqlMapper.GridReader reader) =>
                {
                    var betList = reader.Read<BetInfo>();
                    var settleList = reader.Read<SettleInfo>();
                    return (betList, settleList);
                },
                new { refId });
        }
    }
}
