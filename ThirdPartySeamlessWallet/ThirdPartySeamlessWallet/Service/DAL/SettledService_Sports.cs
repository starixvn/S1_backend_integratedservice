using System.Data;

using Dapper;

using MySql.Data.MySqlClient;

using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region BTI

        public async Task<BTIResult> CancelReserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });
                p.Add("returnAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await _mainDbClient.ExecuteAsync("Service_BettransSports_BTI_CancelReserve_Set", trans, p);

                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    ReturnAmount = p.Get<decimal>("returnAmount"),
                };

                if (btiResult.SPResult < 1 || btiResult.ReturnAmount == 0)
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: btiResult.ReturnAmount,
                        tpId: (int)Tp.BTI,
                        tpTransId: request.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: request.reserve_id,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Cancel",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                if (btiResult.SetBalanceResult == 1)
                {
                    btiResult.AfterAmount = afterAmount;
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return new BTIResult();
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        public async Task<BTIResult> CommitReserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("returnAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await _mainDbClient.ExecuteAsync("Service_BettransSports_BTI_CommitReserve_Set", trans, p);
                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    ReturnAmount = p.Get<decimal>("returnAmount"),
                    Trx_id = p.Get<long>("trx_id"),
                };

                if (btiResult.SPResult < 1)
                {
                    return btiResult;
                }

                btiResult.SPResult = _tpUnsettleService.InsertTpUnsettle(Tp.BTI, GameType.BTI_Sports, request.reserve_id, trans);
                if (btiResult.SPResult == 1 && btiResult.ReturnAmount <= 0)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                    return btiResult;
                }

                //Commit金額跟Reserve金額不同需退回部分下注金額
                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: btiResult.ReturnAmount,
                        tpId: (int)Tp.BTI,
                        tpTransId: request.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: request.reserve_id,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Refund",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                btiResult.AfterAmount = afterAmount;
                if (btiResult.SetBalanceResult == 1)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }
                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return new BTIResult();
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        public async Task<BTIResult> Credit_BTIAsync(TpCustInfo tpCustInfo, BTICreditRequest credit)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(credit);
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await _mainDbClient.ExecuteAsync("Service_BettransSports_BTI_Credit_Ins_V3", trans, p);
                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    Trx_id = p.Get<long>("trx_id"),
                };

                if (btiResult.SPResult < 1)
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: credit.amount,
                        tpId: (int)Tp.BTI,
                        tpTransId: credit.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: credit.reserve_id,
                        TransType.Payout,
                        betData: null,
                        out decimal afterAmount,
                        credit.amount < 0 ? "Resettle" : "Payout",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                btiResult.AfterAmount = afterAmount;



                if (btiResult.SetBalanceResult == 1)
                {
                    if (credit.SettleFlag == (int)SettleFlag.EndGame)
                    {
                        _tpUnsettleService.InsertTpUnsettle(Tp.BTI, GameType.BTI_Sports, credit.reserve_id, trans);
                    }

                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return new BTIResult();
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        #endregion

        public IEnumerable<BTISettleData> GetUnsettledList_BTI()
        {
            return _mainDbClient.Query<BTISettleData>("Service_BettransSports_BTI_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_BTI(string account, string transId, bool isSuccess)
        {
            var parameters = new DynamicParameters(new { account, transId, isSuccess });
            return _mainDbClient.Execute("Service_BettransSports_BTI_Settle_Set", parameters: parameters);
        }

        public async Task<SBOResult> PayoutSports_SBO(TpCustInfo tpCustInfo, PayoutSportDto_SBO dto)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                dto.TicketType = (int)BettransType.Payout;
                dto.TicketStatus = (int)GetSboTicketStatus(dto.ResultType);
                var p = new DynamicParameters(dto);
                var result = await _mainDbClient.ExecuteAsync("Service_BettransSports_SBO_Settle_Set", trans, p);

                if (result < 1)
                {
                    logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode}" +
                                    $" exec Service_BettransSports_SBO_Settle_Set failed return Result:-1");
                    return new SBOResult() { Result = -1 };
                }

                var afterAmount = 0m;
                if (dto.WinLoss != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        dto.WinLoss,
                        (int)Tp.SBO,
                        dto.TransferCode,
                        (int)dto.TpGameType,
                        TpCategory.Sports,
                        dto.TransferCode,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        "Payout",
                        out var bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );

                    if (result != 1)
                    {
                        logger.LogError($"Username:{dto.Username} afterAmount:{afterAmount} < 0 return Result:-2");
                        return new SBOResult() { Result = -2 };
                    }
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.SBO, dto.TpGameType, dto.TransferCode, trans, dto.ResultTime);
                if (result < 1)
                {
                    logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode} " +
                                    $"exec Service_TpUnsettle_Ins_V2 failed return Result:-3");
                    return new SBOResult() { Result = -3 };
                }

                var sboResult = new SBOResult
                {
                    Result = 1,
                    AfterAmount = afterAmount
                };
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return sboResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }

        public async Task<SBOResult> CancelBetSports_SBO(TpCustInfo tpCustInfo, CancelSportDto_SBO dto)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    dto.TransferCode
                });

                var result = await _mainDbClient.ExecuteAsync("Service_BettransSports_SBO_Cancel_Set", trans, p);
                if (result != 1)
                {
                    logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode} " +
                                    $"exec Service_BettransSports_SBO_Cancel_Set failed return Result:-1");
                    return new SBOResult() { Result = -1 };
                }

                result = PayoutV2(
                    tpCustInfo,
                    dto.Amount,
                    (int)Tp.SBO,
                    dto.TransferCode,
                    (int)dto.TpGameType,
                    TpCategory.Sports,
                    dto.TransferCode,
                    TransType.Refunds,
                    null,
                    out var afterAmount,
                    "Cancel",
                    out var bonusId,
                    conn,
                    trans,
                    AllowNegative.Allow);
                if (result != 1)
                {
                    logger.LogError($"Username:{dto.Username} afterAmount:{afterAmount} < 0 return Result:-2");
                    return new SBOResult() { Result = -2 };
                }

                var partitionTime = dto.ResultTime ?? DateTime.UtcNow;
                result = _tpUnsettleService.InsertTpUnsettle(Tp.SBO, dto.TpGameType, dto.TransferCode, trans, partitionTime);
                if (result < 1)
                {
                    logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode}" +
                                    $" exec Service_TpUnsettle_Ins_V2 failed return Result:-3");
                    return new SBOResult() { Result = -3 };
                }

                var sboResult = new SBOResult
                {
                    AfterAmount = afterAmount,
                    Result = 1
                };
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return sboResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }

        public async Task<SBOResult> RollbackSport_SBO(TpCustInfo tpCustInfo, RollbackSportDto_SBO dto)
        {
            try
            {
                var bonusId = 0;
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(dto);
                var result = await _mainDbClient.ExecuteAsync("Service_BettransSports_SBO_Rollback_Set", trans, p);
                if (result < 1)
                {
                    logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode}" +
                                    $" exec Service_BettransSports_SBO_Rollback_Set failed return Result:-1");
                    return new SBOResult() { Result = -1 };
                }

                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(dto.Amount) * -1,
                    (int)Tp.SBO,
                    dto.TransferCode,
                    (int)dto.TpGameType,
                    TpCategory.Sports,
                    dto.TransferCode,
                    TransType.Undo,
                    null,
                    out var afterAmount,
                    "Rollback",
                    out bonusId,
                    conn: conn,
                    trans: trans,
                    AllowNegative.Allow);
                if (result != 1)
                {
                    logger.LogError($"Username:{dto.Username} afterAmount:{afterAmount} < 0 return Result:-2");
                    return new SBOResult() { Result = -2 };
                }

                if (dto.Status == 1)
                {
                    var partitionTime = dto.ResultTime.HasValue ? dto.ResultTime : DateTime.Now;
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.SBO, dto.TpGameType, dto.TransferCode, trans, partitionTime);
                    if (result < 1)
                    {
                        logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode}" +
                                        $" exec Service_TpUnsettle_Ins_V2 failed return Result:-3");
                        return new SBOResult() { Result = -3 };
                    }
                }

                var sboResult = new SBOResult
                {
                    AfterAmount = afterAmount,
                    Result = 1
                };
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return sboResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }

        private TicketStatus GetSboTicketStatus(int resultType)
        {
            return resultType switch
            {
                0 => TicketStatus.Win,
                1 => TicketStatus.Lost,
                2 => TicketStatus.Draw,
                _ => TicketStatus.UnSettled
            };
        }

        public IEnumerable<SBSWUnsettle> GetSBSWUnsettleList(int LimitCount = 5000)
        {
            return _mainDbClient.Query<SBSWUnsettle>("Service_SBSW_TpUnsettle_Get", parameters: new { LimitCount });
        }

        public BetRecordInfo GetSBSWUnsettleList(int TpId, string TransId, int GameType)
        {
            return _mainDbClient.QueryFirst<BetRecordInfo>("Service_TpBetRecord_Get", parameters: new { TpId, TransId, GameType });
        }
    }
}
