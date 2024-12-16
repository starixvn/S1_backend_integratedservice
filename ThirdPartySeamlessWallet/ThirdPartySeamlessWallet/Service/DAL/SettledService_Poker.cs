using System.Data;
using System.Globalization;

using Dapper;

using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.KY;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_OneGaming_Ins", trans, p);

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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _mainDbClient.Execute("Service_BettransPK_OneGaming_Rollback_Set", trans, p);

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

            _mainDbClient.CommitAndDisposeConnection(conn, trans);
            return result;
        }

        public IEnumerable<SettleDataPK_OneGaming> GetUnsettledListPK_OneGaming()
        {
            return _wagerDbClient.Query<SettleDataPK_OneGaming>("Service_TpWagerPK_OneGaming_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_OneGaming(string ticket)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_OneGaming_Settle_Set", commandType: CommandType.StoredProcedure, new { ticket = ticket });
        }

        public int Settled_OneGaming(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = SettleTicket(tpCustInfo, betRecordInfo, trans);

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            return result;
        }

        #endregion

        #region V8 Transfer

        public IEnumerable<SettleDataPK_V8> GetUnsettledListPK_V8()
        {
            return _wagerDbClient.Query<SettleDataPK_V8>("Service_TpWagerPK_V8_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_V8(string gameId)
        {
            return _wagerDbClient.Execute("Service_TpWagerPK_V8_Settle_Set", commandType: CommandType.StoredProcedure, new { GameID = gameId });
        }

        #endregion

        #region AMB

        public int CancelBetPK_AMB(TpCustInfo tpCustInfo, CancelDataPK_AMB cancelDataPK_AMB, out decimal afterAmount)
        {

            afterAmount = 0;
            int result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
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
                var check = _mainDbClient.QueryFirst<BettransDataPK_AMB>("Service_BettransPK_AMB_Get", trans, new { TransId = TransId, TicketType = (int)BettransType.CancelBet, TicketStatus = (int)TicketStatus.Refund });
                if (check != null)
                {
                    // cancel
                    return -1;
                }

                result = _mainDbClient.Execute("Service_BettransPK_AMB_CancelBet_Set", trans, p);
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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_AMB_Payout_Ins", trans, p);

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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check void
                var check = _mainDbClient.QueryFirst<BettransDataPK_AMB>("Service_BettransPK_AMB_Get", trans, new { TransId = TransId, TicketType = (int)BettransType.Payout, TicketStatus = (int)TicketStatus.Refund });
                if (check != null)
                {
                    // voided
                    return -1;
                }

                result = _mainDbClient.Execute("Service_BettransPK_AMB_Resettle_Ins", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            return _mainDbClient.Query<SettleData_AMB>("Service_BettransPK_AMB_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_BettransPK_AMB(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransPK_AMB_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region KM
        public async Task<KmDbResult> CancelbetPK_KM(TpCustInfo tpCustInfo, KMCreditRequestTransaction creditTransaction)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
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
                await _mainDbClient.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", trans, p);

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

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                    AfterAmount = afterAmount
                };
            }
            catch (KmException ex)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PayoutPK_KM(TpCustInfo tpCustInfo, KMCreditRequestTransaction creditTransaction)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
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
                await _mainDbClient.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", trans, p);

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
                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                    AfterAmount = afterAmount
                };
            }
            catch (KmException ex)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PayoutNoSetBalance_KM(KMCreditRequestTransaction creditTransaction)
        {
            var result = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
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
                await _mainDbClient.ExecuteAsync("Service_BettransPK_KM_Credit_Ins", trans, p);

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

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                };
            }
            catch (KmException ex)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec PayoutPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> RewardPK_KM(TpCustInfo tpCustInfo, KMRewardRequestTransaction rewardTransaction)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                userid = rewardTransaction.userid,
                amt = rewardTransaction.amt,
                cur = rewardTransaction.cur,
                ptxid = rewardTransaction.ptxid,
                desc = string.IsNullOrEmpty(rewardTransaction.desc) ? "" : rewardTransaction.desc,
                SettleFlag = (int)SettleFlag.EndGame
            });
            p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await _mainDbClient.ExecuteAsync("Service_RewardPK_KM_Ins", trans, p);

                int result = p.Get<short>("SPResult");
                var sq = p.Get<long>("SQ");
                if (result != 1)
                {
                    if (result == -2)
                    {
                        logger.LogInformation($"訂單已存在, txid:{sq}");
                        return new KmDbResult() { SPResult = result, Trx_id = sq };
                    }

                    logger.LogError("exec Service_RewardPK_KM_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                var afterAmount = 0M;
                if (rewardTransaction.amt != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(rewardTransaction.amt),
                        (int)Tp.KM,
                        TransType.Prize,
                        $"Prize",
                        rewardTransaction.ptxid,
                        out afterAmount,
                        conn,
                        trans
                    );

                    if (result != 1)
                    {
                        logger.LogError("Prize error");
                        throw new KmException(KMErrorCode.SystemError);
                    }
                }

                if (result > 0)
                {
                    result = _tpUnRewardService.InsertTpUnReward(Tp.KM, GameType.KM_Poker, rewardTransaction.ptxid, rewardTransaction.userid, trans);
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }

                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = sq,
                    AfterAmount = afterAmount
                };
            }
            catch (KmException ex)
            {
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw;
            }
            catch (Exception e)
            {
                logger.LogError(e, $"exec RewardPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public IEnumerable<SettleData> GetUnsettledList_KM(string spName)
        {
            return _wagerDbClient.Query<SettleData>(spName);
        }

        public int SetSettleFlag_KM(string spName, string transId)
        {
            var parameters = new DynamicParameters(new { transId });
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
                playerId = transferData_MP.playerId,
                totalBet = transferData_MP.totalBet,
                validBet = transferData_MP.validBet,
                totalWithdraw = transferData_MP.totalWithdraw,
                revenue = transferData_MP.revenue,
                betCount = transferData_MP.betCount,
                timestamp = transferData_MP.timestamp,
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_MP_Ins_V2", trans, p);

                if (result > 0 && amount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.MP,
                        TransType.Payout,
                        $"Transfer In {transferData_MP.roundId} : {transferData_MP.requestAmount:#,##0.##}",
                        transferData_MP.orderId,
                        out afterAmount,
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
                playerId = transferData_MP.playerId,
                TicketType = (int)BettransType.CancelBet,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (int)SettleFlag.Settled
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_BettransPK_MP_CancelBet_Set", trans, p);

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
                        $"Cancel {transferData_MP.roundId} : {stake:#,##0.##}",
                        transferData_MP.orderId,
                        out afterAmount,
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
            else
            {
                p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                _mainDbClient.Execute("Service_BettransPK_MP_CancelBet_Ins_V2", trans, p);
                var refBettransLC = p.Get<int>("out_result");

                if (refBettransLC == 2)
                {
                    // insert cancel record
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    result = -2;
                    return result;
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

        #region V8

        public int PayoutPK_V8(TpCustInfo tpCustInfo, BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(bettingDataPK_V8);
                p.Add("TicketType", (int)BettransType.Payout);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);
                p.Add("SettleFlag", (int)SettleFlag.Running);

                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataPK_V8.timestamp).DateTime;

                var bettrans = _partitionService.Query<BettransDataPK_V8>(partitionTime, "Service_BettransPK_V8_gameNo_Get", new { gameNo = bettingDataPK_V8.gameNo });

                // 檢查是否有下注該round
                if (!bettrans.Any(i => i.TicketType == BettransType.Bet))
                {
                    // 我方游戏目前只有财神到有免费游戏机制，进行免费游戏是无需进行下注的，此部分再劳烦贵司进行调整哦
                    if (bettingDataPK_V8.kindId != "8910")
                    {
                        return -2;
                    }
                }
                if (bettrans.Any(i => i.TicketType == BettransType.Payout))
                {
                    // 一局游戏结算后是返回一笔S1003的哦
                    return -3;
                }

                result = _partitionService.Execute(partitionTime, "Service_BettransPK_V8_Ins", p, trans);

                if (result > 0 && bettingDataPK_V8.money != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataPK_V8.money,
                        (int)Tp.V8,
                        TransType.Payout,
                        $"Payout: {bettingDataPK_V8.money:#,##0.##} {bettingDataPK_V8.orderId}",
                        bettingDataPK_V8.gameNo,
                        out afterAmount,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int CancelBetPK_V8(TpCustInfo tpCustInfo, BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(bettingDataPK_V8);
                p.Add("TicketType", (int)BettransType.CancelBet);
                p.Add("TicketStatus", (int)TicketStatus.Refund);
                p.Add("SettleFlag", (int)SettleFlag.Settled);

                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataPK_V8.timestamp).DateTime;
                result = _partitionService.Execute(partitionTime, "Service_BettransPK_V8_CancelBet_Set", p, trans);
                /*
                 贵方收到通知后需先判断原始订单状态是否成功
                 若原始订单为成功状态时，方能取消下注並修改訂單狀態
                 若订单不存在，请先塞入资料库并记录为取消订单，此订单状态将不可再修改。
                */
                if (result > 0)
                {
                    // cancel update
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataPK_V8.money,
                        (int)Tp.V8,
                        TransType.Refunds,
                        $"Cancel: {bettingDataPK_V8.money:#,##0.##} {bettingDataPK_V8.orderId}",
                        bettingDataPK_V8.gameNo,
                        out afterAmount,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    // insert cancel
                    var cancelInsert = _partitionService.Execute(partitionTime, "Service_BettransPK_V8_Ins", p, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public IEnumerable<BettransDataPK_V8> GetRecordPK_V8(OrderDataPK_V8 orderDataPK_V8)
        {
            var p = new DynamicParameters(orderDataPK_V8);
            var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(orderDataPK_V8.timestamp).DateTime;
            return _partitionService.Query<BettransDataPK_V8>(partitionTime, "Service_BettransPK_V8_orderId_Get", p);
        }

        #endregion

        #region MaxGame, SabaPlay, UuSlots, Live22, FastSpin

        public int PayoutPK_MaxGame(TpCustInfo tpCustInfo, PayoutData_MaxGame payoutData_MaxGame, int tpId, int gameType, WalletKind walletKind, TpCategory tpCategory, SpNameListOfMaxGame spName, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            var cnt = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                foreach (var betDataSet in payoutData_MaxGame.Payload.BetDataSet)
                {
                    var resettle = false;
                    int bonusId = 0;
                    var previousActualStake = 0m;
                    var previousPayout = 0m;
                    var memo = "Payout";
                    var originSeq = "";

                    var partitionTime = DateTimeOffset.FromUnixTimeSeconds(betDataSet.SettleDateUnix).DateTime;
                    var bettrans = _partitionService.Query<BettransData_MaxGame>(partitionTime, spName.GetTicketNo, new { TicketNo = betDataSet.TicketNo });
                    var placeBetActualStake = bettrans.Where(i => i.TicketType == BettransType.Bet).Sum(i => i.ActualStake);

                    // 檢查是否有下注該round
                    if (!bettrans.Any(i => i.TicketType == BettransType.Bet))
                    {
                        return -2;
                    }

                    // 檢查是否有派彩過
                    if (bettrans.Any(i => i.TicketType == BettransType.Payout))
                    {
                        var payoutDatas = bettrans.Where(i => i.TicketType == BettransType.Payout).ToList();
                        var payoutOfActualStake = bettrans.Where(i => i.TicketType == BettransType.Payout).Sum(i => i.ActualStake);

                        // 相同Seq & 相同TicketNo
                        if (payoutDatas.Any(i => i.Seq == payoutData_MaxGame.Seq))
                        {
                            return -1;
                        }

                        // 檢查派彩筆數
                        var seqs = payoutDatas.Select(i => i.Seq).ToList();
                        if (seqs.Count > 1)
                        {
                            return -3;
                        }

                        // 不同Seq & 相同TicketNo
                        if (!seqs.Contains(payoutData_MaxGame.Seq))
                        {
                            var origin = payoutDatas.FirstOrDefault();
                            originSeq = origin.Seq;
                            logger.LogInformation($"不同Seq & 相同TicketNo\r\nbettransData\r\n{JsonConvert.SerializeObject(bettrans)}request\r\n{JsonConvert.SerializeObject(betDataSet)}");

                            // 檢查金額
                            if (origin.ActualStake == betDataSet.ActualStake && origin.Payout == betDataSet.Payout)
                            {
                                // 金額相同 - 不處理
                                cnt++;
                                result = 1;
                                continue;
                            }
                            else
                            {
                                resettle = true;
                                // 金額不同 - 準備更新Seq、計算金額
                                if (origin.ActualStake != betDataSet.ActualStake)
                                {
                                    // 原本ActualStake差額(bettrans TicketType = 4 的 ActualStake)，原退金額扣回、原扣金額加回
                                    previousActualStake = (placeBetActualStake - payoutOfActualStake) * -1;
                                }
                                if (origin.Payout != betDataSet.Payout)
                                {
                                    // 原本差額(bettrans TicketType = 4 的 Payout)，原派金額扣回
                                    previousPayout = origin.Payout * -1;
                                }
                            }
                        }
                    }

                    var roundId = string.IsNullOrEmpty(betDataSet.ResultId) ? "" : betDataSet.ResultId;

                    if (resettle)
                    {
                        // 已有派彩過的不同Seq & 相同TicketNo & 金額不同
                        memo = "Resettle";
                        var resettleParam = new DynamicParameters(new
                        {
                            OriginSeq = originSeq,
                            Seq = payoutData_MaxGame.Seq,
                            TicketNo = betDataSet.TicketNo,
                            ActualStake = betDataSet.ActualStake,
                            NetStake = betDataSet.NetStake,
                            Payout = betDataSet.Payout,
                            GameResult = originSeq,// 更新存放原本派彩Seq，可透過BettransPK_MaxGame_Payload取得原資料
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Payout,
                        });
                        result = _partitionService.Execute(partitionTime, spName.SetAmount, resettleParam, trans);
                    }
                    else
                    {
                        // 未有派彩過
                        var p = new DynamicParameters(new
                        {
                            Seq = payoutData_MaxGame.Seq,
                            OperatorCustId = payoutData_MaxGame.Payload.OperatorCustId,
                            GameType = payoutData_MaxGame.Payload.GameType,
                            IpAddress = "",
                            SettleDate = betDataSet.SettleDate,
                            TicketNo = betDataSet.TicketNo,
                            GameId = betDataSet.GameId,
                            GameRoundId = betDataSet.GameRoundId,
                            TransactionId = string.IsNullOrEmpty(betDataSet.TransactionId) ? "" : betDataSet.TransactionId,
                            Stake = 0m,
                            ActualStake = betDataSet.ActualStake,
                            NetStake = betDataSet.NetStake,
                            Payout = betDataSet.Payout,
                            Odds = betDataSet.Odds,
                            BetChoice = "",
                            TableCode = "",
                            GameResult = string.IsNullOrEmpty(betDataSet.GameResult) ? "" : betDataSet.GameResult,
                            CurrencyISO = "",
                            BonusId = "",
                            BonusName = "",
                            PrizeName = "",
                            ResultId = roundId,
                            OtherBetInfo = "{}",
                            FreeBet = false,
                            FreeSpin = false,
                            ContributionAmount = 0,
                            ContributionRate = 0,
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Payout,
                            TicketStatus = (int)TicketStatus.UnSettled,
                            SettleFlag = (int)SettleFlag.EndGame
                        });

                        p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, spName.InsertBettrans, p, trans);
                    }

                    // 檢查差額: 使用ActualStake當作該局的實際扣款金額，如果跟前面收到的PlaceBet下注金額加總後不符合，需要進行Refund
                    var diffActualStake = (placeBetActualStake - betDataSet.ActualStake) + previousActualStake;
                    if (diffActualStake != 0)
                    {
                        // 差額:扣款
                        if (diffActualStake < 0)
                        {
                            if (walletKind == WalletKind.BonusWallet)
                            {
                                result = PayoutV2(
                                    tpCustInfo,
                                    Math.Abs(diffActualStake) * -1,
                                    tpId,
                                    betDataSet.TicketNo,
                                    gameType,
                                    tpCategory,
                                    betDataSet.TicketNo,
                                    TransType.Wagers,
                                    null,
                                    out afterAmount,
                                    $"Stake",
                                    out bonusId,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }
                            else
                            {
                                result = walletService.SetBalance(
                                    tpCustInfo.CustId,
                                    tpCustInfo.SiteId,
                                    tpCustInfo.UserName,
                                    Math.Abs(diffActualStake) * -1,
                                    tpId,
                                    TransType.Wagers,
                                    $"Stake : {Math.Abs(diffActualStake):#,##0.##} {betDataSet.ActualStake}",
                                    betDataSet.TicketNo,
                                    out afterAmount,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }

                        }
                        // 差額:退款
                        if (diffActualStake > 0)
                        {
                            if (walletKind == WalletKind.BonusWallet)
                            {
                                result = PayoutV2(
                                    tpCustInfo,
                                    Math.Abs(diffActualStake),
                                    tpId,
                                    betDataSet.TicketNo,
                                    gameType,
                                    tpCategory,
                                    betDataSet.TicketNo,
                                    TransType.Refunds,
                                    null,
                                    out afterAmount,
                                    $"Refund",
                                    out bonusId,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }
                            else
                            {
                                result = walletService.SetBalance(
                                    tpCustInfo.CustId,
                                    tpCustInfo.SiteId,
                                    tpCustInfo.UserName,
                                    Math.Abs(diffActualStake),
                                    tpId,
                                    TransType.Refunds,
                                    $"Refund: {Math.Abs(diffActualStake):#,##0.##}",
                                    betDataSet.TicketNo,
                                    out afterAmount,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }
                        }
                    }

                    // 注單派彩
                    var reqPayout = betDataSet.Payout + previousPayout;
                    if (result > 0 && reqPayout != 0)
                    {
                        if (walletKind == WalletKind.BonusWallet)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                reqPayout,
                                tpId,
                                betDataSet.TicketNo,
                                gameType,
                                tpCategory,
                                betDataSet.TicketNo,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                memo,
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }
                        else
                        {
                            result = walletService.SetBalance(
                                tpCustInfo.CustId,
                                tpCustInfo.SiteId,
                                tpCustInfo.UserName,
                                reqPayout,
                                tpId,
                                TransType.Payout,
                                $"{memo}: {reqPayout:#,##0.##}",
                                betDataSet.TicketNo,
                                out afterAmount,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }

                    }

                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle((Tp)tpId, (GameType)gameType, betDataSet.TicketNo, trans, partitionTime);
                    }

                    if (result > 0)
                    {
                        cnt++;
                    }
                }

                // 正常與重結都會寫入新的Payload資料
                DateTime.TryParse(payoutData_MaxGame.Timestamp, out DateTime ts);
                var payload = new DynamicParameters(new
                {
                    Timestamp = payoutData_MaxGame.Timestamp,
                    Seq = payoutData_MaxGame.Seq,
                    Payload = JsonConvert.SerializeObject(payoutData_MaxGame.Payload)
                });
                var req = _partitionService.Execute(ts, spName.InsertPayload, payload, trans);

                if (req > 0 && result > 0 && cnt != 0 && cnt == payoutData_MaxGame.Payload.BetDataSet.Count)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int CancelBetPK_MaxGame(TpCustInfo tpCustInfo, CancelBetData_MaxGame cancelBetData_MaxGame, int tpId, int gameType, WalletKind walletKind, TpCategory tpCategory, SpNameListOfMaxGame spName, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            var cnt = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                DateTime.TryParse(cancelBetData_MaxGame.Timestamp, out DateTime ts);
                var partitionTime = ts;
                var unix = ((DateTimeOffset)ts).ToUnixTimeSeconds();
                foreach (var betDataSet in cancelBetData_MaxGame.Payload.BetDataSet)
                {
                    int bonusId = 0;
                    var p = new DynamicParameters(new
                    {
                        Seq = cancelBetData_MaxGame.Seq,
                        OperatorCustId = cancelBetData_MaxGame.Payload.OperatorCustId,
                        GameType = cancelBetData_MaxGame.Payload.GameType,
                        IpAddress = "",
                        SettleDate = partitionTime,
                        TicketNo = betDataSet.TicketNo,
                        GameId = betDataSet.GameId,
                        GameRoundId = string.IsNullOrEmpty(betDataSet.GameRoundId) ? "" : betDataSet.GameRoundId,
                        TransactionId = string.IsNullOrEmpty(betDataSet.TransactionId) ? "" : betDataSet.TransactionId,
                        Stake = 0m,
                        ActualStake = 0m,
                        NetStake = 0m,
                        Payout = 0m,
                        Odds = 0m,
                        BetChoice = "",
                        TableCode = "",
                        GameResult = "",
                        CurrencyISO = "",
                        BonusId = "",
                        BonusName = "",
                        PrizeName = "",
                        ResultId = "",
                        OtherBetInfo = "{}",
                        FreeBet = false,
                        FreeSpin = false,
                        ContributionAmount = 0,
                        ContributionRate = 0,
                        SettleDateUnix = unix,
                        TicketType = (int)BettransType.CancelBet,
                        TicketStatus = (int)TicketStatus.Refund,
                        SettleFlag = (int)SettleFlag.Settled
                    });

                    // 檢查是否有下注
                    var bettrans = _partitionService.Query<BettransData_MaxGame>(partitionTime, spName.GetTicketNo, new { TicketNo = betDataSet.TicketNo });
                    if (!bettrans.Any(i => i.TicketType == BettransType.Bet))
                    {
                        // insert cancel
                        p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, spName.InsertBettrans, p, trans);
                    }
                    else if (bettrans.Any(i => i.TicketType == BettransType.CancelBet))
                    {
                        var cancelDatas = bettrans.Where(i => i.TicketType == BettransType.CancelBet).ToList();
                        // 相同Seq & 相同TicketNo
                        if (cancelDatas.Any(i => i.Seq == cancelBetData_MaxGame.Seq))
                        {
                            return -1;
                        }

                        var seqs = cancelDatas.Select(i => i.Seq).ToList();
                        // 不同Seq & 相同TicketNo
                        if (!seqs.Contains(cancelBetData_MaxGame.Seq))
                        {
                            // 因對方沒有傳送退款金額，照理進到退款流程是不可在接續下注
                            return -1;
                        }
                    }
                    else
                    {
                        // VoidGame就是以TicketNo將整局的PlaceBet 注單取消並退款
                        var placeBetActualStake = bettrans.Where(i => i.TicketType == BettransType.Bet).Sum(i => i.ActualStake);
                        result = _partitionService.Execute(partitionTime, spName.SetCancelBet, p, trans);

                        if (result > 0 && placeBetActualStake != 0)
                        {
                            if (walletKind == WalletKind.BonusWallet)
                            {
                                result = PayoutV2(
                                    tpCustInfo,
                                    Math.Abs(placeBetActualStake),
                                    tpId,
                                    betDataSet.TicketNo,
                                    gameType,
                                    tpCategory,
                                    betDataSet.TicketNo,
                                    TransType.Refunds,
                                    null,
                                    out _,
                                    "CancelBet",
                                    out bonusId,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }
                            else
                            {
                                result = walletService.SetBalance(
                                    tpCustInfo.CustId,
                                    tpCustInfo.SiteId,
                                    tpCustInfo.UserName,
                                    Math.Abs(placeBetActualStake),
                                    tpId,
                                    TransType.Refunds,
                                    $"Cancel: {Math.Abs(placeBetActualStake):#,##0.##}",
                                    betDataSet.TicketNo,
                                    out afterAmount,
                                    conn,
                                    trans,
                                    AllowNegative.Allow
                                );
                            }
                        }
                    }

                    if (result > 0)
                    {
                        cnt++;
                    }
                }
                var payload = new DynamicParameters(new
                {
                    Timestamp = cancelBetData_MaxGame.Timestamp,
                    Seq = cancelBetData_MaxGame.Seq,
                    Payload = JsonConvert.SerializeObject(cancelBetData_MaxGame.Payload)
                });
                var req = _partitionService.Execute(partitionTime, spName.InsertPayload, payload, trans);

                if (req > 0 && result > 0 && cnt != 0 && cnt == cancelBetData_MaxGame.Payload.BetDataSet.Count)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int BonusPK_MaxGame(TpCustInfo tpCustInfo, BonusData_MaxGame bonusData_MaxGame, int tpId, int gameType, WalletKind walletKind, TpCategory tpCategory, SpNameListOfMaxGame spName, out decimal afterAmount, out string refNo)
        {
            var result = 0;
            afterAmount = 0;
            var cnt = 0;
            refNo = string.Empty;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                foreach (var betDataSet in bonusData_MaxGame.Payload.BetDataSet)
                {
                    var resettle = false;
                    var previousPayout = 0m;
                    int bonusId = 0;
                    var memo = "Payout";
                    var originSeq = "";

                    var partitionTime = DateTimeOffset.FromUnixTimeSeconds(betDataSet.SettleDateUnix).DateTime;
                    // 檢查是否重複
                    var bettrans = _partitionService.Query<BettransData_MaxGame>(partitionTime, spName.GetTicketNo, new { TicketNo = betDataSet.TicketNo });
                    if (bettrans.Any(i => i.TicketType == BettransType.Bonus))
                    {
                        var bonusDatas = bettrans.Where(i => i.TicketType == BettransType.Bonus).ToList();
                        // 相同Seq & 相同TicketNo
                        if (bonusDatas.Any(i => i.Seq == bonusData_MaxGame.Seq))
                        {
                            return -1;
                        }

                        var seqs = bonusDatas.Select(i => i.Seq).ToList();
                        // 不同Seq & 相同TicketNo
                        if (!seqs.Contains(bonusData_MaxGame.Seq))
                        {
                            var origin = bonusDatas.FirstOrDefault();
                            originSeq = origin.Seq;
                            refNo = origin.SQ.ToString();

                            // 檢查金額
                            if (origin.Payout == betDataSet.Payout)
                            {
                                // 金額相同 - 不處理
                                cnt++;
                                result = 1;
                                continue;
                            }
                            else
                            {
                                resettle = true;

                                // 金額不同 - 準備更新Seq、計算金額
                                previousPayout = origin.Payout * -1;
                            }
                        }
                    }

                    // 先扣回原派金額，再加上本次金額
                    var reqPayout = (previousPayout) + betDataSet.Payout;

                    if (resettle)
                    {
                        // 已有派彩過的不同Seq & 相同TicketNo & 金額不同
                        memo = "Resettle";
                        var resettleParam = new DynamicParameters(new
                        {
                            OriginSeq = originSeq,
                            Seq = bonusData_MaxGame.Seq,
                            TicketNo = betDataSet.TicketNo,
                            ActualStake = 0m,
                            NetStake = 0m,
                            Payout = betDataSet.Payout,
                            GameResult = originSeq,// 更新存放原本派彩Seq，可透過BettransPK_MaxGame_Payload取得原資料
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Bonus,
                        });
                        result = _partitionService.Execute(partitionTime, spName.SetAmount, resettleParam, trans);
                    }
                    else
                    {
                        // 未有派彩過
                        var p = new DynamicParameters(new
                        {
                            Seq = bonusData_MaxGame.Seq,
                            OperatorCustId = bonusData_MaxGame.Payload.OperatorCustId,
                            GameType = bonusData_MaxGame.Payload.GameType,
                            IpAddress = "",
                            SettleDate = betDataSet.SettleDate,
                            TicketNo = betDataSet.TicketNo,
                            GameId = betDataSet.GameId,
                            GameRoundId = betDataSet.GameRoundId,
                            TransactionId = string.IsNullOrEmpty(betDataSet.TransactionId) ? "" : betDataSet.TransactionId,
                            Stake = 0m,
                            ActualStake = 0m,
                            NetStake = 0m,
                            Payout = betDataSet.Payout,
                            Odds = 0m,
                            BetChoice = "",
                            TableCode = "",
                            GameResult = "",
                            CurrencyISO = betDataSet.CurrencyISO,
                            BonusId = betDataSet.BonusId,
                            BonusName = betDataSet.BonusName,
                            PrizeName = betDataSet.PrizeName,
                            ResultId = "",
                            OtherBetInfo = "{}",
                            FreeBet = false,
                            FreeSpin = false,
                            ContributionAmount = 0,
                            ContributionRate = 0,
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Bonus,
                            TicketStatus = (int)TicketStatus.UnSettled,
                            SettleFlag = (int)SettleFlag.EndGame
                        });

                        p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, spName.InsertBettrans, p, trans);
                        refNo = p.Get<long>("SQ").ToString();
                    }

                    if (result > 0 && reqPayout != 0)
                    {
                        if (walletKind == WalletKind.BonusWallet)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                reqPayout,
                                tpId,
                                betDataSet.TicketNo,
                                gameType,
                                tpCategory,
                                betDataSet.TicketNo,
                                TransType.Prize,
                                null,
                                out afterAmount,
                                memo,
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }
                        else
                        {
                            result = walletService.SetBalance(
                                tpCustInfo.CustId,
                                tpCustInfo.SiteId,
                                tpCustInfo.UserName,
                                reqPayout,
                                tpId,
                                TransType.Prize,
                                $"{memo}: {reqPayout:#,##0.##}",
                                betDataSet.TicketNo,
                                out afterAmount,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }
                    }
                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle((Tp)tpId, (GameType)gameType, betDataSet.TicketNo, trans, partitionTime);
                    }

                    if (result > 0)
                    {
                        cnt++;
                    }
                }

                // 正常與重結都會寫入新的Payload資料
                DateTime.TryParse(bonusData_MaxGame.Timestamp, out DateTime ts);
                var payload = new DynamicParameters(new
                {
                    Timestamp = bonusData_MaxGame.Timestamp,
                    Seq = bonusData_MaxGame.Seq,
                    Payload = JsonConvert.SerializeObject(bonusData_MaxGame.Payload)
                });
                var req = _partitionService.Execute(ts, spName.InsertPayload, payload, trans);

                if (req > 0 && result > 0 && cnt != 0 && cnt == bonusData_MaxGame.Payload.BetDataSet.Count)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int FreeSpinPK_MaxGame(TpCustInfo tpCustInfo, FreeSpinData_MaxGame freeSpinData_MaxGame, int tpId, int gameType, WalletKind walletKind, TpCategory tpCategory, SpNameListOfMaxGame spName, out decimal afterAmount, out List<BetDataSetList_MaxGame> refData)
        {
            afterAmount = 0;
            var result = 0;
            var cnt = 0;
            refData = new List<BetDataSetList_MaxGame>();
            var refNo = string.Empty;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            /*
             FreeSpin的設計是一些特殊廠商，每一次的畫面都會是一個獨立的TicketNo，需要直接給錢的功能
             Stake & ActualStake & NetStake 都會為0 , Payout為該局的贏
             */
            try
            {
                foreach (var betDataSet in freeSpinData_MaxGame.Payload.BetDataSet)
                {
                    var resettle = false;
                    var previousPayout = 0m;
                    int bonusId = 0;
                    var memo = "FreeSpin";
                    var originSeq = "";

                    var partitionTime = DateTimeOffset.FromUnixTimeSeconds(betDataSet.SettleDateUnix).DateTime;
                    // 檢查是否重複
                    var bettrans = _partitionService.Query<BettransData_MaxGame>(partitionTime, spName.GetTicketNo, new { TicketNo = betDataSet.TicketNo });
                    if (bettrans.Any(i => i.TicketType == BettransType.Payout && i.Seq == freeSpinData_MaxGame.Seq && i.GameRoundId == betDataSet.GameRoundId))
                    {
                        // GameRoundId都會不同
                        return -1;
                    }

                    // GameRoundId都會不同
                    if (bettrans.Any(i => i.TicketType == BettransType.Payout && i.GameRoundId == betDataSet.GameRoundId))
                    {
                        var freeDatas = bettrans.Where(i => i.TicketType == BettransType.Payout && i.GameRoundId == betDataSet.GameRoundId).ToList();
                        // 相同Seq & 相同TicketNo
                        if (freeDatas.Any(i => i.Seq == freeSpinData_MaxGame.Seq))
                        {
                            return -1;
                        }
                        else
                        {
                            // 不同Seq & 相同TicketNo
                            var seqs = freeDatas.Select(i => i.Seq).ToList();
                            if (!seqs.Contains(freeSpinData_MaxGame.Seq))
                            {
                                var origin = freeDatas.FirstOrDefault();
                                originSeq = origin.Seq;
                                refNo = origin.SQ.ToString();

                                // 檢查金額
                                if (origin.Payout == betDataSet.Payout)
                                {
                                    // 金額相同 - 不處理
                                    cnt++;
                                    result = 1;
                                    continue;
                                }
                                else
                                {
                                    resettle = true;

                                    // 金額不同 - 準備更新Seq、計算金額
                                    previousPayout = origin.Payout * -1;
                                }
                            }
                        }
                    }

                    // 先扣回原派金額，再加上本次金額
                    var reqPayout = (previousPayout) + betDataSet.Payout;

                    if (resettle)
                    {
                        // 已有派彩過的不同Seq & 相同TicketNo & 金額不同
                        memo = "Resettle";
                        var resettleParam = new DynamicParameters(new
                        {
                            OriginSeq = originSeq,
                            Seq = freeSpinData_MaxGame.Seq,
                            TicketNo = betDataSet.TicketNo,
                            ActualStake = 0m,
                            NetStake = 0m,
                            Payout = betDataSet.Payout,
                            GameResult = originSeq,// 更新存放原本派彩Seq，可透過BettransPK_MaxGame_Payload取得原資料
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Payout,
                        });
                        result = _partitionService.Execute(partitionTime, spName.SetAmount, resettleParam, trans);
                    }
                    else
                    {
                        var p = new DynamicParameters(new
                        {
                            Seq = freeSpinData_MaxGame.Seq,
                            OperatorCustId = freeSpinData_MaxGame.Payload.OperatorCustId,
                            GameType = freeSpinData_MaxGame.Payload.GameType,
                            IpAddress = "",
                            SettleDate = betDataSet.SettleDate,
                            TicketNo = betDataSet.TicketNo,
                            GameId = betDataSet.GameId,
                            GameRoundId = betDataSet.GameRoundId,
                            TransactionId = string.IsNullOrEmpty(betDataSet.TransactionId) ? "" : betDataSet.TransactionId,
                            Stake = 0m,
                            ActualStake = 0m,
                            NetStake = 0m,
                            Payout = betDataSet.Payout,
                            Odds = 0m,
                            BetChoice = "",
                            TableCode = "",
                            GameResult = "",
                            CurrencyISO = "",
                            BonusId = "",
                            BonusName = "",
                            PrizeName = "",
                            ResultId = "",
                            OtherBetInfo = "{}",
                            FreeBet = false,
                            FreeSpin = false,
                            ContributionAmount = 0,
                            ContributionRate = 0,
                            SettleDateUnix = betDataSet.SettleDateUnix,
                            TicketType = (int)BettransType.Payout,
                            TicketStatus = (int)TicketStatus.UnSettled,
                            SettleFlag = (int)SettleFlag.EndGame
                        });

                        p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                        result = _partitionService.Execute(partitionTime, spName.InsertBettrans, p, trans);
                        refNo = p.Get<long>("SQ").ToString();
                    }

                    if (result > 0 && reqPayout != 0)
                    {
                        if (walletKind == WalletKind.BonusWallet)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                betDataSet.Payout,
                                tpId,
                                betDataSet.TicketNo,
                                gameType,
                                tpCategory,
                                betDataSet.TicketNo,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                memo,
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }
                        else
                        {
                            result = walletService.SetBalance(
                                tpCustInfo.CustId,
                                tpCustInfo.SiteId,
                                tpCustInfo.UserName,
                                betDataSet.Payout,
                                tpId,
                                TransType.Payout,
                                $"{memo}: {reqPayout:#,##0.##}",
                                betDataSet.TicketNo,
                                out afterAmount,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                        }
                    }
                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle((Tp)tpId, (GameType)gameType, betDataSet.TicketNo, trans, partitionTime);
                    }

                    if (result > 0)
                    {
                        refData.Add(new BetDataSetList_MaxGame
                        {
                            TicketNo = betDataSet.TicketNo,
                            RefNo = refNo,
                        });

                        cnt++;
                    }
                }

                // 正常與重結都會寫入新的Payload資料
                DateTime.TryParse(freeSpinData_MaxGame.Timestamp, out DateTime ts);
                var payload = new DynamicParameters(new
                {
                    Timestamp = freeSpinData_MaxGame.Timestamp,
                    Seq = freeSpinData_MaxGame.Seq,
                    Payload = JsonConvert.SerializeObject(freeSpinData_MaxGame.Payload)
                });
                var req = _partitionService.Execute(ts, spName.InsertPayload, payload, trans);

                if (req > 0 && result > 0 && cnt != 0 && cnt == freeSpinData_MaxGame.Payload.BetDataSet.Count)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        #endregion

        #region KY

        public int PayoutPK_KY(TpCustInfo tpCustInfo, BettingDataPK_KY bettingDataPK_KY, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(bettingDataPK_KY);
                p.Add("TicketType", (int)BettransType.Payout);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);
                p.Add("SettleFlag", (int)SettleFlag.Running);

                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataPK_KY.timestamp).DateTime;

                var bettrans = _partitionService.Query<BettransDataPK_KY>(partitionTime, "Service_BettransPK_KY_gameNo_Get", new { gameNo = bettingDataPK_KY.gameNo });

                result = _partitionService.Execute(partitionTime, "Service_BettransPK_KY_Ins", p, trans);

                if (result > 0 && bettingDataPK_KY.money != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataPK_KY.money,
                        (int)Tp.KY,
                        TransType.Payout,
                        $"Payout: {bettingDataPK_KY.money:#,##0.##} {bettingDataPK_KY.orderId}",
                        bettingDataPK_KY.gameNo,
                        out afterAmount,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int CancelBetPK_KY(TpCustInfo tpCustInfo, BettingDataPK_KY bettingDataPK_KY, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(bettingDataPK_KY);
                p.Add("TicketType", (int)BettransType.CancelBet);
                p.Add("TicketStatus", (int)TicketStatus.Refund);
                p.Add("SettleFlag", (int)SettleFlag.Settled);

                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataPK_KY.timestamp).DateTime;
                result = _partitionService.Execute(partitionTime, "Service_BettransPK_KY_CancelBet_Set", p, trans);
                /*
                 贵方收到通知后需先判断原始订单状态是否成功
                 若原始订单为成功状态时，方能取消下注並修改訂單狀態
                 若订单不存在，请先塞入资料库并记录为取消订单，此订单状态将不可再修改。
                */
                if (result > 0)
                {
                    // cancel update
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataPK_KY.money,
                        (int)Tp.KY,
                        TransType.Refunds,
                        $"Cancel: {bettingDataPK_KY.money:#,##0.##} {bettingDataPK_KY.orderId}",
                        bettingDataPK_KY.gameNo,
                        out afterAmount,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    // insert cancel
                    var cancelInsert = _partitionService.Execute(partitionTime, "Service_BettransPK_KY_Ins", p, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }
        
        public IEnumerable<BettransDataPK_KY> GetRecordPK_KY(OrderDataPK_KY orderDataPK_KY)
        {
            var p = new DynamicParameters(orderDataPK_KY);
            var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(orderDataPK_KY.timestamp).DateTime;
            return _partitionService.Query<BettransDataPK_KY>(partitionTime, "Service_BettransPK_KY_orderId_Get", p);
        }

        #endregion
    }
}
