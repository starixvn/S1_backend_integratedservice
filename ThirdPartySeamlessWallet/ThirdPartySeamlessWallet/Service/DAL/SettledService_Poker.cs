using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region OneGaming

        public int MoneyInPK_OneGaming(TpCustInfo tpCustInfo, BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var ticketType = (int)BettransType.Payout;

            var p = new DynamicParameters(new
            {
                amount = bettingDataPK_OneGaming.amount,
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

                if (result > 0)
                {
                    if (bettingDataPK_OneGaming.amount != 0 && p.Get<int>("result") == 1)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(bettingDataPK_OneGaming.amount),
                            (int)Tp.OneGaming,
                            TransType.Payout,
                            $"Transfer In: {Math.Abs(bettingDataPK_OneGaming.amount):#,##0.##}",
                            bettingDataPK_OneGaming.ref_tx_id ?? bettingDataPK_OneGaming.transaction_id,
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

            return result;
        }

        public int RollbackPK_OneGaming(TpCustInfo tpCustInfo, RollbackDataPK_OneGaming rollbackDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 1;
            var transactionId = rollbackDataPK_OneGaming.transaction_id.Replace("TimeOut_", "");

            var p = new DynamicParameters(new
            {
                type = rollbackDataPK_OneGaming.type,
                amount = rollbackDataPK_OneGaming.amount,
                transaction_id = transactionId,
                game_key = rollbackDataPK_OneGaming.game_key,
                game_name = rollbackDataPK_OneGaming.game_name,
                bcode = rollbackDataPK_OneGaming.bcode,
                player_id = rollbackDataPK_OneGaming.player_id,
                ref_tx_id = (string)null,
                dateUnix = rollbackDataPK_OneGaming.dateUnix
            });

            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var amount = Math.Abs(rollbackDataPK_OneGaming.amount);
            if (string.Equals(rollbackDataPK_OneGaming.type, "MoneyIn", StringComparison.OrdinalIgnoreCase))
            {
                amount *= -1;
            }

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            conn.Execute("Service_BettransPK_OneGaming_Rollback_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (amount != 0 && p.Get<int>("result") == 1)
            {
                walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    amount,
                    (int)Tp.OneGaming,
                    TransType.Undo,
                    $"Rollback: {Math.Abs(rollbackDataPK_OneGaming.amount):#,##0.##}",
                    transactionId,
                    out afterAmount,
                    conn,
                    trans, AllowNegative.Allow
                );
            }

            trans.Commit();
            return result;
        }

        public IEnumerable<SettleDataPK_OneGaming> GetUnsettledListPK_OneGaming()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleDataPK_OneGaming>("Service_TpWagerPK_OneGaming_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_OneGaming(string ticket)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_OneGaming_Settle_Set", new { ticket = ticket }, commandType: CommandType.StoredProcedure);
        }

        public int Settled_OneGaming(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = SettleTicket(
                tpCustInfo,
                betRecordInfo,
                0,
                out _,
                conn,
                trans,
                false/*Don't need Payout*/
            );

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        #endregion

        #region V8

        public IEnumerable<SettleDataPK_V8> GetUnsettledListPK_V8()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleDataPK_V8>("Service_TpWagerPK_V8_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_V8(string gameId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_V8_Settle_Set", new { GameID = gameId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region AMB

        public int CancelBetPK_AMB(TpCustInfo tpCustInfo, CancelDataPK_AMB cancelDataPK_AMB, out decimal afterAmount)
        {

            afterAmount = 0;
            int result = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            string TransId = $"{cancelDataPK_AMB.roundId}_{cancelDataPK_AMB.username}";
            var p = new DynamicParameters(new
            {
                TransId = TransId,
                refId = cancelDataPK_AMB.refId,
                TicketType = (int)BettransType.CancelBet,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (int)SettleFlag.Settled
            });

            try
            {
                // check cancel
                var check = conn.QueryFirstOrDefault<BettransDataPK_AMB>("Service_BettransPK_AMB_Get", new { TransId = TransId, TicketType = (int)BettransType.CancelBet, TicketStatus = (int)TicketStatus.Refund }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check != null)
                {
                    // cancel
                    return -1;
                }

                result = conn.Execute("Service_BettransPK_AMB_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                if (result > 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(cancelDataPK_AMB.amount),
                        (int)Tp.AMB,
                        TransType.Refunds,
                        $"Cancel: {cancelDataPK_AMB.amount:#,##0.##}",
                        cancelDataPK_AMB.refId,
                        out afterAmount,
                        conn,
                        trans
                    );
                }
                else
                {
                    result = -1;
                }

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                result = -99;
                throw;
            }

            return result;

        }

        public int PayoutPK_AMB(TpCustInfo tpCustInfo, PayoutDataPK_AMB payoutDataPK_AMB, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            int ticketStatus = (int)TicketStatus.Win;

            if (payoutDataPK_AMB.winlose < 0)
            {
                ticketStatus = (int)TicketStatus.Lost;
            }
            else if (payoutDataPK_AMB.winlose == 0)
            {
                ticketStatus = (int)TicketStatus.Draw;
            }

            DateTime fmTime = DateTime.ParseExact(payoutDataPK_AMB.timestamp.ToString("yyyy-MM-ddTHH:mm:ss:fffZ"), "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
            var p = new DynamicParameters(new
            {
                TransId = $"{payoutDataPK_AMB.roundId}_{payoutDataPK_AMB.username}",
                username = payoutDataPK_AMB.username,
                agent = payoutDataPK_AMB.agent,
                game = payoutDataPK_AMB.game,
                gameId = payoutDataPK_AMB.gameId,
                product = payoutDataPK_AMB.product,
                roundId = payoutDataPK_AMB.roundId,
                amount = payoutDataPK_AMB.amount,
                commission = payoutDataPK_AMB.commission,
                winlose = payoutDataPK_AMB.winlose,
                currency = payoutDataPK_AMB.currency,
                betAmount = payoutDataPK_AMB.betAmount,
                refId = payoutDataPK_AMB.refId,
                timeUnix = new DateTimeOffset(DateTime.Parse(payoutDataPK_AMB.timestamp.ToString()), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                outStanding = payoutDataPK_AMB.outStanding,
                TicketType = (int)BettransType.Payout,
                TicketStatus = ticketStatus,
                SettleFlag = SettleFlag.EndGame
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                result = conn.Execute("Service_BettransPK_AMB_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (payoutDataPK_AMB.amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(payoutDataPK_AMB.amount),
                            (int)Tp.AMB,
                            TransType.Payout,
                            $"Payout: {Math.Abs(payoutDataPK_AMB.amount):#,##0.##}",
                            payoutDataPK_AMB.refId,
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
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
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

        public int ResettlePK_AMB(TpCustInfo tpCustInfo, VoidDataPK_AMB voidDataPK_AMB, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            string TransId = $"{voidDataPK_AMB.roundId}_{voidDataPK_AMB.username}";
            DateTime fmTime = DateTime.ParseExact(voidDataPK_AMB.timestamp.ToString("yyyy-MM-ddTHH:mm:ss:fffZ"), "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
            var p = new DynamicParameters(new
            {
                TransId = TransId,
                username = voidDataPK_AMB.username,
                agent = voidDataPK_AMB.agent,
                game = voidDataPK_AMB.game,
                product = voidDataPK_AMB.product,
                roundId = voidDataPK_AMB.roundId,
                amount = voidDataPK_AMB.amount,
                currency = voidDataPK_AMB.currency,
                refId = voidDataPK_AMB.refId,
                timeUnix = new DateTimeOffset(DateTime.Parse(voidDataPK_AMB.timestamp.ToString()), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = SettleFlag.EndGame
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check void
                var check = conn.QueryFirstOrDefault<BettransDataPK_AMB>("Service_BettransPK_AMB_Get", new { TransId = TransId, TicketType = (int)BettransType.Payout, TicketStatus = (int)TicketStatus.Refund }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check != null)
                {
                    // voided
                    return -1;
                }

                result = conn.Execute("Service_BettransPK_AMB_Resettle_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (voidDataPK_AMB.amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            voidDataPK_AMB.amount,
                            (int)Tp.AMB,
                            TransType.Refunds,
                            $"Resettle: {Math.Abs(voidDataPK_AMB.amount):#,##0.##}",
                            voidDataPK_AMB.refId,
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
            }
            catch (Exception ex)
            {
                result = -99;
                throw;
            }

            return result;
        }

        public IEnumerable<SettleData_AMB> GetUnsettledListPK_AMB()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleData_AMB>("Service_BettransPK_AMB_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_BettransPK_AMB(string TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransPK_AMB_Settle_Set", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region KM
        public async Task<KmDbResult> CancelbetPK_KM(TpCustInfo tpCustInfo, KMCreditRequestTransaction creditTransaction)
        {
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = creditTransaction.ptxid,
                userId = creditTransaction.userid,
                amount = creditTransaction.amt,
                currency = creditTransaction.cur,
                ipaddress = creditTransaction.ipaddress,
                refptxid = creditTransaction.refptxid,
                txtype = creditTransaction.txtype,
                timestamp = DateTime.Parse(creditTransaction.timestamp),
                platformtype = creditTransaction.platformtype,
                gpcode = creditTransaction.gpcode,
                gamecode = creditTransaction.gamecode,
                gamename = creditTransaction.gamename,
                gametype = creditTransaction.gametype,
                roundid = creditTransaction.roundid,
                betid = creditTransaction.betid,
                senton = DateTime.Parse(creditTransaction.senton),
                isclosinground = creditTransaction.isclosinground,
                ggr = creditTransaction.ggr,
                turnover = creditTransaction.turnover,
                bonustype = creditTransaction.bonustype,
                bonuscode = creditTransaction.bonuscode,
                description = creditTransaction.desc,
                commission = creditTransaction.commission,
                TicketType = (int)BettransType.CancelBet,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (int)SettleFlag.Settled
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", p, transaction: trans,
                    commandType: CommandType.StoredProcedure);

                int result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        logger.LogInformation($"訂單已存在, trxid:{trxId}");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }
                    else if (result == 2)
                    {
                        logger.LogInformation($"交易不存在, trxid:{trxId}");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }

                    logger.LogError("exec Service_BettransPK_KM_Credit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                var afterAmount = 0M;
                if (creditTransaction.amt != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(creditTransaction.amt),
                        (int)Tp.KM,
                        TransType.Refunds,
                        $"Cancel {creditTransaction.amt:#,##0.##}",
                        creditTransaction.ptxid,
                        out afterAmount,
                        conn,
                        trans
                    );
                    if (result != 1)
                    {
                        logger.LogError("SetBalance error");
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
                await trans.RollbackAsync();
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await trans.RollbackAsync();
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PayoutPK_KM(TpCustInfo tpCustInfo, KMCreditRequestTransaction creditTransaction)
        {
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = creditTransaction.ptxid,
                userId = creditTransaction.userid,
                amount = creditTransaction.amt,
                currency = creditTransaction.cur,
                ipaddress = creditTransaction.ipaddress,
                refptxid = creditTransaction.refptxid,
                txtype = creditTransaction.txtype,
                timestamp = DateTime.Parse(creditTransaction.timestamp).ToUniversalTime(),
                platformtype = creditTransaction.platformtype,
                gpcode = creditTransaction.gpcode,
                gamecode = creditTransaction.gamecode,
                gamename = creditTransaction.gamename,
                gametype = creditTransaction.gametype,
                roundid = creditTransaction.roundid,
                betid = creditTransaction.betid,
                senton = DateTime.Parse(creditTransaction.senton).ToUniversalTime(),
                isclosinground = creditTransaction.isclosinground,
                ggr = creditTransaction.ggr,
                turnover = creditTransaction.turnover,
                bonustype = creditTransaction.bonustype,
                bonuscode = creditTransaction.bonuscode,
                description = creditTransaction.desc,
                commission = creditTransaction.commission,
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.Win,
                SettleFlag = (int)SettleFlag.EndGame
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                int result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        logger.LogInformation($"訂單已存在, trxid:{trxId}");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }
                    else if (result == 2)
                    {
                        logger.LogInformation($"交易不存在, trxid:{trxId}");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }
                    logger.LogError("exec Service_BettransPK_KM_Credit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                var afterAmount = 0M;
                if (creditTransaction.amt != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(creditTransaction.amt),
                        (int)Tp.KM,
                        TransType.Payout,
                        $"Payout {creditTransaction.amt:#,##0.##}",
                        creditTransaction.ptxid,
                        out afterAmount,
                        conn,
                        trans
                    );
                    if (result != 1)
                    {
                        logger.LogError("SetBalance error");
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
                await trans.RollbackAsync();
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await trans.RollbackAsync();
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PayoutNoSetBalance_KM(KMCreditRequestTransaction creditTransaction)
        {
            var result = 0;
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = creditTransaction.ptxid,
                userId = creditTransaction.userid,
                amount = creditTransaction.amt,
                currency = creditTransaction.cur,
                ipaddress = creditTransaction.ipaddress,
                refptxid = creditTransaction.refptxid,
                txtype = creditTransaction.txtype,
                timestamp = DateTime.Parse(creditTransaction.timestamp),
                platformtype = creditTransaction.platformtype,
                gpcode = creditTransaction.gpcode,
                gamecode = creditTransaction.gamecode,
                gamename = creditTransaction.gamename,
                gametype = creditTransaction.gametype,
                roundid = creditTransaction.roundid,
                betid = creditTransaction.betid,
                senton = DateTime.Parse(creditTransaction.senton),
                isclosinground = creditTransaction.isclosinground,
                ggr = creditTransaction.ggr,
                turnover = creditTransaction.turnover,
                bonustype = creditTransaction.bonustype,
                bonuscode = creditTransaction.bonuscode,
                description = creditTransaction.desc,
                commission = creditTransaction.commission,
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.Win,
                SettleFlag = (int)SettleFlag.EndGame
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await conn.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        logger.LogInformation("訂單已存在");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }
                    logger.LogError("exec Service_BettransPK_KM_Credit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                trans.Commit();
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                };
            }
            catch (KmException ex)
            {
                await trans.RollbackAsync();
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await trans.RollbackAsync();
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public IEnumerable<SettleData> GetUnsettledList_KM(string spName)
        {
            return _wagerDbClient.Query<SettleData>(spName);
        }

        public int SetSettleFlag_KM(string spName, string transId, bool isSuccess)
        {
            var parameters = new DynamicParameters(new { transId, isSuccess });
            return _wagerDbClient.Execute(spName, parameters: parameters);
        }

        #endregion

        #region MP

        public int MoneyInPK_MP(TpCustInfo tpCustInfo, TransferData_MP transferData_MP, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            var amount = transferData_MP.requestAmount;

            var p = new DynamicParameters(new
            {
                channelId = transferData_MP.channelId,
                account = transferData_MP.account,
                requestAmount = transferData_MP.requestAmount,
                gameId = transferData_MP.gameId,
                orderId = transferData_MP.orderId,
                roundId = transferData_MP.roundId,
                gameNo = transferData_MP.gameNo,
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransPK_MP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && amount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.MP,
                        TransType.Payout,
                        $"Transfer In: {transferData_MP.requestAmount:#,##0.##}",
                        transferData_MP.orderId,
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
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
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

        public int CancelMoneyInPK_MP(TpCustInfo tpCustInfo, TransferData_MP transferData_MP, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;

            var p = new DynamicParameters(new
            {
                channelId = transferData_MP.channelId,
                account = transferData_MP.account,
                orderId = transferData_MP.orderId,
                roundId = transferData_MP.roundId,
                gameNo = transferData_MP.gameNo,
                TicketType = (int)BettransType.CancelBet,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (int)SettleFlag.Settled
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransPK_MP_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal stake = p.Get<decimal>("out_stake");

                if (stake > 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        stake,
                        (int)Tp.MP,
                        TransType.Payout,
                        $"Cancel: {stake:#,##0.##}",
                        transferData_MP.orderId,
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
            else
            {
                p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                conn.Execute("Service_BettransPK_MP_CancelBet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var refBettransLC = p.Get<int>("out_result");

                if (refBettransLC == 2)
                {
                    // insert cancel record
                    trans.Commit();
                }
                result = -1;
            }

            return result;
        }

        public IEnumerable<SettleDataPK_MP> GetUnsettledListPK_MP()
        {
            return _wagerDbClient.Query<SettleDataPK_MP>("Service_TpWagerPK_MP_Unsettle_Get");
        }

        public int SetSettleFlag_TpWagerPK_MP(string TransId)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_MP_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }
        #endregion
    }
}
