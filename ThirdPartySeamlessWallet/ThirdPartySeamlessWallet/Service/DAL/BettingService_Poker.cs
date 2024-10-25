using System.Data;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_OneGaming_Ins", p, conn, trans);

                if (p.Get<int>("result") == 1)
                {
                    result = _walletService.SetBalance(
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
                _mainDbClient.CommitAndDisposeConnection(conn, trans);

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

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_AMB_Bet_Ins", p, conn, trans);

                if (result > 0)
                {
                    result = _walletService.SetBalance(
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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = debitTransaction.PtxId,
                userId = debitTransaction.UserId,
                amount = debitTransaction.amt,
                currency = debitTransaction.cur,
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
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await _mainDbClient.ExecuteAsync("Service_BettransPK_KM_Debit_Ins", trans, p);
                result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        _logger.LogInformation("訂單已存在");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }

                    _logger.LogError("exec Service_BettransPK_KM_Debit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                var afterAmount = 0M;
                if (debitTransaction.amt != 0)
                {
                    result = _walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(debitTransaction.amt) * -1,
                        (int)Tp.KM,
                        TransType.Wagers,
                        $"Stake: {Math.Abs(debitTransaction.amt):#,##0.##}",
                        debitTransaction.PtxId,
                        out afterAmount,
                        conn,
                        trans
                    );
                    if (result != 1)
                    {
                        await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                        _logger.LogError($"PlaceBetPK_KM result:{result}, afterAmount:{afterAmount}");
                        if (result == 0 && afterAmount < 0)
                        {
                            throw new KmException(KMErrorCode.InsufficientFunds);
                        }

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
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"exec PlaceBetPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        public async Task<KmDbResult> PlaceBetNoSetBalance_KM(KMDebitRequestTransaction debitTransaction)
        {
            var result = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            var p = new DynamicParameters(new
            {
                ptxid = debitTransaction.PtxId,
                userId = debitTransaction.UserId,
                amount = debitTransaction.amt,
                currency = debitTransaction.cur,
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
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });
            p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

            try
            {
                await _mainDbClient.ExecuteAsync("Service_BettransPK_KM_Debit_Ins", trans, p);
                result = p.Get<short>("SPResult");
                var trxId = p.Get<long>("trx_id");
                if (result != 1)
                {
                    if (result == 0)
                    {
                        _logger.LogInformation($"訂單已存在:{trxId}");
                        return new KmDbResult() { SPResult = result, Trx_id = trxId };
                    }

                    _logger.LogError("exec Service_BettransPK_KM_Debit_Ins error");
                    throw new KmException(KMErrorCode.SystemError);
                }

                await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                return new KmDbResult()
                {
                    SPResult = result,
                    Trx_id = trxId,
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"exec PlaceBetPK_KM failed: {e.Message}");
                await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                throw new KmException(KMErrorCode.SystemError);
            }
        }

        #endregion

        #region MP

        public int MoneyOutPK_MP(TpCustInfo tpCustInfo, TransferData_MP transferData_MP, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var amount = transferData_MP.requestAmount * -1;

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
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransPK_MP_Ins_V2", p, conn, trans);

                if (result > 0 && amount != 0)
                {
                    result = _walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.MP,
                        TransType.Wagers,
                        $"Transfer Out {transferData_MP.roundId} : {transferData_MP.requestAmount:#,##0.##}",
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

        #region V8

        public int PlaceBetPK_V8(TpCustInfo tpCustInfo, BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(bettingDataPK_V8);
                p.Add("TicketType", (int)BettransType.Bet);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);
                p.Add("SettleFlag", (int)SettleFlag.Running);

                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataPK_V8.timestamp).DateTime;
                result = _partitionService.Execute(partitionTime, "Service_BettransPK_V8_Ins", p, trans);

                if (result > 0 && bettingDataPK_V8.money != 0)
                {
                    result = _walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(bettingDataPK_V8.money) * -1,
                        (int)Tp.V8,
                        TransType.Wagers,
                        $"Stake: {bettingDataPK_V8.money:#,##0.##} {bettingDataPK_V8.orderId}",
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
                _logger.LogError(mySqlEx.Message);
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

        #region MaxGame, SabaPlay, UuSlots, Live22, FastSpin

        public int PlaceBetPK_MaxGame(TpCustInfo tpCustInfo, BettingData_MaxGame bettingData_MaxGame, int tpId, int gameType, WalletKind walletKind, SpNameListOfMaxGame spName, out decimal afterAmount, out List<BetDataSetList_MaxGame> refData)
        {
            afterAmount = 0;
            var result = 0;
            var cnt = 0;
            refData = new List<BetDataSetList_MaxGame>();

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                foreach (var betDataSet in bettingData_MaxGame.Payload.BetDataSet)
                {
                    int bonusId = 0;
                    DateTime.TryParse(bettingData_MaxGame.Timestamp, out DateTime partitionTime);
                    // 檢查是否重複
                    var bettrans = _partitionService.Query<BettransData_MaxGame>(partitionTime, spName.GetTicketNo, new { TicketNo = betDataSet.TicketNo });
                    if (bettrans.Any(i => i.TicketType == BettransType.Bet && i.Seq == bettingData_MaxGame.Seq))
                    {
                        return -1;
                    }

                    var otherBetInfo = betDataSet.OtherBetInfo == null ? "{}" : JsonConvert.SerializeObject(betDataSet.OtherBetInfo);
                    var p = new DynamicParameters(new
                    {
                        Seq = bettingData_MaxGame.Seq,
                        OperatorCustId = bettingData_MaxGame.Payload.OperatorCustId,
                        GameType = bettingData_MaxGame.Payload.GameType,
                        IpAddress = string.IsNullOrEmpty(bettingData_MaxGame.Payload.IpAddress) ? "" : bettingData_MaxGame.Payload.IpAddress,
                        SettleDate = new DateTime(1970, 1, 1),
                        TicketNo = betDataSet.TicketNo,
                        GameId = betDataSet.GameId,
                        GameRoundId = betDataSet.GameRoundId,
                        TransactionId = string.IsNullOrEmpty(betDataSet.TransactionId) ? "" : betDataSet.TransactionId,
                        Stake = betDataSet.Stake,
                        ActualStake = betDataSet.ActualStake,
                        NetStake = 0m,
                        Payout = 0m,
                        Odds = betDataSet.Odds,
                        BetChoice = string.IsNullOrEmpty(betDataSet.BetChoice) ? "" : betDataSet.BetChoice,
                        TableCode = string.IsNullOrEmpty(betDataSet.TableCode) ? "" : betDataSet.TableCode,
                        GameResult = "",
                        CurrencyISO = "",
                        BonusId = "",
                        BonusName = "",
                        PrizeName = "",
                        ResultId = "",
                        OtherBetInfo = otherBetInfo,
                        FreeBet = betDataSet.OtherBetInfo == null ? false : betDataSet.OtherBetInfo.FreeBet,
                        FreeSpin = betDataSet.OtherBetInfo == null ? false : betDataSet.OtherBetInfo.FreeSpin,
                        ContributionAmount = betDataSet.OtherBetInfo == null ? 0 : betDataSet.OtherBetInfo.ContributionAmount,
                        ContributionRate = betDataSet.OtherBetInfo == null ? 0 : betDataSet.OtherBetInfo.ContributionRate,
                        SettleDateUnix = 0,
                        TicketType = (int)BettransType.Bet,
                        TicketStatus = (int)TicketStatus.UnSettled,
                        SettleFlag = (int)SettleFlag.Running,
                    });
                    p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                    result = _partitionService.Execute(partitionTime, spName.InsertBettrans, p, trans);

                    if (result > 0 && betDataSet.ActualStake != 0)
                    {
                        if (walletKind == WalletKind.BonusWallet)
                        {
                            result = PlaceBet(
                                tpCustInfo,
                                Math.Abs(betDataSet.ActualStake) * -1,
                                tpId,
                                betDataSet.TicketNo,
                                gameType,
                                betDataSet.TicketNo,
                                out afterAmount,
                                ref bonusId,
                                out _,
                                conn,
                                trans
                            );
                        }
                        else
                        {
                            result = _walletService.SetBalance(
                                tpCustInfo.CustId,
                                tpCustInfo.SiteId,
                                tpCustInfo.UserName,
                                Math.Abs(betDataSet.ActualStake) * -1,
                                tpId,
                                TransType.Wagers,
                                $"Stake: {betDataSet.ActualStake:#,##0.##}",
                                betDataSet.TicketNo,
                                out afterAmount,
                                conn,
                                trans
                            );
                        }
                    }

                    if (result > 0)
                    {
                        var refNo = p.Get<long>("SQ").ToString();
                        refData.Add(new BetDataSetList_MaxGame
                        {
                            TicketNo = betDataSet.TicketNo,
                            RefNo = refNo,
                        });

                        cnt++;
                    }
                }

                DateTime.TryParse(bettingData_MaxGame.Timestamp, out DateTime ts);
                var payload = new DynamicParameters(new
                {
                    Timestamp = bettingData_MaxGame.Timestamp,
                    Seq = bettingData_MaxGame.Seq,
                    Payload = JsonConvert.SerializeObject(bettingData_MaxGame.Payload)
                });
                var req = _partitionService.Execute(ts, spName.InsertPayload, payload, trans);

                if (req > 0 && result > 0 && cnt != 0 && cnt == bettingData_MaxGame.Payload.BetDataSet.Count)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                _logger.LogError(mySqlEx.Message);
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
    }
}