using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region OneGaming

        public int MoneyOutPK_OneGaming(TpCustInfo tpCustInfo, BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var amount = bettingDataPK_OneGaming.amount * -1;
            var ticketType = (int)BettransType.Bet;

            var p = new DynamicParameters(new
            {
                amount = amount,
                transaction_id = bettingDataPK_OneGaming.transaction_id,
                game_key = bettingDataPK_OneGaming.game_key,
                game_name = bettingDataPK_OneGaming.game_name,
                bcode = bettingDataPK_OneGaming.bcode,
                player_id = bettingDataPK_OneGaming.player_id,
                ref_tx_id = bettingDataPK_OneGaming.ref_tx_id,
                dateUnix = bettingDataPK_OneGaming.dateUnix,
                TicketType = ticketType
            });

            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransPK_OneGaming_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (p.Get<int>("result") == 1)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.OneGaming,
                        TransType.Wagers,
                        $"Transfer Out: {Math.Abs(bettingDataPK_OneGaming.amount):#,##0.##}",
                        bettingDataPK_OneGaming.ref_tx_id ?? bettingDataPK_OneGaming.transaction_id,
                        out afterAmount,
                        conn,
                        trans
                    );
                }

                trans.Commit();
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;

        }

        #endregion

        #region AMB

        public int PlaceBetPK_AMB(TpCustInfo tpCustInfo, BettingDataPK_AMB bettingDataPK_AMB, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var amount = bettingDataPK_AMB.amount * -1;
            
            var p = new DynamicParameters(new
            {
                TransId = $"{bettingDataPK_AMB.roundId}_{bettingDataPK_AMB.username}",
                username = bettingDataPK_AMB.username,
                agent = bettingDataPK_AMB.agent,
                game = bettingDataPK_AMB.game,
                gameId = bettingDataPK_AMB.gameId,
                roundId = bettingDataPK_AMB.roundId,
                amount = amount,
                currency = bettingDataPK_AMB.currency,
                refId = bettingDataPK_AMB.refId,
                timeUnix = new DateTimeOffset(DateTime.Parse(bettingDataPK_AMB.timestamp.ToString()), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                outStanding = bettingDataPK_AMB.outStanding,
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransPK_AMB_Bet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.AMB,
                        TransType.Wagers,
                        $"Stake: {bettingDataPK_AMB.amount:#,##0.##}",
                        bettingDataPK_AMB.refId,
                        out afterAmount,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    result = -1;
                }
                else
                {
                    throw;
                }
            }

            return result;
        }

        #endregion

        #region KM

        public async Task<KmDbResult> PlaceBetPK_KM(TpCustInfo tpCustInfo, KMDebitRequestTransaction debitTransaction, int tpId, int gameType)
        {
            var result = 0;
            var bonusId = 0;
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = debitTransaction.PtxId,
                userId = debitTransaction.UserId,
                amount = debitTransaction.Amount,
                currency = debitTransaction.Currency,
                ipaddress = debitTransaction.IpAddress,
                refptxid = debitTransaction.RefPtxId,
                txtype = debitTransaction.TxType,
                timestamp = DateTime.Parse(debitTransaction.Timestamp),
                platformtype = debitTransaction.PlatformType,
                gpcode = debitTransaction.GpCode,
                gamecode = debitTransaction.GameCode,
                gamename = debitTransaction.GameName,
                gametype = debitTransaction.GameType,
                roundid = debitTransaction.RoundId,
                betid = debitTransaction.BetId,
                senton = DateTime.Parse(debitTransaction.Senton),
                isclosinground = debitTransaction.IsClosingRound,
                ggr = debitTransaction.Ggr,
                turnover = debitTransaction.Turnover,
                bonustype = debitTransaction.BonusType,
                bonuscode = debitTransaction.BonusCode,
                description = debitTransaction.Desc,
                commission = debitTransaction.Commission,
                TicketType = (int) BettransType.Bet,
                TicketStatus = (int) TicketStatus.UnSettled,
                SettleFlag = (int) SettleFlag.Running
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync("Service_BettransPK_KM_Debit_Ins", p, transaction: trans,
                    commandType: CommandType.StoredProcedure);
                result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        logger.LogInformation("訂單已存在");
                        return new KmDbResult() {SPResult = result, Trx_id = trxId};
                    }

                    logger.LogError("exec Service_BettransPK_KM_Debit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                var afterAmount = 0M;
                if (debitTransaction.Amount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(debitTransaction.Amount) * -1,
                        (int)Tp.KM,
                        TransType.Wagers,
                        $"Stake: {Math.Abs(debitTransaction.Amount):#,##0.##}",
                        debitTransaction.PtxId,
                        out afterAmount,
                        conn,
                        trans
                    );
                    if (result != 1)
                    {
                        await trans.RollbackAsync();
                        logger.LogError($"PlaceBetPK_KM result:{result}, afterAmount:{afterAmount}");
                        if (result == 0 && afterAmount < 0)
                        {
                            throw new KmException(KMErrorCode.InsufficientFunds);
                        }

                        throw new KmException(KMErrorCode.SystemError);
                    }
                }

                trans.Commit();
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                    AfterAmount = afterAmount
                };
            }
            catch (KmException ex)
            {
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PlaceBetPK_KM failed: {e.Message}");
                await trans.RollbackAsync();
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PlaceBetNoSetBalance_KM(KMDebitRequestTransaction debitTransaction)
        {
            var result = 0;
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = debitTransaction.PtxId,
                userId = debitTransaction.UserId,
                amount = debitTransaction.Amount,
                currency = debitTransaction.Currency,
                ipaddress = debitTransaction.IpAddress,
                refptxid = debitTransaction.RefPtxId,
                txtype = debitTransaction.TxType,
                timestamp = DateTime.Parse(debitTransaction.Timestamp),
                platformtype = debitTransaction.PlatformType,
                gpcode = debitTransaction.GpCode,
                gamecode = debitTransaction.GameCode,
                gamename = debitTransaction.GameName,
                gametype = debitTransaction.GameType,
                roundid = debitTransaction.RoundId,
                betid = debitTransaction.BetId,
                senton = DateTime.Parse(debitTransaction.Senton),
                isclosinground = debitTransaction.IsClosingRound,
                ggr = debitTransaction.Ggr,
                turnover = debitTransaction.Turnover,
                bonustype = debitTransaction.BonusType,
                bonuscode = debitTransaction.BonusCode,
                description = debitTransaction.Desc,
                commission = debitTransaction.Commission,
                TicketType = (int) BettransType.Bet,
                TicketStatus = (int) TicketStatus.UnSettled,
                SettleFlag = (int) SettleFlag.Running
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync("Service_BettransPK_KM_Debit_Ins", p, transaction: trans,
                    commandType: CommandType.StoredProcedure);
                result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        logger.LogInformation($"訂單已存在:{trxId}");
                        return new KmDbResult() {SPResult = result, Trx_id = trxId};
                    }

                    logger.LogError("exec Service_BettransPK_KM_Debit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }
                
                trans.Commit();
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PlaceBetPK_KM failed: {e.Message}");
                await trans.RollbackAsync();
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        #endregion
    }
}