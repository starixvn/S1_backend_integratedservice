using System.Data;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using Ubiety.Dns.Core;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region SA

        public int CancelBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SA_LiveCasino;

            afterAmount = 0;
            int result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var refBettransLC = _mainDbClient.Execute("Service_CancelBetLC_SA_Set", new { Txnid = bettingDataLC_SA.txn_reverse_id }, conn, trans);

            if (refBettransLC > 0)
            {
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(bettingDataLC_SA.amount),
                    (int)Tp.SA,
                    bettingDataLC_SA.txn_reverse_id,
                    gameType,
                    TpCategory.LiveCasino,
                    $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}",
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "CancelBet",
                    out bonusId,
                    conn,
                    trans
                );

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
                var p = new DynamicParameters(new
                {
                    Txnid = bettingDataLC_SA.txnid,
                    TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}",
                    Currency = bettingDataLC_SA.currency,
                    Username = bettingDataLC_SA.username,
                    Gametype = bettingDataLC_SA.gametype,
                    Amount = bettingDataLC_SA.amount,
                    Gameid = bettingDataLC_SA.gameid,
                    TxnReverseId = bettingDataLC_SA.txn_reverse_id,
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });

                p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                _mainDbClient.Execute("Service_BettransLC_SA_CancelBet_Ins", p, conn, trans);
                var refLC = p.Get<int>("out_result");
                if (refLC == 2)
                {
                    // insert cancel record
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
                return -1;
            }

            return result;
        }
        public int PayoutLC_SA(TpCustInfo tpCustInfo, PayoutDataLC_SA payoutDataLC_SA, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SA_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            long payouttime = 0;
            payouttime = tools.ConvertLocTime(payoutDataLC_SA.Payouttime, 8);

            long timestamp = 0;
            timestamp = tools.ConvertLocTime(payoutDataLC_SA.timestamp, 8);

            int ticketStatus = (int)TicketStatus.Lost;

            if (payoutDataLC_SA.amount != 0)
            {
                ticketStatus = (int)TicketStatus.Win;
            }

            var p = new DynamicParameters(new
            {
                Txnid = payoutDataLC_SA.txnid,
                Currency = payoutDataLC_SA.currency,
                Username = payoutDataLC_SA.username,
                Gametype = payoutDataLC_SA.gametype,
                Amount = payoutDataLC_SA.amount,
                Timestamp = timestamp,
                Payouttime = payouttime,
                Gameid = payoutDataLC_SA.gameid,
                Hostid = payoutDataLC_SA.hostid,
                TicketStatus = ticketStatus
            });

            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_SA_Payout_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (payoutDataLC_SA.amount != 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, payoutDataLC_SA.amount, (int)Tp.SA, TransType.Settled, $"Payout: {payoutDataLC_SA.amount:#,##0.##}", $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, payoutDataLC_SA.amount, (int)Tp.SA, $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", gameType, TpCategory.LiveCasino, $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.Execute("Service_BettransLC_SAV2_Set", new { Username = payoutDataLC_SA.username, Gameid = payoutDataLC_SA.gameid, SettleFlag = 2 }, conn, trans);
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

        public SettleData_SA GetSettle_BettransLC_SA(string gameId, string userName)
        {
            return _mainDbClient.QueryFirst<SettleData_SA>("Service_BettransLC_SA_Settle_Get_V2", new DynamicParameters(new { GameId = gameId, UserName = userName }));
        }


        #endregion

        #region DG
        public int CancelBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.DG_LiveCasino;

            afterAmount = 0;
            int result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            result = _mainDbClient.Execute("Service_CancelBettransLC_DG_Set", new { data = bettingDataLC_DG.data }, conn, trans);
            if (result == 0)
            {
                return -1;
            }
            else
            {
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_DG.Amount), (int)Tp.DG, TransType.Refunds, $"Cancel: {Math.Abs(bettingDataLC_DG.Amount).ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                result = PayoutV2(tpCustInfo, Math.Abs(bettingDataLC_DG.Amount), (int)Tp.DG, bettingDataLC_DG.TransId, gameType, TpCategory.LiveCasino, bettingDataLC_DG.TransId, TransType.Refunds, null, out afterAmount, "CancelBet", out bonusId, conn, trans);
                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            return result;
        }
        public int PayoutLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.DG_LiveCasino;

            afterAmount = 0;
            TransferType transferType = TransferType.Payout;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { data = bettingDataLC_DG.data, TransId = bettingDataLC_DG.TransId, Username = bettingDataLC_DG.Username, Amount = bettingDataLC_DG.Amount, Memo = (string)null, TicketType = (int)transferType });

            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_DGV2_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (bettingDataLC_DG.Amount != 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_DG.Amount, (int)Tp.DG, TransType.Payout, $"Payout: {bettingDataLC_DG.Amount.ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataLC_DG.Amount, (int)Tp.DG, bettingDataLC_DG.TransId, gameType, TpCategory.LiveCasino, bettingDataLC_DG.TransId, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        //_mainDbClient.Execute("Service_BettransLC_DGV2_Set", new { TransId = bettingDataLC_DG.TransId, SettleFlag = 2 }, conn, trans);
                        var insertTpResults = _tpUnsettleService.InsertTpUnsettle(Tp.DG, GameType.DG_LiveCasino, bettingDataLC_DG.TransId, trans);
                        if (insertTpResults <= 0)
                        {
                            result = 0;
                        }
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
        public int Get_TpWagerDG_Settle(string TransId, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            var p = new DynamicParameters(new { TransId = TransId });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            p.Add("GameType", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("GameId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            int result = _wagerDbClient.Execute("Service_TpWagerDG_Settle_Get", commandType: CommandType.StoredProcedure, p);

            if (p.Get<decimal?>("Stake") != null)
            {
                stake = p.Get<decimal>("Stake");
                validBet = p.Get<decimal>("ValidBet");
                winLostAmount = p.Get<decimal>("WinLostAmount");
                var checkSettleTime = p.Get<dynamic>("SettleTime");
                if (checkSettleTime == null)
                {
                    int gameType = p.Get<int>("GameType");
                    int gameId = p.Get<int>("GameId");
                    //会员抢红包
                    if (gameType == 2 && gameId == 2)
                    {
                        return -1;
                    }
                }
                else
                {
                    settleTime = p.Get<long>("SettleTime");
                }

            }
            else
            {
                result = 0;
            }

            return result;
        }
        public IEnumerable<BettingDataLC_DG> GetUnsettledListLC_DG()
        {
            return _mainDbClient.Query<BettingDataLC_DG>("Service_BettransLC_DG_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_TpWagerDG(string transId)
        {
            long.TryParse(transId, out long lTransId);
            return _wagerDbClient.Execute("Service_TpWagerDG_Settle_Set", CommandType.StoredProcedure, new { TransId = lTransId });
        }
        public int SetSettleFlag_DG(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransLC_DGV2_Set", CommandType.StoredProcedure, new { TransId = TransId, SettleFlag = 1 });
        }

        public SettleData_DG GetSettle_TpWagerDG(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData_DG>("Service_TpWagerDG_Settle_Get_V2", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        #endregion

        #region AG
        public int PayoutLC_AG(TpCustInfo tpCustInfo, PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AG_LiveCasino;

            afterAmount = 0;
            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);

            try
            {
                var p = new DynamicParameters(new
                {
                    billNo = payoutDataLC_AG.BillNo,
                    transactionID = payoutDataLC_AG.TransactionID,
                    transactionIDs = JsonConvert.SerializeObject(payoutDataLC_AG.TransactionIDs),
                    currency = payoutDataLC_AG.Currency,
                    netAmount = payoutDataLC_AG.NetAmount,
                    validBetAmount = payoutDataLC_AG.ValidBetAmount,
                    playname = payoutDataLC_AG.Playname,
                    agentCode = payoutDataLC_AG.AgentCode,
                    settletime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4),
                    gametype = payoutDataLC_AG.Gametype,
                    gameCode = payoutDataLC_AG.GameCode,
                    transactionType = payoutDataLC_AG.TransactionType,
                    transactionCode = payoutDataLC_AG.TransactionCode,
                    gameResult = payoutDataLC_AG.GameResult,
                    TicketStatus = payoutDataLC_AG.TransactionType.Equals("LOSE", StringComparison.OrdinalIgnoreCase) ? (int)TicketStatus.Lost : (int)TicketStatus.Win
                });

                p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("CanPayout", dbType: DbType.Int32, direction: ParameterDirection.Output);

                result = _mainDbClient.Execute("Service_BettransLC_AGV2_Payout_Ins_V2", p, conn, trans);

                if (p.Get<int>("CanPayout") == 1)
                {
                    decimal payoutAmount = p.Get<decimal>("Stake") + p.Get<decimal>("WinLostAmount");

                    if (result > 0 && payoutAmount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, payoutAmount, (int)Tp.AG, TransType.Payout, $"Payout: {payoutAmount.ToString("#,##0.##")}", payoutDataLC_AG.TransactionIDs.Count() > 1 ? payoutDataLC_AG.BillNo : payoutDataLC_AG.TransactionID, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, payoutAmount, (int)Tp.AG, payoutDataLC_AG.TransactionID, gameType, TpCategory.LiveCasino, payoutDataLC_AG.TransactionIDs.Count() > 1 ? payoutDataLC_AG.BillNo : payoutDataLC_AG.TransactionID, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }
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
                    throw mySqlEx;
                }
            }

            return result;
        }
        public IEnumerable<DataLC_AG> GetUnsettledListLC_AG()
        {
            return _mainDbClient.Query<DataLC_AG>("Service_BettransLC_AGV2_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int RefundLC_AG(TpCustInfo tpCustInfo, BettingDataLC_AG_Refund bettingDataLC_AG_Refund, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AG_LiveCasino;

            afterAmount = 0;
            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            string username = bettingDataLC_AG_Refund.Playname.Substring(bettingDataLC_AG_Refund.AgentCode.Length);

            var p = new DynamicParameters(new
            {
                transactionID = bettingDataLC_AG_Refund.TransactionID,
                playname = bettingDataLC_AG_Refund.Playname
            });

            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_CancelBetLC_AGV2_Set", p, conn, trans);

            if (result > 0)
            {
                decimal payoutAmount = p.Get<decimal?>("Stake") ?? 0;

                if (payoutAmount == bettingDataLC_AG_Refund.Value)
                {
                    if (payoutAmount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, payoutAmount, (int)Tp.AG, TransType.Refunds, $"Refund: {payoutAmount.ToString("#,##0.##")}", bettingDataLC_AG_Refund.TransactionID, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, payoutAmount, (int)Tp.AG, bettingDataLC_AG_Refund.TransactionID, gameType, TpCategory.LiveCasino, bettingDataLC_AG_Refund.TransactionID, TransType.Refunds, null, out afterAmount, "Refund", out bonusId, conn, trans);
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
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                return -1;
            }

            return result;
        }
        public IEnumerable<SettleDataLC_AG> Get_PayoutList_AG(string transactionID)
        {
            return _mainDbClient.Query<SettleDataLC_AG>("Service_BettransLC_AGV2_Payout_Get", commandType: CommandType.StoredProcedure, new { transactionID = transactionID });
        }
        public IEnumerable<BettingDataLC_AG> Get_BetList_AG(string transactionIDs)
        {
            return _mainDbClient.Query<BettingDataLC_AG>("Service_BettransLC_AGV2_Bet_Get", commandType: CommandType.StoredProcedure, new { transactionIDs = transactionIDs });
        }
        public int SetSettleFlag_AG(string transactionID, string transactionIDs)
        {
            return _mainDbClient.Execute("Service_BettransLC_AGV2_SettleFlag_Set", commandType: CommandType.StoredProcedure, new { transactionID = transactionID, transactionIDs = transactionIDs });
        }
        public int SetSettleFlag_TpWagerAG(string billNoList)
        {
            return _wagerDbClient.Execute("Service_TpWagerAG_Settle_Set", commandType: CommandType.StoredProcedure, new { billNoList = billNoList });
        }
        public int Get_TpWagerAG_Settle(string billNoList, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            var p = new DynamicParameters(new { billNoList = billNoList });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            int result = _wagerDbClient.Execute("Service_TpWagerAG_Settle_Get", commandType: CommandType.StoredProcedure, p);

            if (p.Get<decimal?>("Stake") != null)
            {
                stake = p.Get<decimal>("Stake");
                validBet = p.Get<decimal>("ValidBet");
                winLostAmount = p.Get<decimal>("WinLostAmount");
                settleTime = p.Get<long>("SettleTime");
            }
            else
            {
                result = 0;
            }

            return result;
        }

        public SettleData GetSettle_TpWagerAG(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData>("Service_TpWagerAG_Settle_Get_V2", parameters: new DynamicParameters(new { billNoList = TransId }));
        }

        #endregion

        #region Sexy
        public int CancelBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            foreach (var txn in cancelBetTxn.txns)
            {
                DateTime.TryParse(txn.updateTime, out DateTime uT);
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                var p = new DynamicParameters(new { txId = txn.txId, userId = cancelBetTxn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_CancelBetLC_Sexy_Set", p, conn, trans);

                if (result > 0)
                {
                    decimal betAmount = p.Get<decimal>("betAmount");
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betAmount, (int)Tp.Sexy, TransType.Refunds, $"Cancel Bet: {betAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, betAmount, (int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, txn.txId, TransType.Refunds, null, out afterAmount, "CancelBet", out bonusId, conn, trans);
                    if (result == 0)
                    {
                        return 0;
                    }
                }
                else
                {
                    result = -1;
                    continue;
                }
            }

            _mainDbClient.CommitAndDisposeConnection(conn, trans);
            return result;
        }
        public int SettledLC_Sexy(BettingDataLC_Sexy_Settle bettingDataLC_Sexy_Settle, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;
            bool haveNoBetting = false;
            DateTime.TryParse(bettingDataLC_Sexy_Settle.RoundStartTime, out DateTime dtTransTime);
            long roundStartTime = tools.ConvertLocTime(dtTransTime, 8);

            _mainDbClient.Execute("Service_BettransLC_Sexy_Settle_Ins", commandType: CommandType.StoredProcedure, new { roundId = bettingDataLC_Sexy_Settle.RoundId, dealerDomain = bettingDataLC_Sexy_Settle.DealerDomain, roundStartTime = roundStartTime, validBetTime = bettingDataLC_Sexy_Settle.ValidBetTime / 1000, winner = bettingDataLC_Sexy_Settle.Winner, result = bettingDataLC_Sexy_Settle.Result });

            foreach (var txn in bettingDataLC_Sexy_Settle.Txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                DateTime.TryParse(txn.BetTime, out dtTransTime);
                long betTime = tools.ConvertLocTime(dtTransTime, 8);
                DateTime.TryParse(txn.UpdateTime, out dtTransTime);
                long updateTime = tools.ConvertLocTime(dtTransTime, 8);
                try
                {
                    result = _mainDbClient.Execute("Service_BettransmLC_Sexy_Settle_Ins", new { txId = txn.TxId, roundId = bettingDataLC_Sexy_Settle.RoundId, userId = txn.UserId, category = txn.Category, gameType = txn.GameType, betAmount = txn.BetAmount, betTime = betTime, updateTime = updateTime, winLoss = txn.WinLoss, odds = txn.Odds, settleWinLoss = txn.SettleWinLoss, validBet = txn.ValidBet, status = txn.Status }, conn, trans);
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                    {
                        result = -1;
                        log2File.Error($"{JsonConvert.SerializeObject(txn)}", $"Sexy_Duplicate");
                        continue;
                    }
                    else
                    {
                        throw mySqlEx;
                    }
                }

                if (result > 0)
                {
                    if (txn.SettleWinLoss != 0)
                    {
                        //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, txn.SettleWinLoss, (int)Tp.Sexy, TransType.Payout, $"Payout: {Math.Abs(txn.SettleWinLoss).ToString("#,##0.##")}", txn.TxId, out afterAmount, conn, trans, AllowNegative.Allow);
                        result = PayoutV2(txn.tpCustInfo, txn.SettleWinLoss, (int)Tp.Sexy, txn.TxId, gameType, TpCategory.LiveCasino, txn.TxId, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.Sexy, txn.TxId, gameType, TpCategory.LiveCasino, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.Sexy;
                        betRecordInfo.Stake = txn.BetAmount;
                        betRecordInfo.ValidBet = txn.ValidBet;
                        betRecordInfo.WinlostAmount = txn.WinLoss;
                        betRecordInfo.TransId = txn.TxId;
                        betRecordInfo.GameType = GameType.Sexy_LiveCasino;
                        betRecordInfo.TpUserName = txn.UserId;
                        betRecordInfo.SettledTime = updateTime;
                        betRecordInfo.SettlementType = SettlementType.SettledForValid;
                        betRecordInfo.BonusId = bonusId;

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, trans);

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
                else
                {
                    log2File.Error($"{JsonConvert.SerializeObject(txn)}", $"Sexy_haveNoBetting");/*need add check in SP*/
                    haveNoBetting = true;
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            if (haveNoBetting)
            {
                return 0;
            }
            else
            {
                _mainDbClient.Execute("Service_BettransLC_Sexy_Settle_Set",
                    commandType: CommandType.StoredProcedure, new
                    {
                        roundId = bettingDataLC_Sexy_Settle.RoundId
                    });

                result = result == -1 ? 1 : result;
                return result;
            }
        }
        public int UnsettledLC_Sexy(Unsettle_VoidRound unsettle, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;
            bool cantUnsettle = false;

            foreach (var txn in unsettle.txns)
            {
                DateTime.TryParse(txn.updateTime, out DateTime uT);
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                var p = new DynamicParameters(new { txId = txn.txId });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("winLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("settleWinLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("validBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransmLC_Sexy_Unsettle_Del", p, conn, trans);

                if (result > 0)
                {
                    decimal settleWinLoss = p.Get<decimal>("settleWinLoss");
                    decimal validBet = p.Get<decimal>("validBet");
                    decimal betAmount = p.Get<decimal>("betAmount");
                    decimal winLoss = p.Get<decimal>("winLoss");
                    if (settleWinLoss != 0)
                    {
                        //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, settleWinLoss * -1, (int)Tp.Sexy, TransType.Payout, $"Unsettle: {(settleWinLoss * -1).ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
                        result = PayoutV2(txn.tpCustInfo, settleWinLoss * -1, (int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, txn.txId, TransType.Undo, null, out afterAmount, "Unsettle", out bonusId, conn, trans, AllowNegative.Allow);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.Sexy;
                        betRecordInfo.Stake = betAmount;
                        betRecordInfo.ValidBet = 0;
                        betRecordInfo.WinlostAmount = 0;
                        betRecordInfo.TransId = txn.txId;
                        betRecordInfo.GameType = GameType.Sexy_LiveCasino;
                        betRecordInfo.TpUserName = txn.userId;
                        betRecordInfo.SettledTime = updateTime.ToUnixTimeSeconds();
                        betRecordInfo.SettlementType = SettlementType.UnSettled;
                        betRecordInfo.TicketStatus = TicketStatus.UnSettled;
                        betRecordInfo.BonusId = bonusId;

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, trans);

                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            cantUnsettle = true;
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
                    else
                    {
                        cantUnsettle = true;
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            if (cantUnsettle)
            {
                return -99;
            }
            else
            {
                _mainDbClient.Execute("Service_BettransLC_Sexy_Unsettle_Del",
                    commandType: CommandType.StoredProcedure, new
                    {
                        roundId = unsettle.roundId
                    });

                return 1;
            }
        }

        public int VoidRoundLC_Sexy(Unsettle_VoidRound voidRound, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;

            var checkUnsettled = _mainDbClient.QueryFirst<int>("Service_BettransLC_Sexy_CheckUnsettled_Get", new { roundId = voidRound.roundId }) > 0;
            if (checkUnsettled)
            {
                return 0;
            }
            else
            {
                foreach (var txn in voidRound.txns)
                {
                    DateTime.TryParse(txn.updateTime, out DateTime uT);
                    DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                    using var conn = _mainDbClient.GetMySqlConnection();
                    conn.Open();
                    using var trans = conn.BeginTransaction();
                    var p = new DynamicParameters(new { txId = txn.txId, userId = txn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                    p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                    result = _mainDbClient.Execute("Service_CancelBetLC_Sexy_Set", p, conn, trans);

                    if (result > 0)
                    {
                        decimal betAmount = p.Get<decimal>("betAmount");

                        if (betAmount != 0)
                        {
                            //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, betAmount, (int)Tp.Sexy, TransType.Refunds, $"VoidRound: {betAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
                            result = PayoutV2(txn.tpCustInfo, betAmount, (int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, txn.txId, TransType.Refunds, null, out afterAmount, "VoidRound", out bonusId, conn, trans, AllowNegative.Allow);
                        }
                        else
                        {
                            GetTpBonus((int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, out int custId, out bonusId);
                        }

                        if (result > 0)
                        {
                            BetRecordInfo betRecordInfo = new BetRecordInfo();
                            betRecordInfo.TpId = (int)Tp.Sexy;
                            betRecordInfo.Stake = betAmount;
                            betRecordInfo.ValidBet = 0;
                            betRecordInfo.WinlostAmount = 0;
                            betRecordInfo.TransId = txn.txId;
                            betRecordInfo.GameType = GameType.Sexy_LiveCasino;
                            betRecordInfo.TpUserName = txn.userId;
                            betRecordInfo.SettledTime = updateTime.ToUnixTimeSeconds();
                            betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                            betRecordInfo.TicketStatus = TicketStatus.Refund;
                            betRecordInfo.BonusId = bonusId;

                            result = SettleTicket(txn.tpCustInfo, betRecordInfo, trans);

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
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        result = 1;
                    }
                }
            }

            return result;
        }
        public int VoidTxnLC_Sexy(VoidTxn voidTxn, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;
            bool cantVoidTxn = false;

            foreach (var txn in voidTxn.txns)
            {
                DateTime.TryParse(txn.updateTime, out DateTime uT);
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                var p = new DynamicParameters(new { txId = txn.txId, userId = txn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("winLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("settleWinLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("validBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransmLC_Sexy_VoidTxn_Set", p, conn, trans);

                if (result > 0)
                {
                    decimal betAmount = p.Get<decimal>("betAmount");
                    decimal settleWinLoss = p.Get<decimal>("settleWinLoss");
                    decimal validBet = p.Get<decimal>("validBet");
                    decimal fixAmount = betAmount - settleWinLoss;
                    if (fixAmount != 0)
                    {
                        //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, fixAmount, (int)Tp.Sexy, TransType.Refunds, $"VoidTxn: {fixAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
                        result = PayoutV2(txn.tpCustInfo, fixAmount, (int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, txn.txId, TransType.Refunds, null, out afterAmount, "VoidTxn", out bonusId, conn, trans, AllowNegative.Allow);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.Sexy;
                        betRecordInfo.Stake = betAmount;
                        betRecordInfo.ValidBet = validBet;
                        betRecordInfo.WinlostAmount = 0;
                        betRecordInfo.TransId = txn.txId;
                        betRecordInfo.GameType = GameType.Sexy_LiveCasino;
                        betRecordInfo.TpUserName = txn.userId;
                        betRecordInfo.SettledTime = updateTime.ToUnixTimeSeconds();
                        betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        betRecordInfo.TicketStatus = TicketStatus.Refund;
                        betRecordInfo.BonusId = bonusId;

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, trans);

                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            cantVoidTxn = true;
                        }
                    }
                    else
                    {
                        cantVoidTxn = true;
                    }
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            if (cantVoidTxn)
            {
                return -99;
            }
            else
            {
                return 1;
            }
        }

        public int PrizeLC_Sexy(IEnumerable<Sexy_Treat> sexy_Treats, out decimal afterAmount)
        {
            int result = 0;
            afterAmount = 0;

            foreach (var sexy_Treat in sexy_Treats)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                DateTime _transferTime = DateTime.ParseExact(sexy_Treat.transferTime, "dd-MM-yyyy HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
                long transferTime = tools.ConvertLocTime(_transferTime, 8);

                try
                {
                    result = _mainDbClient.Execute("Service_BettransLC_Sexy_Prize_Ins", new { txId = sexy_Treat.txId, treatId = sexy_Treat.treatId, userId = sexy_Treat.userId, amount = sexy_Treat.amount, transferTime = transferTime }, conn, trans);

                    if (result > 0)
                    {
                        if (sexy_Treat.amount > 0)
                        {
                            //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, txn.SettleWinLoss, (int)Tp.Sexy, TransType.Payout, $"Payout: {Math.Abs(txn.SettleWinLoss).ToString("#,##0.##")}", txn.TxId, out afterAmount, conn, trans, AllowNegative.Allow);
                            result = PayoutV2(sexy_Treat.tpCustInfo, sexy_Treat.amount, (int)Tp.Sexy, sexy_Treat.txId, (int)GameType.Sexy_LiveCasino, TpCategory.LiveCasino, sexy_Treat.txId, TransType.Prize, null, out afterAmount, $"{sexy_Treat.treatId} Prize", out _, conn, trans);
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
                        log2File.Error($"{JsonConvert.SerializeObject(sexy_Treat)}", $"Sexy_Duplicate_Treat");
                        continue;
                    }
                    else
                    {
                        throw mySqlEx;
                    }
                }
            }

            return result;
        }
        #endregion

        #region Sexy (AWC)

        public int CancelBetLC_AWC_Sexy(TpCustInfo tpCustInfo, CancelLC_Sexy cancelLC_Sexy, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;
            foreach (var item in cancelLC_Sexy.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    userId = item.userId,
                    platform = item.platform,
                    gameType = item.gameType,
                    gameCode = item.gameCode,
                    roundId = item.roundId,
                    roundStartUnix = new DateTimeOffset((DateTime)item.gameInfo.roundStartTime).ToUnixTimeSeconds(),
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_CancelBet_Set", p, conn, trans);

                if (result > 0)
                {
                    decimal stake = p.Get<decimal>("out_stake");

                    if (stake > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            stake,
                            (int)Tp.Sexy,
                            item.platformTxId,
                            (int)GameType.Sexy_LiveCasino,
                            TpCategory.LiveCasino,
                            item.platformTxId,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "CancelBet",
                            out bonusId,
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
                    _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_CancelBet_Ins", p, conn, trans);
                    var refBettransLC = p.Get<int>("out_result");

                    if (refBettransLC == 2)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    result = -1;
                }
            }

            return result;
        }

        public int CancelTipLC_AWC_Sexy(TpCustInfo tpCustInfo, CancelTipLC_Sexy cancelTipLC_Sexy, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            foreach (var item in cancelTipLC_Sexy.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    userId = item.userId,
                    platform = item.platform,
                    gameType = item.gameType,
                    gameCode = item.gameCode,
                    gameName = item.gameName,
                    betType = "",
                    roundId = "",
                    tipInfo = JsonConvert.SerializeObject(item.tipInfo),
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Tip,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });
                p.Add("out_tip", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_CancelTip_Set", p, conn, trans);

                if (result > 0)
                {
                    decimal tip = p.Get<decimal>("out_tip");

                    if (tip > 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            tip,
                            (int)Tp.Sexy,
                            TransType.Tip,
                            $"CancelTip: {tip:#,##0.##}",
                            item.platformTxId,
                            out afterAmount,
                            conn,
                            trans,
                            AllowNegative.Allow
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
                    _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_CancelTip_Ins", p, conn, trans);
                    var refBettransLC = p.Get<int>("out_result");

                    if (refBettransLC == 2)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    result = -1;
                }
            }

            return result;
        }

        public int VoidBetLC_AWC_Sexy(VoidBetLC_Sexy voidBetLC_Sexy)
        {
            var result = 0;
            foreach (var item in voidBetLC_Sexy.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    // 如果最後是Void狀態，表示該局已判定無效，不會再變動狀態，也不等於cancel
                    var p = new DynamicParameters(new
                    {
                        platformTxId = item.platformTxId,
                        gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                        updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                        voidType = item.voidType,
                        TicketType = (int)BettransType.CancelBet,
                        TicketStatus = (int)TicketStatus.Refund,
                        SettleFlag = (int)SettleFlag.Settled
                    });

                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_VoidBet_Set", p, conn, trans);
                    if (result > 0)
                    {
                        decimal stake = Math.Abs(item.betAmount);
                        result = PayoutV2(
                            item.tpCustInfo,
                            stake,
                            (int)Tp.Sexy,
                            item.platformTxId,
                            (int)GameType.Sexy_LiveCasino,
                            TpCategory.LiveCasino,
                            item.platformTxId,
                            TransType.Refunds,
                            null,
                            out _,
                            "VoidBet",
                            out _,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }
                    else
                    {
                        result = -1;
                        continue;
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
                    throw mySqlEx;
                }
            }
            return result;
        }

        public int PayoutLC_AWC_Sexy(PayoutLC_Sexy payoutLC_Sexy, out decimal afterAmount)
        {
            int bonusId = 0;
            bool isError = false;
            afterAmount = 0;
            var result = 0;

            foreach (var txn in payoutLC_Sexy.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                int ticketStatus;
                ticketStatus = txn.gameInfo.status switch
                {
                    "WIN" => (int)TicketStatus.Win,
                    "LOSE" => (int)TicketStatus.Lost,
                    "DRAW" => (int)TicketStatus.Draw,
                    "TIE" => (int)TicketStatus.Draw,
                    "VOID" => (int)TicketStatus.Refund,
                    "CANCEL" => (int)TicketStatus.Refund,
                    _ => throw new NotImplementedException()
                };

                try
                {
                    var p = new DynamicParameters(new
                    {
                        platformTxId = txn.platformTxId,
                        winAmount = txn.winAmount,
                        turnover = txn.turnover,
                        winLoss = txn.gameInfo.winLoss,
                        roundId = txn.roundId,
                        betType = txn.betType,
                        status = txn.gameInfo.status,
                        settleType = txn.settleType,
                        refPlatformTxId = txn.refPlatformTxId,
                        gameInfo = JsonConvert.SerializeObject(txn.gameInfo),
                        betUnix = new DateTimeOffset(txn.betTime).ToUnixTimeSeconds(),
                        txUnix = new DateTimeOffset(txn.txTime).ToUnixTimeSeconds(),
                        updateUnix = new DateTimeOffset(txn.updateTime).ToUnixTimeSeconds(),
                        TicketType = (int)BettransType.Payout,
                        TicketStatus = ticketStatus,
                        SettleFlag = (int)SettleFlag.EndGame
                    });

                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_Payout_Set", p, conn, trans);
                    decimal payout = txn.winAmount;
                    if (result > 0)
                    {
                        if (payout != 0)
                        {
                            result = PayoutV2(
                                txn.tpCustInfo,
                                payout,
                                (int)Tp.Sexy,
                                txn.platformTxId,
                                (int)GameType.Sexy_LiveCasino,
                                TpCategory.LiveCasino,
                                txn.platformTxId,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                "Payout",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                           );
                        }

                        if (result > 0)
                        {
                            var insertTpResults = _tpUnsettleService.InsertTpUnsettle(Tp.Sexy, GameType.Sexy_LiveCasino, txn.platformTxId, trans);
                            if (insertTpResults <= 0)
                            {
                                result = 0;
                            }
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
                    else
                    {
                        result = -1;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(txn)}\r\n{mySqlEx}", $"{Tp.Sexy}_settle", mySqlEx);
                }
            }
            return isError ? 0 : result;
        }

        public int UnPayoutLC_AWC_Sexy(UnPayoutLC_Sexy unPayoutLC_Sexy)
        {
            bool isError = false;
            var result = 0;
            foreach (var item in unPayoutLC_Sexy.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.EndGame
                });
                p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_UnPayout_Set", p, conn, trans);
                    if (result > 0)
                    {
                        decimal payout = p.Get<decimal>("out_payout");

                        if (payout != 0)
                        {
                            result = PayoutV2(
                                item.tpCustInfo,
                                payout * -1,
                                (int)Tp.Sexy,
                                item.platformTxId,
                                (int)GameType.Sexy_LiveCasino,
                                TpCategory.LiveCasino,
                                item.platformTxId,
                                TransType.Undo,
                                null,
                                out _,
                                "UnSettle",
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        result = -1;
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"{Tp.Sexy}_unsettle", mySqlEx);
                }
            }

            return isError ? 0 : result;
        }

        public int VoidSettleLC_AWC_Sexy(VoidSettleLC_Sexy voidSettleLC_Sexy)
        {
            bool isError = false;
            var result = 0;
            foreach (var item in voidSettleLC_Sexy.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    voidType = item.voidType,
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.EndGame
                });
                p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_VoidSettle_Set", p, conn, trans);

                    if (result > 0)
                    {
                        decimal payout = p.Get<decimal>("out_payout");
                        payout = (payout > 0) ? (payout * -1) + item.betAmount : item.betAmount;

                        if (payout != 0)
                        {
                            result = PayoutV2(
                                item.tpCustInfo,
                                payout,
                                (int)Tp.Sexy,
                                item.platformTxId,
                                (int)GameType.Sexy_LiveCasino,
                                TpCategory.LiveCasino,
                                item.platformTxId,
                                TransType.Refunds,
                                null,
                                out _,
                                "VoidSettle",
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        result = -1;
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"{Tp.Sexy}_voidSettle", mySqlEx);
                }
            }

            return isError ? 0 : result;
        }

        public int RewardLC_AWC_Sexy(RewardLC_Sexy rewardLC_Sexy, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in rewardLC_Sexy.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    var p = new DynamicParameters(new
                    {
                        promotionTxId = item.promotionTxId,
                        promotionId = item.promotionId,
                        promotionTypeId = item.promotionTypeId,
                        platform = item.platform,
                        userId = item.userId,
                        amount = item.amount,
                        txUnix = new DateTimeOffset(item.txTime).ToUnixTimeSeconds()
                    });
                    result = _mainDbClient.Execute("Service_RewardLC_AWC_Sexy_Ins", p, conn, trans);
                    decimal amount = item.amount;
                    if (result > 0)
                    {
                        if (amount != 0)
                        {
                            result = PayoutV2(
                                item.tpCustInfo,
                                amount,
                                (int)Tp.Sexy,
                                item.promotionTxId,
                                (int)GameType.Sexy_LiveCasino,
                                TpCategory.LiveCasino,
                                item.promotionTxId,
                                TransType.Prize,
                                null,
                                out afterAmount,
                                $"{item.promotionId} Prize",
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                    if (mySqlEx.Number == 1062)
                    {
                        result = -1;
                    }
                    else
                    {
                        isError = true;
                        log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"{Tp.Sexy}_give", mySqlEx);
                    }
                }
            }

            return isError ? 0 : result;
        }

        public int ResettleLC_AWC_Sexy(ResettleLC_Sexy resettleLC_Sexy)
        {
            bool isError = false;
            var result = 0;
            foreach (var item in resettleLC_Sexy.txns)
            {
                try
                {
                    using var conn = _mainDbClient.GetMySqlConnection();
                    conn.Open();
                    using var trans = conn.BeginTransaction();
                    var check = _mainDbClient.QueryFirst<BettransDataLC_Sexy>("Service_BettransLC_AWC_Sexy_Get", trans, new { platformTxId = item.platformTxId });
                    if (check == null)
                    {
                        // no data
                        log2File.Error($"not exist\r\n{JsonConvert.SerializeObject(item)}", $"{Tp.Sexy}_resettle");
                        result = -1;
                        continue;
                    }

                    decimal payout = 0;
                    // Way to change Balance : Current Balance + new winAmount (resettle) - old winAmount (settle)
                    if (check.TicketType == (int)BettransType.Payout && check.winAmount != item.winAmount)
                    {
                        payout = item.winAmount - check.winAmount;
                    }

                    int ticketStatus;
                    ticketStatus = item.gameInfo.status switch
                    {
                        "WIN" => (int)TicketStatus.Win,
                        "LOSE" => (int)TicketStatus.Lost,
                        "DRAW" => (int)TicketStatus.Draw,
                        "TIE" => (int)TicketStatus.Draw,
                        "VOID" => (int)TicketStatus.Refund,
                        "CANCEL" => (int)TicketStatus.Refund,
                        _ => throw new NotImplementedException()
                    };

                    var p = new DynamicParameters(new
                    {
                        platformTxId = item.platformTxId,
                        gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                        updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                        txUnix = new DateTimeOffset(item.txTime).ToUnixTimeSeconds(),
                        winAmount = item.winAmount,
                        winLoss = item.gameInfo.winLoss,
                        TicketType = (int)BettransType.Payout,
                        TicketStatus = ticketStatus,
                        SettleFlag = (int)SettleFlag.EndGame
                    });

                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_Resettle_Set", p, conn, trans);
                    if (result > 0)
                    {
                        if (payout != 0)
                        {
                            result = PayoutV2(
                                item.tpCustInfo,
                                payout,
                                (int)Tp.Sexy,
                                item.platformTxId,
                                (int)GameType.Sexy_LiveCasino,
                                TpCategory.LiveCasino,
                                item.platformTxId,
                                TransType.Undo,
                                null,
                                out _,
                                "Resettle",
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        result = -1;
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"{Tp.Sexy}_resettle", mySqlEx);
                }
            }

            return isError ? 0 : result;
        }

        public IEnumerable<SettleData> GetUnsettledListLC_AWC_Sexy()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransLC_AWC_Sexy_Unsettle_Get");
        }

        public int SetSettleFlag_BettransLC_AWC_Sexy(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }
        #endregion

        #region EBet
        public int PayoutLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            int ticketStatus = (int)TicketStatus.Win;

            if (bettingData_EBet.detail.payout == 0)
            {
                ticketStatus = (int)TicketStatus.Lost;
            }
            else if (bettingData_EBet.detail.payout == bettingData_EBet.detail.totalBet)
            {
                ticketStatus = (int)TicketStatus.Draw;
            }

            string mixUniqueId = $"{bettingData_EBet.detail.roundCode}_{bettingData_EBet.username}";
            var settleFlag = (int)SettleFlag.EndGame;
            if (bettingData_EBet.detail.totalBet == 0 && bettingData_EBet.detail.validBet == 0 && bettingData_EBet.detail.payout == 0 && bettingData_EBet.money == 0)
            {
                settleFlag = (int)SettleFlag.Settled;
                ticketStatus = (int)TicketStatus.Refund;
            }

            var srcResults = JsonConvert.SerializeObject(bettingData_EBet.detail.srcResults);
            var results = JsonConvert.SerializeObject(bettingData_EBet.detail.results);
            var p = new DynamicParameters(new
            {
                TransId = mixUniqueId,
                username = bettingData_EBet.username,
                channelId = bettingData_EBet.channelId,
                money = bettingData_EBet.money,
                type = bettingData_EBet.type,
                platform = bettingData_EBet.platform,
                currency = bettingData_EBet.currency,
                seqNo = bettingData_EBet.seqNo,
                Event = bettingData_EBet.Event,
                timestamp = bettingData_EBet.timestamp,
                tableCode = bettingData_EBet.detail.tableCode,
                roundCode = bettingData_EBet.detail.roundCode,
                betTime = bettingData_EBet.detail.betTime,
                totalBet = bettingData_EBet.detail.totalBet,
                validBet = bettingData_EBet.detail.validBet,
                detail = JsonConvert.SerializeObject(bettingData_EBet.detail),
                betList = JsonConvert.SerializeObject(bettingData_EBet.detail.betList),
                payoutTime = bettingData_EBet.detail.payoutTime,
                payout = bettingData_EBet.detail.payout,
                srcResults = (string.IsNullOrEmpty(srcResults) || srcResults == "null") ? "[]" : srcResults,
                results = (string.IsNullOrEmpty(results) || results == "null") ? "[]" : results,
                TicketStatus = ticketStatus,
                TicketType = (int)BettransType.Payout,
                SettleFlag = settleFlag
            });


            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_EBet_Payout_Ins_V2", p, conn, trans);

                if (result > 0)
                {
                    if (bettingData_EBet.money != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingData_EBet.money,
                            (int)Tp.EBet,
                            mixUniqueId,
                            gameType,
                            TpCategory.LiveCasino,
                            mixUniqueId,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
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

        public int PreRefundLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var betList = (bettingData_EBet.detail.withHoldingList != null) ? JsonConvert.SerializeObject(bettingData_EBet.detail.withHoldingList) : JsonConvert.SerializeObject(bettingData_EBet.detail.betList);
                string mixUniqueId = $"{bettingData_EBet.detail.roundCode}_{bettingData_EBet.username}";
                var p = new DynamicParameters(new
                {
                    TransId = mixUniqueId,
                    username = bettingData_EBet.username,
                    channelId = bettingData_EBet.channelId,
                    money = bettingData_EBet.money,
                    type = bettingData_EBet.type,
                    platform = bettingData_EBet.platform,
                    currency = bettingData_EBet.currency,
                    seqNo = bettingData_EBet.seqNo,
                    Event = bettingData_EBet.Event,
                    timestamp = bettingData_EBet.timestamp,
                    tableCode = bettingData_EBet.detail.tableCode,
                    roundCode = bettingData_EBet.detail.roundCode,
                    betTime = bettingData_EBet.timestamp,
                    totalBet = bettingData_EBet.detail.totalBet,
                    validBet = bettingData_EBet.detail.validBet,
                    detail = JsonConvert.SerializeObject(bettingData_EBet.detail),
                    betList = betList,
                    TicketStatus = (int)TicketStatus.Refund,
                    TicketType = (int)BettransType.Reserve
                });

                result = _mainDbClient.Execute("Service_BettransLC_EBet_Bet_Ins_V2", p, conn, trans);

                if (result > 0)
                {
                    if (bettingData_EBet.money != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingData_EBet.money,
                            (int)Tp.EBet,
                            mixUniqueId,
                            gameType,
                            TpCategory.LiveCasino,
                            mixUniqueId,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
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

        public int CancelBetLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            int result = 0;
            string mixUniqueId = $"{bettingData_EBet.detail.roundCode}_{bettingData_EBet.username}";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var seqNo = string.IsNullOrEmpty(bettingData_EBet.detail.withHoldingSeqNo)
                ? bettingData_EBet.seqNo
                : bettingData_EBet.detail.withHoldingSeqNo;

            var refBettransLC = _mainDbClient.Execute("Service_BettransLC_Ebet_CancelBet_Set", new
            {
                seqNo = seqNo
            }, conn, trans);
            if (refBettransLC > 0)
            {
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(bettingData_EBet.money),
                    (int)Tp.EBet,
                    bettingData_EBet.seqNo,
                    gameType, TpCategory.LiveCasino,
                    mixUniqueId,
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "CancelBet",
                    out bonusId,
                    conn,
                    trans
                );

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
                return -1;
            }

            return result;
        }

        public int RefundLC_EBet(TpCustInfo tpCustInfo, RefundListEbet refundListEbet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            int result = 0;
            string mixUniqueId = $"{refundListEbet.roundCode}_{refundListEbet.username}";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { seqNo = refundListEbet.refundSeqNo });
            p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var refBettransLC = -99;
            _mainDbClient.Execute("Service_BettransLC_Ebet_Refund_Set", p, conn, trans);
            refBettransLC = p.Get<int>("out_result");

            if (refBettransLC == 0)
            {
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(refundListEbet.refundMoney),
                    (int)Tp.EBet,
                    refundListEbet.refundSeqNo,
                    gameType, TpCategory.LiveCasino,
                    mixUniqueId,
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "Refund",
                    out bonusId,
                    conn,
                    trans
                );

                if (result > 0)
                {
                    _mainDbClient.Execute("Service_BettransLC_EBet_ReSettle_Set", new { TransId = mixUniqueId }, conn, trans);
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            else
            {
                return refBettransLC;
            }

            return result;
        }

        public int RefundBatchLC_EBet(TpCustInfo tpCustInfo, BatchRefundList batchRefundList, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            int result = 0;
            string mixUniqueId = $"{batchRefundList.roundCode}_{batchRefundList.username}";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var refBettransLC = _mainDbClient.Execute("Service_BettransLC_Ebet_RefundBatch_Set", new
            {
                seqNo = batchRefundList.seqNo
            }, conn, trans);

            if (refBettransLC > 0)
            {
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(batchRefundList.refundMoney),
                    (int)Tp.EBet,
                    batchRefundList.seqNo,
                    gameType, TpCategory.LiveCasino,
                    mixUniqueId,
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "Refund",
                    out bonusId,
                    conn,
                    trans
                );

                if (result > 0)
                {
                    _mainDbClient.Execute("Service_BettransLC_EBet_ReSettle_Set", new { TransId = mixUniqueId }, conn, trans);
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        public int RewardLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            var p = new DynamicParameters(new
            {
                username = bettingData_EBet.username,
                channelId = bettingData_EBet.channelId,
                money = bettingData_EBet.money,
                type = bettingData_EBet.type,
                platform = bettingData_EBet.platform,
                currency = bettingData_EBet.currency,
                seqNo = bettingData_EBet.seqNo,
                Event = bettingData_EBet.Event,
                timestamp = bettingData_EBet.timestamp,
                detail = JsonConvert.SerializeObject(bettingData_EBet.detail),
                TicketStatus = (int)TicketStatus.Win,
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_RewardLC_EBet_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (bettingData_EBet.money != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingData_EBet.money,
                            (int)Tp.EBet,
                            bettingData_EBet.seqNo,
                            (int)GameType.EBet_LiveCasino,
                            TpCategory.LiveCasino,
                            bettingData_EBet.seqNo,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            $"{bettingData_EBet.seqNo} Prize",
                            out bonusId,
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


        public IEnumerable<CreditRecord_EBet> GetCreditRecordLC_EBet(string[] seqNo)
        {
            var jsonSeqNo = JsonConvert.SerializeObject(seqNo);
            var bet = _mainDbClient.Query<CreditRecord_EBet>("Service_BettransLC_EBet_Bets_Get", commandType: CommandType.StoredProcedure, new { seqNos = jsonSeqNo });
            var payout = _mainDbClient.Query<CreditRecord_EBet>("Service_BettransLC_EBet_Payouts_Get", commandType: CommandType.StoredProcedure, new { seqNos = jsonSeqNo });
            var list = bet.Union(payout);
            if (list?.Count() > 0)
            {
                foreach (var record in list)
                {
                    if (record.TicketStatus == (int)TicketStatus.Refund)
                    {
                        // 已经退款的数据
                        record.status = 209;
                    }
                    else
                    {
                        record.status = 200;
                    }
                }
            }
            return list;
        }

        public IEnumerable<SettleData> GetUnsettledListLC_EBet()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransLC_EBet_Unsettle_Get_V4", commandType: CommandType.StoredProcedure);
        }

        public SettleData GetUnsettledListLC_EBet_ByTrans(string transId)
        {
            return _mainDbClient.QueryFirst<SettleData>("Service_BettransLC_EBet_Unsettle_ByTrans_Get", parameters: new { _TransId = transId });
        }

        public int SetSettleFlag_EBet(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransLC_EBet_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId, SettleFlag = 1 });
        }
        #endregion

        #region WM

        public int PayoutLC_WM(TpCustInfo tpCustInfo, BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.WM_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            string mixUniqueId = $"{bettingDataLC_WM.gameno}_{bettingDataLC_WM.user}";
            var word = "_cancel";
            if (bettingDataLC_WM.gameno.Contains(word))
            {
                mixUniqueId = $"{bettingDataLC_WM.gameno.Replace(word, "")}_{bettingDataLC_WM.user}";
            }

            var p = new DynamicParameters(new
            {
                TransId = mixUniqueId,
                user = bettingDataLC_WM.user,
                money = bettingDataLC_WM.money,
                dealid = bettingDataLC_WM.dealid,
                gtype = bettingDataLC_WM.gtype,
                type = bettingDataLC_WM.type,
                betdetail = bettingDataLC_WM.betdetail,
                gameno = bettingDataLC_WM.gameno,
                code = bettingDataLC_WM.code,
                category = bettingDataLC_WM.category,
                requestUnix = new DateTimeOffset(bettingDataLC_WM.requestDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2,
                bettingDataLC_WM.RealBetAmount,
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_WM_Ins_V2", p, conn, trans);

                if (result > 0)
                {
                    if (bettingDataLC_WM.money != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataLC_WM.money,
                            (int)Tp.WM,
                            mixUniqueId,
                            gameType,
                            TpCategory.LiveCasino,
                            mixUniqueId,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
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

        public int CancelBetLC_WM(TpCustInfo tpCustInfo, BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.WM_LiveCasino;

            afterAmount = 0;
            int result = 0;
            string mixUniqueId = $"{bettingDataLC_WM.gameno}_{bettingDataLC_WM.user}";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            result = _mainDbClient.Execute("Service_BettransLC_WM_CancelBet_Set", new { dealid = bettingDataLC_WM.dealid }, conn, trans);

            if (result > 0)
            {
                // 1:下注单
                if (bettingDataLC_WM.category == "1")
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettingDataLC_WM.money),
                        (int)Tp.WM,
                        bettingDataLC_WM.dealid,
                        gameType, TpCategory.LiveCasino,
                        mixUniqueId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "CancelBet",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }
                else
                {
                    // 2:小费单
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(bettingDataLC_WM.money),
                        (int)Tp.WM,
                        TransType.Tip,
                        $"Tip: {Math.Abs(bettingDataLC_WM.money):#,##0.##}",
                        bettingDataLC_WM.dealid,
                        out afterAmount,
                        conn,
                        trans,
                        AllowNegative.Allow
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
                var p = new DynamicParameters(new
                {
                    TransId = mixUniqueId,
                    user = bettingDataLC_WM.user,
                    money = bettingDataLC_WM.money,
                    dealid = bettingDataLC_WM.dealid,
                    gtype = bettingDataLC_WM.gtype,
                    type = bettingDataLC_WM.type,
                    betdetail = bettingDataLC_WM.betdetail,
                    gameno = bettingDataLC_WM.gameno,
                    code = bettingDataLC_WM.code,
                    category = bettingDataLC_WM.category,
                    requestUnix = new DateTimeOffset(bettingDataLC_WM.requestDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    TicketType = bettingDataLC_WM.category == "1" ? (int)BettransType.Bet : (int)BettransType.Tip,
                    TicketStatus = TicketStatus.Refund,
                    SettleFlag = SettleFlag.Settled,
                    bettingDataLC_WM.RealBetAmount,
                });

                p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                _mainDbClient.Execute("Service_BettransLC_WM_CancelBet_Ins_V2", p, conn, trans);
                var refBettransLC = p.Get<int>("out_result");

                if (refBettransLC == 2)
                {
                    // insert cancel record
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                result = -1;
            }

            return result;
        }

        public int ResettleLC_WM(TpCustInfo tpCustInfo, BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.WM_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            string mixUniqueId = $"{bettingDataLC_WM.gameno}_{bettingDataLC_WM.user}";
            var word = "_cancel";
            if (bettingDataLC_WM.gameno.Contains(word))
            {
                mixUniqueId = $"{bettingDataLC_WM.gameno.Replace(word, "")}_{bettingDataLC_WM.user}";
            }
            var p = new DynamicParameters(new
            {
                TransId = mixUniqueId,
                user = bettingDataLC_WM.user,
                money = bettingDataLC_WM.money,
                dealid = bettingDataLC_WM.dealid,
                gtype = bettingDataLC_WM.gtype,
                type = bettingDataLC_WM.type,
                betdetail = bettingDataLC_WM.betdetail,
                gameno = bettingDataLC_WM.gameno,
                code = bettingDataLC_WM.code,
                category = bettingDataLC_WM.category,
                requestUnix = new DateTimeOffset(bettingDataLC_WM.requestDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                bettingDataLC_WM.RealBetAmount,
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_WM_Resettle_Set_V2", p, conn, trans);

                if (result > 0)
                {
                    if (bettingDataLC_WM.money != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataLC_WM.money,
                            (int)Tp.WM,
                            mixUniqueId,
                            gameType,
                            TpCategory.LiveCasino,
                            mixUniqueId,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Resettle",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
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

        public SettleData_WM GetUnsettled_WM(string TransId)
        {
            return _mainDbClient.QueryFirst<SettleData_WM>("Service_BettransLC_WM_Unsettle_Get_V3", new { TransId = TransId });
        }

        public SettleData GetSettle_TpWagerWM(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData>("Service_TpWagerWM_Settle_Get", new { TransId = TransId });
        }

        public int SetSettleFlag_TpWagerWM(string TransId)
        {
            return _wagerDbClient.Execute("Service_TpWagerWM_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        public int SetSettleFlag_BettransLC_WM(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransLC_WM_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region N2Live

        public int CancelBetLC_N2Live(TpCustInfo tpCustInfo, CancelBetDataLC_N2Live cancelBetDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;

            afterAmount = 0;
            int result = 0;

            var p = new DynamicParameters(new
            {
                tradeid = cancelBetDataLC_N2Live.request.tradeid
            });
            //p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var check = _mainDbClient.QueryFirst<BettransData_N2Live>("Service_BettransLC_N2Live_Get", trans, new { tradeid = cancelBetDataLC_N2Live.request.tradeid });
            if (check == null)
            {
                // no record
                return -2;
            }
            else if (check.TicketStatus == 3)
            {
                // already canceled 
                return -1;
            }

            result = _mainDbClient.Execute("Service_BettransLC_N2Live_CancelBet_Set", p, conn, trans);

            if (result > 0)
            {
                //decimal stake = p.Get<decimal>("out_stake");
                decimal stake = check.Stake;
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(stake),
                    (int)Tp.N2Live,
                    cancelBetDataLC_N2Live.request.tradeid,
                    gameType, TpCategory.LiveCasino,
                    cancelBetDataLC_N2Live.request.tradeid,
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "CancelBet",
                    out bonusId,
                    conn,
                    trans,
                    AllowNegative.Allow
                );

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            return result;
        }

        public int PayoutLC_N2Live(TpCustInfo tpCustInfo, PayoutDataLC_N2Live payoutDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;
            string TransId = $"{payoutDataLC_N2Live.request.gameid}_{payoutDataLC_N2Live.request.userid}";

            var ticketStatus = (int)TicketStatus.UnSettled;
            var settleFlag = (int)SettleFlag.EndGame;
            if (payoutDataLC_N2Live.IsVoid)
            {
                ticketStatus = (int)TicketStatus.Refund;
                settleFlag = (int)SettleFlag.Settled;
            }

            var p = new DynamicParameters(new
            {
                TransId = TransId,
                tradeid = payoutDataLC_N2Live.request.tradeid,
                userid = payoutDataLC_N2Live.request.userid,
                vendorid = payoutDataLC_N2Live.request.vendorid,
                gameid = payoutDataLC_N2Live.request.gameid,
                gamecode = payoutDataLC_N2Live.request.gamecode,
                Payout = payoutDataLC_N2Live.request.totalamount,
                PayoutTrades = JsonConvert.SerializeObject(payoutDataLC_N2Live.request.trades),
                //totalamount = payoutDataLC_N2Live.request.totalamount,
                //trades = JsonConvert.SerializeObject(payoutDataLC_N2Live.request.trades),
                timeUnix = new DateTimeOffset(DateTime.Parse(payoutDataLC_N2Live.request.timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Payout,
                TicketStatus = ticketStatus,
                SettleFlag = settleFlag
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = _mainDbClient.QueryFirst<BettransData_N2Live>("Service_BettransLC_N2Live_Get", trans, new { tradeid = payoutDataLC_N2Live.request.tradeid });
                if (check != null && check.TicketType == 4)
                {
                    // already payout 
                    return -1;
                }

                result = _mainDbClient.Execute("Service_BettransLC_N2Live_Payout_Set", p, conn, trans);

                if (result > 0)
                {
                    if (payoutDataLC_N2Live.request.totalamount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            payoutDataLC_N2Live.request.totalamount,
                            (int)Tp.N2Live,
                            payoutDataLC_N2Live.request.tradeid,
                            gameType,
                            TpCategory.LiveCasino,
                            payoutDataLC_N2Live.request.tradeid,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                         );
                    }

                    if (result > 0)
                    {
                        if (!payoutDataLC_N2Live.IsVoid)
                        {
                            var insertTpResults = _tpUnsettleService.InsertTpUnsettle(Tp.N2Live, GameType.N2Live_LiveCasino, TransId, trans);
                            if (insertTpResults <= 0)
                            {
                                result = 0;
                            }
                        }

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
                throw mySqlEx;
            }

            return result;
        }

        public int SmartCancelBetLC_N2Live(TpCustInfo tpCustInfo, SmartCancelBetDataLC_N2Live smartCancelBetDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;

            afterAmount = 0;
            int result = 0;

            var p = new DynamicParameters(new
            {
                schemeid = smartCancelBetDataLC_N2Live.request.schemeid
            });
            //p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var check = _mainDbClient.QueryFirst<BettransData_N2Live>("Service_SmartBettransLC_N2Live_Get", trans, new { schemeid = smartCancelBetDataLC_N2Live.request.schemeid });
            if (check == null)
            {
                // no record
                return -2;
            }
            else if (check.TicketStatus == 3)
            {
                // already canceled 
                return -1;
            }

            result = _mainDbClient.Execute("Service_SmartBettransLC_N2Live_CancelBet_Set", p, conn, trans);

            if (result > 0)
            {
                //decimal stake = p.Get<decimal>("out_stake");
                decimal stake = check.Stake;
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(stake),
                    (int)Tp.N2Live,
                    smartCancelBetDataLC_N2Live.request.schemeid,
                    gameType, TpCategory.LiveCasino,
                    smartCancelBetDataLC_N2Live.request.schemeid,
                    TransType.Refunds,
                    null,
                    out afterAmount,
                    "CancelBet",
                    out bonusId,
                    conn,
                    trans,
                    AllowNegative.Allow
                );

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            return result;
        }

        public int SmartPayoutLC_N2Live(TpCustInfo tpCustInfo, SmartPayoutDataLC_N2Live smartPayoutDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            var p = new DynamicParameters(new
            {
                schemeid = smartPayoutDataLC_N2Live.request.schemeid,
                userid = smartPayoutDataLC_N2Live.request.userid,
                vendorid = smartPayoutDataLC_N2Live.request.vendorid,
                Payout = smartPayoutDataLC_N2Live.request.amount,
                reason = smartPayoutDataLC_N2Live.request.reason,
                timeUnix = smartPayoutDataLC_N2Live.request.timeUnix,
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = _mainDbClient.QueryFirst<BettransData_N2Live>("Service_SmartBettransLC_N2Live_Get", trans, new { schemeid = smartPayoutDataLC_N2Live.request.schemeid });
                if (check == null && check.TicketType == 4)
                {
                    // already payout 
                    return -1;
                }

                result = _mainDbClient.Execute("Service_SmartBettransLC_N2Live_Payout_Set", p, conn, trans);

                if (result > 0)
                {
                    if (smartPayoutDataLC_N2Live.request.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            smartPayoutDataLC_N2Live.request.amount,
                            (int)Tp.N2Live,
                            smartPayoutDataLC_N2Live.request.schemeid,
                            gameType,
                            TpCategory.LiveCasino,
                            smartPayoutDataLC_N2Live.request.schemeid,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                         );
                    }

                    if (result > 0)
                    {
                        var insertTpResults = _tpUnsettleService.InsertTpUnsettle(Tp.N2Live, GameType.N2Live_LiveCasino, smartPayoutDataLC_N2Live.request.schemeid, trans);
                        if (insertTpResults <= 0)
                        {
                            result = 0;
                        }
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
                throw mySqlEx;
            }

            return result;
        }

        public int BonusLC_N2Live(TpCustInfo tpCustInfo, BonusDataLC_N2Live bonusDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            var p = new DynamicParameters(new
            {
                userid = bonusDataLC_N2Live.request.userid,
                vendorid = bonusDataLC_N2Live.request.vendorid,
                bonusid = bonusDataLC_N2Live.request.bonusid,
                creditdate = bonusDataLC_N2Live.request.creditdate,
                amount = bonusDataLC_N2Live.request.amount,
                desc = bonusDataLC_N2Live.request.desc,
                creditUnix = new DateTimeOffset(DateTime.Parse(bonusDataLC_N2Live.request.creditdate), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                timeUnix = new DateTimeOffset(DateTime.Parse(bonusDataLC_N2Live.request.timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_BonusLC_N2Live_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (bonusDataLC_N2Live.request.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bonusDataLC_N2Live.request.amount,
                            (int)Tp.N2Live,
                            bonusDataLC_N2Live.request.bonusid,
                            gameType,
                            TpCategory.LiveCasino,
                            bonusDataLC_N2Live.request.bonusid,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            $"{bonusDataLC_N2Live.request.bonusid} Prize",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
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

        public int EventLC_N2Live(TpCustInfo tpCustInfo, EventDataLC_N2Live eventDataLC_N2Live, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;
            decimal amount = 0;
            var ticketStatus = (int)TicketStatus.Refund;

            if (string.Equals(eventDataLC_N2Live.request.trxtype, "deposit", StringComparison.OrdinalIgnoreCase))
            {
                amount = Math.Abs(eventDataLC_N2Live.request.amount);
                ticketStatus = (int)TicketStatus.Win;
            }
            else if (string.Equals(eventDataLC_N2Live.request.trxtype, "withdraw", StringComparison.OrdinalIgnoreCase))
            {
                amount = Math.Abs(eventDataLC_N2Live.request.amount) * -1;
                ticketStatus = (int)TicketStatus.Lost;
            }
            else if (string.Equals(eventDataLC_N2Live.request.trxtype, "canceldeposit", StringComparison.OrdinalIgnoreCase))
            {
                amount = Math.Abs(eventDataLC_N2Live.request.amount) * -1;
                ticketStatus = (int)TicketStatus.Refund;
            }
            else if (string.Equals(eventDataLC_N2Live.request.trxtype, "cancelwithdraw", StringComparison.OrdinalIgnoreCase))
            {
                amount = Math.Abs(eventDataLC_N2Live.request.amount);
                ticketStatus = (int)TicketStatus.Refund;
            }

            var p = new DynamicParameters(new
            {
                userid = eventDataLC_N2Live.request.userid,
                vendorid = eventDataLC_N2Live.request.vendorid,
                eventtrxid = eventDataLC_N2Live.request.eventtrxid,
                eventtype = eventDataLC_N2Live.request.eventtype,
                trxtype = eventDataLC_N2Live.request.trxtype,
                amount = eventDataLC_N2Live.request.amount,
                desc = eventDataLC_N2Live.request.desc,
                timeUnix = new DateTimeOffset(DateTime.Parse(eventDataLC_N2Live.request.timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Tip,
                TicketStatus = ticketStatus,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.Execute("Service_RewardLC_N2Live_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            amount,
                            (int)Tp.N2Live,
                            TransType.Tip,
                            $"Tip: {Math.Abs(amount):#,##0.##}",
                            eventDataLC_N2Live.request.eventtrxid,
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

        public SettleData_N2Live GetSettle_TpWagerLC_N2Live(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData_N2Live>("Service_TpWagerLC_N2Live_Get_V2", new { TransId = TransId });
        }

        public SettleData GetSettle_TpSmartWagerLC_N2Live(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData>("Service_TpSmartWagerLC_N2Live_Get", new { TransId = TransId });
        }

        public IEnumerable<SettleData> GetUnsettledList_BettransLC_N2Live()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransLC_N2Live_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<SettleData_N2Live_Smart> GetUnsettledList_TpSmartWagerLC_N2Live()
        {
            return _wagerDbClient.Query<SettleData_N2Live_Smart>("Service_TpSmartWagerLC_N2Live_Unsettle_Get_V2", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_BettransLC_N2Live(string TransId, int SettleFlag)
        {
            return _mainDbClient.Execute("Service_BettransLC_N2Live_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId, SettleFlag = SettleFlag });
        }

        public int SetSettleFlag_TpWagerLC_N2Live(string TransId)
        {
            return _wagerDbClient.Execute("Service_TpWagerLC_N2Live_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        public int SetSettleFlag_BettransSmartLC_N2Live(string TransId, int SettleFlag)
        {
            return _wagerDbClient.Execute("Service_SmartBettransLC_N2Live_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId, SettleFlag = SettleFlag });
        }

        public int SetSettleFlag_TpSmartWagerLC_N2Live(string TransId)
        {
            return _wagerDbClient.Execute("Service_TpSmartWagerLC_N2Live_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region PT

        public IEnumerable<SettleData> GetUnsettledListLC_PT()
        {
            return _wagerDbClient.Query<SettleData>("Service_TpWagerLC_PT_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerLC_PT(long TransId)
        {
            return _wagerDbClient.Execute("Service_TpWagerLC_PT_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region MG

        public MGPayoutResult PayoutLC_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            var result = 0;

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataRNG_MG.creationTimeMs).DateTime;

                var bettransMg = _partitionService.Query<BettransLC_MG>(partitionTime, "Service_BettransLC_MG_BetId_Get", new { bettingDataRNG_MG.betId });
                var bettrans = bettransMg.ToList();

                // 已結束局
                if (bettrans.Any(a => a.Completed))
                {
                    logger.LogError($"{bettingDataRNG_MG.betId} is completed");

                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }

                // 檢查注單是否已經存在
                if (bettrans.Any(a => a.TxnId == bettingDataRNG_MG.txnId))
                {
                    logger.LogError($"{bettingDataRNG_MG.txnId} exist");

                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                        ExtTxnId = bettrans.FirstOrDefault(b => b.TxnId == bettingDataRNG_MG.txnId).trx_id
                    };
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();

                bettingDataRNG_MG.TransType = "C";
                bettingDataRNG_MG.TicketType = (int)BettransType.Payout;
                bettingDataRNG_MG.TicketStatus = bettingDataRNG_MG.amount == 0 ? (int)TicketStatus.Draw : (int)TicketStatus.Win;
                bettingDataRNG_MG.SettleFlag = (bettingDataRNG_MG.completed) ? (int)SettleFlag.EndGame : (int)SettleFlag.Running;
                var p = new DynamicParameters(bettingDataRNG_MG);
                p.Add("extTxnId", dbType: DbType.Int64, direction: ParameterDirection.Output);

                result = _partitionService.Execute(partitionTime, "Service_BettransLC_MG_Ins_V2", p, trans);
                var extTxnId = p.Get<long>("extTxnId");

                if (result > 0 && bettingDataRNG_MG.amount > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_MG.amount,
                        (int)Tp.MG,
                        bettingDataRNG_MG.betId,
                        gameType,
                        TpCategory.LiveCasino,
                        bettingDataRNG_MG.txnId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout: {Math.Abs(bettingDataRNG_MG.amount):#,##0.##}",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                        );
                }
                if (result > 0)
                {
                    if (bettingDataRNG_MG.completed)
                    {
                        _tpUnsettleService.InsertTpUnsettle(Tp.MG, GameType.MG_LiveCasino, bettingDataRNG_MG.betId, trans, partitionTime);
                    }
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }

                return new MGPayoutResult()
                {
                    Result = result,
                    AfterAmount = afterAmount,
                    ExtTxnId = extTxnId
                };
            }
            catch (MySqlException mySqlEx)
            {
                // 重複注單派彩
                if (mySqlEx.Number == 1062)
                {
                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                    };
                }
                else
                {
                    logger.LogError(mySqlEx, $"PayoutLC_MG failed: {JsonConvert.SerializeObject(bettingDataRNG_MG)}");
                    throw;
                }
            }
        }

        public async Task<MGCancelResult> CancelBetLC_MG(TpCustInfo tpCustInfo, CancelDataRNG_MG cancelDataRng_MG, int gameType)
        {
            int bonusId = 0;
            var afterAmount = 0M;

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(cancelDataRng_MG.creationTimeMs).DateTime;

                var bettransMg = _partitionService.Query<BettransLC_MG>(partitionTime, "Service_BettransLC_MG_txnId_Get", new { txnId = cancelDataRng_MG.txnId });
                var bettrans = bettransMg.ToList();

                // 不存在有效下注
                if (bettrans.Count == 0 || bettrans.All(a => a.TicketType != 1))
                {
                    logger.LogError($"No valid bet {cancelDataRng_MG.betId}");
                    return new MGCancelResult() { Result = 1 };
                }

                // 已結束局
                if (bettrans.Any(a => a.Completed))
                {
                    logger.LogError($"{cancelDataRng_MG.betId} is completed");
                    return new MGCancelResult() { Result = 1 };
                }

                var bet = bettrans.FirstOrDefault(a => a.TxnId == cancelDataRng_MG.txnId);
                // 注單不存在
                if (bet == null)
                {
                    logger.LogInformation($"no txnId: {cancelDataRng_MG.txnId}");
                    return new MGCancelResult() { Result = 1 };
                }
                // 玩家或金額不符
                if (bet.PlayerId != cancelDataRng_MG.playerId || Math.Abs(bet.Amount) != cancelDataRng_MG.amount)
                {
                    logger.LogError($"player or amount doesn't match:{JsonConvert.SerializeObject(bet)}");
                    throw new MGException(MGErrorCode.TransactionIncompatible, $"{JsonConvert.SerializeObject(cancelDataRng_MG)}");
                }

                var betId = (!string.IsNullOrEmpty(cancelDataRng_MG.betId)) ? cancelDataRng_MG.betId : bet.BetId;

                //已取消
                if (bet.TicketType == (int)BettransType.CancelBet)
                {
                    logger.LogError($"betId:{cancelDataRng_MG.betId}, txnId:{cancelDataRng_MG.txnId} is canceled");
                    return new MGCancelResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                        ExtTxnId = bettrans.FirstOrDefault(b => b.TxnId == cancelDataRng_MG.txnId).trx_id
                    };
                }

                var result = _partitionService.Execute(partitionTime, "Service_BettransLC_MG_CancelBet_Set", new { betId, cancelDataRng_MG.txnId }, trans);

                if (result > 0 && cancelDataRng_MG.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        cancelDataRng_MG.amount,
                        (int)Tp.MG,
                        cancelDataRng_MG.betId,
                        gameType, TpCategory.LiveCasino,
                        cancelDataRng_MG.txnId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        $"Refund {cancelDataRng_MG.amount:#,##0.##}",
                        out bonusId,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }
                return new MGCancelResult()
                {
                    Result = result,
                    AfterAmount = afterAmount,
                    ExtTxnId = bettrans.FirstOrDefault(b => b.TxnId == cancelDataRng_MG.txnId).ExtTxnId
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, $"CancelBetLC_MG failed: {JsonConvert.SerializeObject(cancelDataRng_MG)}");
                throw;
            }
        }

        public int SetSettleFlag_BettransLC_MG(string spName, string transId, long transTime)
        {
            var result = 0;
            var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(transTime).DateTime;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();
            result = _partitionService.Execute(partitionTime, "Service_BettransLC_MG_Settle_Set", new DynamicParameters(new { TransId = transId }), trans);
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

        #region EVO

        public int CancelBetLC_EVO(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var precheck = _mainDbClient.Query<BettransData_EVO>("Service_BettransLC_EVO_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });

            if (precheck.Where(x => x.TicketType is (int)BettransType.Payout or (int)BettransType.CancelBet).Any())
            {
                // already payout
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                return -1;
            }

            var p = new DynamicParameters(new
            {
                TransactionId = bettingData_EVO.transaction.id,
                TransactionRefId = bettingData_EVO.transaction.refId,
                userId = bettingData_EVO.userId,
                uuid = bettingData_EVO.uuid,
                currency = bettingData_EVO.currency,
                amount = bettingData_EVO.transaction.amount,
                transaction = JsonConvert.SerializeObject(bettingData_EVO.transaction),
                game = JsonConvert.SerializeObject(bettingData_EVO.game),
                GameId = bettingData_EVO.game.id,
                GameType = bettingData_EVO.game.type,
                GameDetailsId = bettingData_EVO.game.details.table.id,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                CurrencyId = tpCustInfo.CurrencyId,
                TicketType = (int)BettransType.CancelBet,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (int)SettleFlag.Settled
            });

            result = _mainDbClient.Execute("Service_BettransLC_EVO_CancelBet_Set", p, conn, trans);

            if (result > 0)
            {
                if (bettingData_EVO.transaction.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettingData_EVO.transaction.amount),
                        (int)Tp.EVO,
                        bettingData_EVO.transaction.refId,
                        (int)GameType.EVO_LiveCasino,
                        TpCategory.LiveCasino,
                        bettingData_EVO.transaction.id,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "CancelBet",
                        out bonusId,
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
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransLC_EVO_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
                if (!check.Any())
                {
                    var cancelInsert = _mainDbClient.Execute("Service_BettransLC_EVO_Ins_V2", p, conn, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -2;
                    }
                }
                var subChk = check.Where(x => x.TicketType == 2);
                if (subChk.Any())
                {
                    // had cancel
                    return -1;
                }
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            return result;
        }

        public int PayoutLC_EVO(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransLC_EVO_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
                if (check.Where(x => x.TicketType is (int)BettransType.Payout or (int)BettransType.CancelBet).Any())
                {
                    // already payout
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return -1;
                }
                if (!check.Where(x => x.TicketType == (int)BettransType.Bet).Any())
                {
                    // no bet or cancel data
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return -2;
                }

                var p = new DynamicParameters(new
                {
                    TransactionId = bettingData_EVO.transaction.id,
                    TransactionRefId = bettingData_EVO.transaction.refId,
                    userId = bettingData_EVO.userId,
                    uuid = bettingData_EVO.uuid,
                    currency = bettingData_EVO.currency,
                    amount = bettingData_EVO.transaction.amount,
                    transaction = JsonConvert.SerializeObject(bettingData_EVO.transaction),
                    game = JsonConvert.SerializeObject(bettingData_EVO.game),
                    GameId = bettingData_EVO.game.id,
                    GameType = bettingData_EVO.game.type,
                    GameDetailsId = bettingData_EVO.game.details.table.id,
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = bettingData_EVO.transaction.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                result = _mainDbClient.Execute("Service_BettransLC_EVO_Ins_V2", p, conn, trans);

                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingData_EVO.transaction.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_EVO.transaction.amount),
                            (int)Tp.EVO,
                            bettingData_EVO.transaction.refId,
                            (int)GameType.EVO_LiveCasino,
                            TpCategory.LiveCasino,
                            bettingData_EVO.transaction.id,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
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

        public int PromoPayoutLC_EVO(TpCustInfo tpCustInfo, Reward_EVO data, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = _mainDbClient.QueryFirstAsync<int>("Service_Reward_EVO_Ins", trans, new DynamicParameters(data)).Result;

                if (result > 0)
                {
                    int bonusId = 0;

                    if (data.PromoTransactionAmount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(data.PromoTransactionAmount),
                            (int)Tp.EVO,
                            data.PromoTransactionId,
                            (int)GameType.EVO_LiveCasino,
                            TpCategory.LiveCasino,
                            data.PromoTransactionId,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            $"{data.PromoTransactionId} Prize",
                            out bonusId,
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

        public IEnumerable<SettleData> GetUnsettledList_EVO()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransLC_EVO_Unsettle_Get");
        }

        public SettleData GetUnsettled_EVO(string transId)
        {
            return _mainDbClient.QueryFirst<SettleData>("Service_BettransLC_EVO_Unsettle_ByTrans_Get", parameters: new DynamicParameters(new { _TransId = transId }));
        }

        public int SetSettleFlag_EVO(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransLC_EVO_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        public IEnumerable<SettledReward_EVO> GetUnsettledRewardList_EVO()
        {
            return _mainDbClient.Query<SettledReward_EVO>("Service_Reward_EVO_Unsettle_Get");
        }

        public SettledReward_EVO GetUnsettledReward_EVO(string transId)
        {
            return _mainDbClient.QueryFirst<SettledReward_EVO>("Service_Reward_EVO_Unsettle_ByTrans_Get", parameters: new DynamicParameters(new { _TransId = transId }));
        }

        public int SetRewardSettleFlag_EVO(int sq)
        {
            return _mainDbClient.Execute("Service_Reward_EVO_Settle_Set", parameters: new DynamicParameters(new { _SQ = sq }));
        }

        public SettleData_EVO GetSettle_TpWagerLC_EVO(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData_EVO>("Service_TpWagerLC_EVO_Settle_Get_V2", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        public IEnumerable<Reward_EVO> GetExistReward_EVO(PromoData_EVO data, int tpId, int tpGameType)
        {
            var query = new { userId = data.userId, PromoTransactionId = data.promoTransaction.id, TpId = tpId, TpGameType = tpGameType };
            return _mainDbClient.Query<Reward_EVO>("Service_Reward_EVO_Check", CommandType.StoredProcedure, new DynamicParameters(query));
        }

        #endregion

        #region Yeebet

        public IEnumerable<BettingDataLC_Yeebet> IsExistBet_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data)
        {
            var query = new { serialnumber = data.serialnumber };
            return _partitionService.Query<BettingDataLC_Yeebet>(partitionTime, "Service_BettransLC_Yeebet_Cancel_Check", query);
        }

        public bool Payout_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data, MySqlTransaction trans, out int SQ)
        {
            SQ = 0;
            var insertSQ = _partitionService.Execute(partitionTime, "Service_BettransLC_Yeebet_Ins", data, trans);

            if (insertSQ == 0)
                return false;

            SQ = insertSQ;
            return true;
        }

        public bool Refund_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data, MySqlTransaction trans, out int SQ)
        {
            SQ = 0;
            var insertSQ = _partitionService.Execute(partitionTime, "Service_BettransLC_Yeebet_Ins", data, trans);

            if (insertSQ == 0)
                return false;
            SQ = insertSQ;
            return true;
        }

        public bool Rollback_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data, MySqlTransaction trans)
        {
            var insertSQ = _partitionService.Execute(partitionTime, "Service_BettransLC_Yeebet_Rollback_Set",
                new DynamicParameters(new { notifyid = data.notifyid, type = data.type, serialnumber = data.serialnumber, errmsg = data.errmsg }), trans);

            if (insertSQ == 0)
                return false;

            return true;
        }


        public bool SetBalance_Yeebet(TpCustInfo tpCustInfo, BettingDataLC_Yeebet data, TransType transType, string memo,
                                       out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {

            var result = PayoutV2(
                tpCustInfo,
                data.amount,
                (int)Tp.Yeebet,
                data.serialnumber,
                (int)GameType.Yeebet_LiveCasino,
                TpCategory.LiveCasino,
                data.serialnumber,
                transType,
                null,
                out afterAmount,
                $"{memo} - {data.notifyid} ",
                out bonusId,
                conn,
                trans
            );

            if (result == 0)
                return false;

            return true;
        }

        public bool InsertUnsettle_Yeebet(DateTime partitionTime, string serialnumber, MySqlTransaction trans)
        {
            var result = _tpUnsettleService.InsertTpUnsettle(Tp.Yeebet, GameType.Yeebet_LiveCasino, serialnumber, trans, partitionTime);

            if (result == 0)
                return false;

            return true;
        }

        public IEnumerable<SettleData_Yeebet> GetSettle_TpWagerLC_Yeebet(string serialnumber)
        => _wagerDbClient.Query<SettleData_Yeebet>("Service_TpWagerLC_Yeebet_Get", parameters: new DynamicParameters(new { _serialnumber = serialnumber }));

        #endregion

        #region BG

        public IEnumerable<BettingDataLC_BG> IsExistBet_BG(DateTime partitionTime, BettingDataLC_BG data)
        {
            var query = new { orderId = data.orderId };
            return _partitionService.Query<BettingDataLC_BG>(partitionTime, "Service_BettransLC_BG_Cancel_Check", query);
        }

        public IEnumerable<BettingDataLC_BG> IsExistLastPayout_BG(DateTime partitionTime, BettingDataLC_BG data)
        {
            var query = new { orderId = data.orderId };
            return _partitionService.Query<BettingDataLC_BG>(partitionTime, "Service_BettransLC_BG_ReSettle_Check", query);
        }

        public bool Payout_BG(DateTime partitionTime, BettingDataLC_BG data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransLC_BG_Ins", data, trans);

            if (result == 0)
                return false;
            return true;
        }

        public bool SetBalance_BG(TpCustInfo tpCustInfo, BettingDataLC_BG data, decimal payoutAmount, TransType transType, string memo,
                                       out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {

            var result = PayoutV2(
                tpCustInfo,
                payoutAmount,
                (int)Tp.BG,
                data.orderId,
                (int)GameType.BG_LiveCasino,
                TpCategory.LiveCasino,
                data.orderId,
                transType,
                null,
                out afterAmount,
                $"{memo}_{data.random}_{data.orderId} ",
                out bonusId,
                conn,
                trans
            );

            if (result == 0)
                return false;

            return true;
        }

        public bool InsertUnsettle_BG(DateTime partitionTime, string orderId, MySqlTransaction trans)
        {
            var result = _tpUnsettleService.InsertTpUnsettle(Tp.BG, GameType.BG_LiveCasino, orderId, trans, partitionTime);

            if (result == 0)
                return false;

            return true;
        }

        public IEnumerable<SettleData_BG> GetSettle_TpWagerLC_BG(string orderId)
        => _wagerDbClient.Query<SettleData_BG>("Service_TpWagerLC_BG_Get", parameters: new DynamicParameters(new { _orderId = orderId }));


        #endregion
        
        #region WonCasino
        
        public int DeductLC_WonCasino(TpCustInfo tpCustInfo, WonCasinoDeductRequest settleDataLC_WonCasino,
            out decimal afterAmount,out string orderNo)
        {
            int bonusId = 0;
            afterAmount = 0;
            orderNo = "99999999";
            var result = 0;

            var tpId = Tp.WonCasino;
            var tpGameType = GameType.WonCasino_LiveCasino;
            var category = TpCategory.LiveCasino;
            var InsSpName = "Service_Bettrans_WonCasino_Ins";
            var CheckSpName = "Service_Bettrans_WonCasino_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(settleDataLC_WonCasino.bets.createtime).DateTime;
                var bettrans = _partitionService.Query<BettransWonCasino>(partitionTime, CheckSpName, 
                    new { id = settleDataLC_WonCasino.bets.id , username = tpCustInfo.TpUserName });
                
                //若TicketStatus已經為 Refund = 3,/*Reject, Refund, Void, Draw*/,就不給再次扣款了。
                if (bettrans.Any(i => i.TicketStatus == (int)TicketStatus.Refund))
                {
                    orderNo = bettrans.LastOrDefault().SQ.ToString();
                    return -2;
                }
                
                settleDataLC_WonCasino.username = tpCustInfo.TpUserName;
                settleDataLC_WonCasino.notifyid = settleDataLC_WonCasino.notifyid + 
                                                   settleDataLC_WonCasino.type +
                                                   settleDataLC_WonCasino.bets.id;
                var p = new DynamicParameters(new
                {
                    id = settleDataLC_WonCasino.bets.id,
                    username = settleDataLC_WonCasino.username,
                    notifyid = settleDataLC_WonCasino.notifyid,
                    amount = settleDataLC_WonCasino.amount,
                    type = settleDataLC_WonCasino.type,
                    currency = settleDataLC_WonCasino.currency,
                    appid = settleDataLC_WonCasino.appid,
                    sign = settleDataLC_WonCasino.sign,
                    gameid = settleDataLC_WonCasino.bets.gameid,
                    userid = settleDataLC_WonCasino.bets.userid,
                    userstatus = settleDataLC_WonCasino.bets.userstatus,
                    betpoint = settleDataLC_WonCasino.bets.betpoint,
                    betodds = settleDataLC_WonCasino.bets.betodds,
                    betamount = settleDataLC_WonCasino.bets.betamount,
                    commamount = settleDataLC_WonCasino.bets.commamount,
                    winlost = settleDataLC_WonCasino.bets.winlost,
                    foregift = settleDataLC_WonCasino.bets.foregift,
                    gameroundid = settleDataLC_WonCasino.bets.gameroundid,
                    gameresult = settleDataLC_WonCasino.bets.gameresult,
                    gametype = settleDataLC_WonCasino.bets.gametype,
                    gamestyle = settleDataLC_WonCasino.bets.gamestyle,
                    serialnumber = settleDataLC_WonCasino.serialnumber,
                    gameroundno = settleDataLC_WonCasino.bets.gameroundno,
                    gameno = settleDataLC_WonCasino.bets.gameno,
                    bettype = settleDataLC_WonCasino.bets.bettype,
                    cid = settleDataLC_WonCasino.bets.cid,
                    state = settleDataLC_WonCasino.bets.state,
                    createtime =  settleDataLC_WonCasino.bets.createtime,
                    settletime = settleDataLC_WonCasino.bets.settletime,
                    UTCBetTime = DateTimeOffset.FromUnixTimeSeconds(settleDataLC_WonCasino.bets.createtime).UtcDateTime,
                    UTCSettleTime = DateTimeOffset.FromUnixTimeSeconds(settleDataLC_WonCasino.bets.settletime).UtcDateTime,
                    TicketType = settleDataLC_WonCasino.TicketType,
                    TicketStatus = settleDataLC_WonCasino.TicketStatus,
                    SettleFlag = settleDataLC_WonCasino.SettleFlag
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                if (result > 0 && settleDataLC_WonCasino.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        settleDataLC_WonCasino.amount,
                        (int)tpId,
                        settleDataLC_WonCasino.serialnumber,
                        (int)tpGameType,
                        category,
                        settleDataLC_WonCasino.serialnumber,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        "Void",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    orderNo = p.Get<long>("SQ").ToString();
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
        }
        
        public int Deposit_WonCasino(TpCustInfo tpCustInfo, WonCasinoDepositRequest settleDataLC_WonCasino, 
            out decimal afterAmount,out string orderNo)
        {
            var bonusId = 0;
            var tpId = Tp.WonCasino;
            var tpGameType = GameType.WonCasino_LiveCasino;
            var category = TpCategory.LiveCasino;
            var InsSpName = "Service_Bettrans_WonCasino_Ins";
            var GetSpName = "Service_Bettrans_WonCasino_Get";
            afterAmount = 0;
            orderNo = "99999999";

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.Now;
                var bettrans = _partitionService.Query<BettransWonCasino>(partitionTime, GetSpName, 
                    new { id = settleDataLC_WonCasino.bets.id , username = tpCustInfo.TpUserName });
                //若Deduct無單號，直接回傳-3。
                if (!bettrans.Any(i => i.id == settleDataLC_WonCasino.bets.id))
                {
                    orderNo = bettrans.LastOrDefault()?.SQ.ToString() ?? orderNo;
                    return -3;
                }
                
                //若TicketStatus已經為 Refund = 3,/*Reject, Refund, Void, Draw*/,就不給再次結算。
                if (bettrans.Any(i => i.TicketStatus == (int)TicketStatus.Refund))
                {
                    orderNo = bettrans.LastOrDefault().SQ.ToString();
                    return -2;
                }

                settleDataLC_WonCasino.username = tpCustInfo.TpUserName;
                settleDataLC_WonCasino.notifyid = settleDataLC_WonCasino.notifyid + 
                                                  settleDataLC_WonCasino.type +
                                                  settleDataLC_WonCasino.bets.id;
                var p = new DynamicParameters(new
                {
                    id = settleDataLC_WonCasino.bets.id,
                    username = settleDataLC_WonCasino.username,
                    notifyid = settleDataLC_WonCasino.notifyid,
                    amount = settleDataLC_WonCasino.amount,
                    type = settleDataLC_WonCasino.type,
                    currency = settleDataLC_WonCasino.currency,
                    appid = settleDataLC_WonCasino.appid,
                    sign = settleDataLC_WonCasino.sign,
                    gameid = settleDataLC_WonCasino.bets.gameid,
                    userid = settleDataLC_WonCasino.bets.userid,
                    userstatus = settleDataLC_WonCasino.bets.userstatus,
                    betpoint = settleDataLC_WonCasino.bets.betpoint,
                    betodds = settleDataLC_WonCasino.bets.betodds,
                    betamount = settleDataLC_WonCasino.bets.betamount,
                    commamount = settleDataLC_WonCasino.bets.commamount,
                    winlost = settleDataLC_WonCasino.bets.winlost,
                    foregift = settleDataLC_WonCasino.bets.foregift,
                    gameroundid = settleDataLC_WonCasino.bets.gameroundid,
                    gameresult = settleDataLC_WonCasino.bets.gameresult,
                    gametype = settleDataLC_WonCasino.bets.gametype,
                    gamestyle = settleDataLC_WonCasino.bets.gamestyle,
                    serialnumber = settleDataLC_WonCasino.serialnumber,
                    gameroundno = settleDataLC_WonCasino.bets.gameroundno,
                    gameno = settleDataLC_WonCasino.bets.gameno,
                    bettype = settleDataLC_WonCasino.bets.bettype,
                    cid = settleDataLC_WonCasino.bets.cid,
                    state = settleDataLC_WonCasino.bets.state,
                    createtime =  settleDataLC_WonCasino.bets.createtime,
                    settletime = settleDataLC_WonCasino.bets.settletime,
                    UTCBetTime = DateTimeOffset.FromUnixTimeSeconds(settleDataLC_WonCasino.bets.createtime).UtcDateTime,
                    UTCSettleTime = DateTimeOffset.FromUnixTimeSeconds(settleDataLC_WonCasino.bets.settletime).UtcDateTime,
                    TicketType = settleDataLC_WonCasino.TicketType,
                    TicketStatus = settleDataLC_WonCasino.TicketStatus,
                    SettleFlag = (int)SettleFlag.EndGame
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                if (result > 0)
                {
                    switch (settleDataLC_WonCasino.type)
                    {
                        case (int)WonCasinoTypeEnum.Cancel:
                            result = PayoutV2(
                                tpCustInfo,
                                settleDataLC_WonCasino.amount,
                                (int)tpId,
                                settleDataLC_WonCasino.serialnumber,
                                (int)tpGameType,
                                category,
                                settleDataLC_WonCasino.serialnumber,
                                TransType.Refunds,
                                null,
                                out afterAmount,
                                "Void",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                        case (int)WonCasinoTypeEnum.Settle:
                            result = PayoutV2(
                                tpCustInfo,
                                Math.Abs(settleDataLC_WonCasino.amount),
                                (int)tpId,
                                settleDataLC_WonCasino.serialnumber,
                                (int)tpGameType,
                                category,
                                settleDataLC_WonCasino.serialnumber,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"Payout",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                    }

                    if (result > 0)
                    {
                        orderNo = p.Get<long>("SQ").ToString();
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
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }
        
        public int Rollback_WonCasino(TpCustInfo tpCustInfo ,RollbackWonCasino rollbackDataLC_WonCasino, out decimal afterAmount)
        {
            var bonusId = 0;
            var tpId = Tp.WonCasino;
            var tpGameType = GameType.WonCasino_LiveCasino;
            var category = TpCategory.LiveCasino;
            var SetSpName = "Service_Bettrans_WonCasino_Ins";
            afterAmount = 0;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.Now;
                
                rollbackDataLC_WonCasino.username = tpCustInfo.TpUserName;
                var p = new DynamicParameters(rollbackDataLC_WonCasino);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, SetSpName, p, trans);

                if (result > 0)
                {
                    switch (rollbackDataLC_WonCasino.type)
                    {
                        case (int)WonCasinoTypeEnum.Bet:
                            result = PayoutV2(
                                tpCustInfo,
                                Math.Abs(rollbackDataLC_WonCasino.amount),
                                (int)tpId,
                                rollbackDataLC_WonCasino.serialnumber,
                                (int)tpGameType,
                                category,
                                rollbackDataLC_WonCasino.serialnumber,
                                TransType.Refunds,
                                null,
                                out afterAmount,
                                "Void Rollback",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                        case (int)WonCasinoTypeEnum.Cancel:
                            result = PayoutV2(
                                tpCustInfo,
                                rollbackDataLC_WonCasino.amount,
                                (int)tpId,
                                rollbackDataLC_WonCasino.serialnumber,
                                (int)tpGameType,
                                category,
                                rollbackDataLC_WonCasino.serialnumber,
                                TransType.Undo,
                                null,
                                out afterAmount,
                                "Cancel Rollback",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                        case (int)WonCasinoTypeEnum.Settle:
                            result = PayoutV2(
                                tpCustInfo,
                                rollbackDataLC_WonCasino.amount,
                                (int)tpId,
                                rollbackDataLC_WonCasino.serialnumber,
                                (int)tpGameType,
                                category,
                                rollbackDataLC_WonCasino.serialnumber,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"Settle Rollback",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                    }


                    if (result > 0)
                    {
                        _tpUnsettleService.InsertTpUnsettle(Tp.WonCasino, GameType.WonCasino_LiveCasino, rollbackDataLC_WonCasino.id.ToString(), trans);
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
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
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
    }
}
