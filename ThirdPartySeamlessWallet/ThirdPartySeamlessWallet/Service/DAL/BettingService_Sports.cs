using System.Data;

using Dapper;

using MySql.Data.MySqlClient;

using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region BTI

        public async Task<BTIResult> Reserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                var bonusId = 0;
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    request.amount,
                    TicketType = (int)BettransType.Reserve,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);
                _ = await _mainDbClient.ExecuteAsync("Service_BettransSports_BTI_Reserve_Ins", transaction: trans, p);

                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    Trx_id = p.Get<long>("trx_id"),
                };
                if (btiResult.SPResult < 1)
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PlaceBet(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(request.amount) * -1,
                    tpId: (int)Tp.BTI,
                    tpTransId: request.reserve_id,
                    gameType: (int)GameType.BTI_Sports,
                    refId: request.reserve_id,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    useBonus: out _,
                    conn: conn,
                    trans: trans
                );

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

        public async Task<BTIResult> DebitReserve_BTIAsync(TpCustInfo tpCustInfo, BTIDebitRequest debitReserve)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(debitReserve);
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);
                _ = await _mainDbClient.ExecuteAsync("Service_BettransSports_BTI_DebitReserve_Ins_V2", trans, p);

                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    Trx_id = p.Get<long>("trx_id"),
                };
                if (btiResult.SPResult < 1)
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    return btiResult;
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);

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

        public async Task<SBOResult> PlaceBetSport_SBO(TpCustInfo tpCustInfo, PlaceBetSportDto_SBO dto)
        {
            try
            {
                var bonusId = 0;
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                dto.TicketType = (int)BettransType.Bet;
                dto.TicketStatus = (int)TicketStatus.UnSettled;
                var p = new DynamicParameters(dto);
                var result = await _mainDbClient.ExecuteAsync("Service_BettransSports_SBO_Deduct_Ins", trans, p);

                if (result < 1)
                {
                    _logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode}" +
                                    $" exec Service_BettransSports_SBO_Deduct_Ins failed return Result:-1");
                    return new SBOResult() { Result = -1 };
                }

                result = PlaceBet(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(dto.Amount) * -1,
                    tpId: (int)Tp.SBO,
                    tpTransId: dto.TransferCode,
                    gameType: (int)dto.TpGameType,
                    refId: dto.TransferCode,
                    afterAmount: out var afterAmount,
                    bonusId: ref bonusId,
                    useBonus: out _,
                    conn: conn,
                    trans: trans
                );
                if (result != 1)
                {
                    _logger.LogError($"Username:{dto.Username} afterAmount:{afterAmount} < 0 return Result:-2");
                    return new SBOResult() { Result = -2 };
                }

                result = _tpUnsettleService.InsertTpUnsettle(Tp.SBO, dto.TpGameType, dto.TransferCode, trans, dto.BetTime);
                if (result < 1)
                {
                    _logger.LogError($"Username:{dto.Username} TransferCode:{dto.TransferCode} " +
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
                _logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }
    }
}
