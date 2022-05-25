using System;
using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region BTI

        public async Task<BTIResult> CancelReserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

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

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_CancelReserve_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

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
                    trans.Commit();
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
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

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

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_CommitReserve_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
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

                if (btiResult.SPResult == 1 && btiResult.ReturnAmount <= 0)
                {
                    trans.Commit();
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
                    trans.Commit();
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
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(credit);
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_Credit_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
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
                    trans.Commit();
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
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BTISettleData>("Service_BettransSports_BTI_Unsettle_Get", commandType: CommandType.StoredProcedure);
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
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();

                dto.TicketType = (int)BettransType.Payout;
                dto.TicketStatus = (int)GetSboTicketStatus(dto.ResultType);
                var p = new DynamicParameters(dto);
                var result = await conn.ExecuteAsync("Service_BettransSports_SBO_Settle_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var sboResult = new SBOResult();
                if (result != 1)
                {
                    sboResult.Result = result;
                    return sboResult;
                }
                
                result = PayoutV2(
                        tpCustInfo,
                        dto.WinLoss,
                        (int)Tp.SBO,
                        dto.TransferCode,
                        (int)GameType.SBO_Sports,
                        TpCategory.Sports,
                        dto.TransferCode,
                        TransType.Payout,
                        null,
                        out var afterAmount,
                        "Payout",
                        out var bonusId,
                        conn,
                        trans);
                
                if (result == 1)
                {
                    sboResult.Result = result;
                    sboResult.AfterAmount = afterAmount;
                    await trans.CommitAsync();
                }
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
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();
                
                var p = new DynamicParameters(new
                {
                    dto.TransferCode
                });

                var result = await conn.ExecuteAsync("Service_BettransSports_SBO_Cancel_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                
                var sboResult = new SBOResult();
                if (result != 1)
                {
                    sboResult.Result = result;
                    return sboResult;
                }

                result = PayoutV2(
                    tpCustInfo,
                    dto.Amount,
                    (int)Tp.SBO,
                    dto.TransferCode,
                    (int)GameType.SBO_Sports,
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

                if (result == 1)
                {
                    sboResult.AfterAmount = afterAmount;
                    sboResult.Result = 1;
                    trans.Commit();
                }

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
                conn.Open();
                await using var trans = await conn.BeginTransactionAsync();
                
                var p = new DynamicParameters(dto);
                var result = await conn.ExecuteAsync("Service_BettransSports_SBO_Rollback_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                
                var sboResult = new SBOResult()
                {
                    Result = result,
                };
                if (result < 1)
                {
                    return sboResult;
                }

                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(dto.Amount) * -1,
                    (int)Tp.SBO,
                    dto.TransferCode,
                    (int)GameType.SBO_Sports,
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

                sboResult.Result = result;
                if (result == 1)
                {
                    sboResult.AfterAmount = afterAmount;
                    await trans.CommitAsync();
                }
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
    }
}
