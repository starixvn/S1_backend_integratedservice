using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region SA
        public IEnumerable<PayoutDataLC_SA> GetUnsettledListLC_SA()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<PayoutDataLC_SA>("Service_BettransLC_SA_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int CancelBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SA_LiveCasino;

            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var refBettransLC = conn.Execute("Service_CancelBetLC_SA_Set", new { Txnid = bettingDataLC_SA.txn_reverse_id }, transaction: trans, commandType: CommandType.StoredProcedure);

            /*BetRecordInfo betRecordInfo = new BetRecordInfo();
            betRecordInfo.TpId = (int)Tp.SA;
            betRecordInfo.Stake = bettingDataLC_SA.amount;
            betRecordInfo.TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}";
            betRecordInfo.GameType = GameType.SA_LiveCasino;
            betRecordInfo.TpUserName = bettingDataLC_SA.username;

            result = CancelBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);*/
            if (refBettransLC > 0)
            {
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_SA.amount), (int)Tp.SA, TransType.Refunds, $"Cancel Bet: {Math.Abs(bettingDataLC_SA.amount).ToString("#,##0.##")}", $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", out afterAmount, conn, trans);
                result = PayoutV2(tpCustInfo, Math.Abs(bettingDataLC_SA.amount), (int)Tp.SA, bettingDataLC_SA.txn_reverse_id, gameType, TpCategory.LiveCasino, $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", TransType.Refunds, null, out afterAmount, "CancelBet", out bonusId, conn, trans);

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            else
            {
                return -1;
            }

            return result;
        }
        public int PayoutLC_SA(TpCustInfo tpCustInfo, PayoutDataLC_SA payoutDataLC_SA, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SA_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                result = conn.Execute("Service_BettransLC_SA_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (payoutDataLC_SA.amount != 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, payoutDataLC_SA.amount, (int)Tp.SA, TransType.Settled, $"Payout: {payoutDataLC_SA.amount:#,##0.##}", $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, payoutDataLC_SA.amount, (int)Tp.SA, $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", gameType, TpCategory.LiveCasino, $"{payoutDataLC_SA.gameid}_{payoutDataLC_SA.username}", TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        conn.Execute("Service_BettransLC_SAV2_Set", new { Username = payoutDataLC_SA.username, Gameid = payoutDataLC_SA.gameid, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        public int Get_TpWagerSA_Settle(string GameID, string Username, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            var p = new DynamicParameters(new { GameID = GameID, Username = Username });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            int result = conn.Execute("Service_TpWagerSA_Settle_Get", p, commandType: CommandType.StoredProcedure);

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

        public int SetSettleFlag_SA(string GameID, string Username)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_SAV2_Set", new { Username = Username, Gameid = GameID, SettleFlag = 1 }, commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_TpWagerSA(string GameID, string Username)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerSA_Settle_Set", new { GameID = GameID, Username = Username }, commandType: CommandType.StoredProcedure);
        }
        #endregion
        #region DG
        public int CancelBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.DG_LiveCasino;

            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            result = conn.Execute("Service_CancelBettransLC_DG_Set", new { data = bettingDataLC_DG.data }, transaction: trans, commandType: CommandType.StoredProcedure);
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
                    trans.Commit();
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
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { data = bettingDataLC_DG.data, TransId = bettingDataLC_DG.TransId, Username = bettingDataLC_DG.Username, Amount = bettingDataLC_DG.Amount, Memo = (string)null, TicketType = (int)transferType });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransLC_DGV2_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataLC_DG.Amount != 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_DG.Amount, (int)Tp.DG, TransType.Payout, $"Payout: {bettingDataLC_DG.Amount.ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataLC_DG.Amount, (int)Tp.DG, bettingDataLC_DG.TransId, gameType, TpCategory.LiveCasino, bettingDataLC_DG.TransId, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        conn.Execute("Service_BettransLC_DGV2_Set", new { TransId = bettingDataLC_DG.TransId, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        public int Get_TpWagerDG_Settle(string TransId, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            var p = new DynamicParameters(new { TransId = TransId });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            int result = conn.Execute("Service_TpWagerDG_Settle_Get", p, commandType: CommandType.StoredProcedure);

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
        public IEnumerable<BettingDataLC_DG> GetUnsettledListLC_DG()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BettingDataLC_DG>("Service_BettransLC_DG_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_TpWagerDG(string transId)
        {
            long.TryParse(transId, out long lTransId);
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerDG_Settle_Set", new { TransId = lTransId }, commandType: CommandType.StoredProcedure);
        }
        #endregion

        #region AG
        public int PayoutLC_AG(TpCustInfo tpCustInfo, PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AG_LiveCasino;

            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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

                result = conn.Execute("Service_BettransLC_AGV2_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

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
        public IEnumerable<DataLC_AG> GetUnsettledListLC_AG()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<DataLC_AG>("Service_BettransLC_AGV2_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int RefundLC_AG(TpCustInfo tpCustInfo, BettingDataLC_AG_Refund bettingDataLC_AG_Refund, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AG_LiveCasino;

            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            string username = bettingDataLC_AG_Refund.Playname.Substring(bettingDataLC_AG_Refund.AgentCode.Length);

            var p = new DynamicParameters(new
            {
                transactionID = bettingDataLC_AG_Refund.TransactionID,
                playname = bettingDataLC_AG_Refund.Playname
            });

            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_CancelBetLC_AGV2_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

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
                        trans.Commit();
                    }
                }
            }
            else
            {
                trans.Rollback();
                return -1;
            }

            return result;
        }
        public IEnumerable<SettleDataLC_AG> Get_PayoutList_AG(string transactionID)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataLC_AG>("Service_BettransLC_AGV2_Payout_Get", new { transactionID = transactionID }, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<BettingDataLC_AG> Get_BetList_AG(string transactionIDs)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BettingDataLC_AG>("Service_BettransLC_AGV2_Bet_Get", new { transactionIDs = transactionIDs }, commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_DG(string TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_DGV2_Set", new { TransId = TransId, SettleFlag = 1 }, commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_AG(string transactionID, string transactionIDs)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_AGV2_SettleFlag_Set", new { transactionID = transactionID, transactionIDs = transactionIDs }, commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_TpWagerAG(string billNoList)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerAG_Settle_Set", new { billNoList = billNoList }, commandType: CommandType.StoredProcedure);
        }
        public int Get_TpWagerAG_Settle(string billNoList, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            var p = new DynamicParameters(new { billNoList = billNoList });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            int result = conn.Execute("Service_TpWagerAG_Settle_Get", p, commandType: CommandType.StoredProcedure);

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
        #endregion
        #region Allbet
        public IEnumerable<UnsettleBettingDataLC_Allbet> GetUnsettledListLC_Allbet()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<UnsettleBettingDataLC_Allbet>("Service_BettransLC_Allbet_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int CancelBetLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Allbet_LiveCasino;

            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new { originalTranId = bettingDataLC_Allbet.OriginalTranId, client = bettingDataLC_Allbet.Client, details = JsonConvert.SerializeObject(bettingDataLC_Allbet.Details) });
            p.Add("hasCanceled", dbType: DbType.Int32, direction: ParameterDirection.Output);
            result = conn.Execute("Service_CancelBetLC_Allbet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (p.Get<int>("hasCanceled") == 0 && result == bettingDataLC_Allbet.Details.Count)
            {
                if (result > 0 && bettingDataLC_Allbet.Amount != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount, (int)Tp.Allbet, TransType.Refunds, $"Cancel: {Math.Abs(bettingDataLC_Allbet.Amount).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, bettingDataLC_Allbet.Amount, (int)Tp.Allbet, $"T{bettingDataLC_Allbet.OriginalTranId}", gameType, TpCategory.LiveCasino, bettingDataLC_Allbet.OriginalTranId.ToString(), TransType.Refunds, null, out afterAmount, "CancelBet", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            else if (p.Get<int>("hasCanceled") == 1)
            {
                result = -1;
            }
            else
            {
                result = -3;
            }

            return result;
        }
        public int PayoutLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Allbet_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { tranId = bettingDataLC_Allbet.TranId, transferType = bettingDataLC_Allbet.TransferType, client = bettingDataLC_Allbet.Client, currency = bettingDataLC_Allbet.Currency, amount = bettingDataLC_Allbet.Amount, details = JsonConvert.SerializeObject(bettingDataLC_Allbet.Details) });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransLC_Allbet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }

                if (result > 0)
                {
                    decimal Stake = p.Get<decimal>("Stake");
                    decimal payoutAmount = bettingDataLC_Allbet.Amount + Stake;
                    if (payoutAmount != 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount + Stake, (int)Tp.Allbet, TransType.Payout, $"Settle: {(bettingDataLC_Allbet.Amount + Stake).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, payoutAmount, (int)Tp.Allbet, bettingDataLC_Allbet.Details[0].BetNum.ToString(), gameType, TpCategory.LiveCasino, bettingDataLC_Allbet.Details[0].BetNum.ToString(), TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        ////BetRecordInfo betRecordInfo = new BetRecordInfo
                        ////{
                        ////    TpId = (int)Tp.Allbet,
                        ////    Stake = Stake,
                        ////    WinlostAmount = bettingDataLC_Allbet.Amount,
                        ////    TransId = bettingDataLC_Allbet.Details[0].BetNum.ToString(),
                        ////    GameType = GameType.Allbet_LiveCasino,
                        ////    TpUserName = bettingDataLC_Allbet.Client,
                        ////    SettledTime = 0,
                        ////    SettlementType = SettlementType.SettledForValidNotRollover
                        ////};

                        ////result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        conn.Execute("Service_BettransLC_Allbet_SettleFlag_Set", new { betNum = bettingDataLC_Allbet.Details[0].BetNum, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
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
        public int ReSettledLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Allbet_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { tranId = bettingDataLC_Allbet.TranId, transferType = bettingDataLC_Allbet.TransferType, client = bettingDataLC_Allbet.Client, currency = bettingDataLC_Allbet.Currency, amount = bettingDataLC_Allbet.Amount, details = JsonConvert.SerializeObject(bettingDataLC_Allbet.Details) });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransLC_Allbet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }

                if (result > 0)
                {
                    decimal Stake = p.Get<decimal>("Stake");
                    decimal? WinLostAmount = p.Get<decimal?>("WinLostAmount");

                    if (WinLostAmount == null || bettingDataLC_Allbet.Amount != WinLostAmount)
                    {
                        decimal backAmount = 0;
                        if (WinLostAmount == null)
                        {
                            backAmount = bettingDataLC_Allbet.Amount + Stake;
                        }
                        else
                        {
                            backAmount = bettingDataLC_Allbet.Amount - (WinLostAmount ?? 0);
                        }

                        if (backAmount != 0)
                        {
                            //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, (int)Tp.Allbet, TransType.Payout, $"ReSettle: {backAmount.ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
                            result = PayoutV2(tpCustInfo, backAmount, (int)Tp.Allbet, bettingDataLC_Allbet.Details[0].BetNum.ToString(), gameType, TpCategory.LiveCasino, bettingDataLC_Allbet.Details[0].BetNum.ToString(), TransType.Payout, null, out afterAmount, "Resettle", out bonusId, conn, trans);
                        }

                        if (result > 0)
                        {
                            ////BetRecordInfo betRecordInfo = new BetRecordInfo
                            ////{
                            ////    TpId = (int)Tp.Allbet,
                            ////    Stake = Stake,
                            ////    WinlostAmount = bettingDataLC_Allbet.Amount,
                            ////    TransId = bettingDataLC_Allbet.Details[0].BetNum.ToString(),
                            ////    GameType = GameType.Allbet_LiveCasino,
                            ////    TpUserName = bettingDataLC_Allbet.Client,
                            ////    SettledTime = 0,
                            ////    SettlementType = SettlementType.SettledForValidNotRollover
                            ////};

                            ////result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);
                            conn.Execute("Service_BettransLC_Allbet_SettleFlag_Set", new { betNum = bettingDataLC_Allbet.Details[0].BetNum, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
                            trans.Commit();
                        }
                    }
                    else
                    {
                        trans.Commit();
                        result = -2;
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
        public int SetSettleFlag_Allbet(long betNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_Allbet_SettleFlag_Set", new { betNum = betNum, SettleFlag = 1 }, commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerAllbet(long betNum)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerAllBet_Settle_Set", new { betNum = betNum }, commandType: CommandType.StoredProcedure);
        }
        public int Get_TpWagerAllbet_Settle(long betNum, out decimal stake, out decimal validBet, out decimal winLostAmount, out long settleTime)
        {
            stake = 0;
            validBet = 0;
            winLostAmount = 0;
            settleTime = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            var p = new DynamicParameters(new { betNum = betNum });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("ValidBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("SettleTime", dbType: DbType.Int64, direction: ParameterDirection.Output);
            int result = conn.Execute("Service_TpWagerAllBet_Settle_Get", p, commandType: CommandType.StoredProcedure);

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
        #endregion
        #region Sexy
        public int CancelBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            foreach (var txn in cancelBetTxn.txns)
            {
                DateTime.TryParse(txn.updateTime, out DateTime uT);
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                var p = new DynamicParameters(new { txId = txn.txId, userId = cancelBetTxn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = conn.Execute("Service_CancelBetLC_Sexy_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

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

            trans.Commit();
            return result;
        }
        public int SettledLC_Sexy(BettingDataLC_Sexy_Settle bettingDataLC_Sexy_Settle, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;
            bool haveNoBetting = false;
            using var connSettled = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            DateTime.TryParse(bettingDataLC_Sexy_Settle.RoundStartTime, out DateTime dtTransTime);
            long roundStartTime = tools.ConvertLocTime(dtTransTime, 8);

            connSettled.Execute("Service_BettransLC_Sexy_Settle_Ins", new { roundId = bettingDataLC_Sexy_Settle.RoundId, dealerDomain = bettingDataLC_Sexy_Settle.DealerDomain, roundStartTime = roundStartTime, validBetTime = bettingDataLC_Sexy_Settle.ValidBetTime / 1000, winner = bettingDataLC_Sexy_Settle.Winner, result = bettingDataLC_Sexy_Settle.Result }, commandType: CommandType.StoredProcedure);

            foreach (var txn in bettingDataLC_Sexy_Settle.Txns)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                DateTime.TryParse(txn.BetTime, out dtTransTime);
                long betTime = tools.ConvertLocTime(dtTransTime, 8);
                DateTime.TryParse(txn.UpdateTime, out dtTransTime);
                long updateTime = tools.ConvertLocTime(dtTransTime, 8);
                try
                {
                    result = conn.Execute("Service_BettransmLC_Sexy_Settle_Ins", new { txId = txn.TxId, roundId = bettingDataLC_Sexy_Settle.RoundId, userId = txn.UserId, category = txn.Category, gameType = txn.GameType, betAmount = txn.BetAmount, betTime = betTime, updateTime = updateTime, winLoss = txn.WinLoss, odds = txn.Odds, settleWinLoss = txn.SettleWinLoss, validBet = txn.ValidBet, status = txn.Status }, transaction: trans, commandType: CommandType.StoredProcedure);
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
                        betRecordInfo.SettledTime = betTime;
                        betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;
                        betRecordInfo.BonusId = bonusId;

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }
                else
                {
                    log2File.Error($"{JsonConvert.SerializeObject(txn)}", $"Sexy_haveNoBetting");/*need add check in SP*/
                    haveNoBetting = true;
                    trans.Rollback();
                }
            }

            if (haveNoBetting)
            {
                return 0;
            }
            else
            {
                connSettled.Execute("Service_BettransLC_Sexy_Settle_Set", new
                {
                    roundId = bettingDataLC_Sexy_Settle.RoundId
                }, commandType: CommandType.StoredProcedure);

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

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var p = new DynamicParameters(new { txId = txn.txId });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("winLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("settleWinLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("validBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = conn.Execute("Service_BettransmLC_Sexy_Unsettle_Del", p, transaction: trans, commandType: CommandType.StoredProcedure);

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

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            cantUnsettle = true;
                            trans.Rollback();
                        }
                    }
                    else
                    {
                        cantUnsettle = true;
                        trans.Rollback();
                    }
                }
                else
                {
                    trans.Rollback();
                }
            }

            if (cantUnsettle)
            {
                return -99;
            }
            else
            {
                using var connSettled = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                connSettled.Execute("Service_BettransLC_Sexy_Unsettle_Del", new
                {
                    roundId = unsettle.roundId
                }, commandType: CommandType.StoredProcedure);

                return 1;
            }
        }

        public int VoidRoundLC_Sexy(Unsettle_VoidRound voidRound, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            int result = 0;

            using var connSettled = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var checkUnsettled = connSettled.QueryFirstOrDefault<int>("Service_BettransLC_Sexy_CheckUnsettled_Get", new { roundId = voidRound.roundId }, commandType: CommandType.StoredProcedure) > 0;
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

                    using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                    conn.Open();
                    using var trans = conn.BeginTransaction();
                    var p = new DynamicParameters(new { txId = txn.txId, userId = txn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                    p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                    result = conn.Execute("Service_CancelBetLC_Sexy_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        decimal betAmount = p.Get<decimal>("betAmount");

                        if (betAmount != 0)
                        {
                            //result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, betAmount, (int)Tp.Sexy, TransType.Refunds, $"VoidRound: {betAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
                            result = PayoutV2(txn.tpCustInfo, betAmount, (int)Tp.Sexy, txn.txId, gameType, TpCategory.LiveCasino, txn.txId, TransType.Refunds, null, out afterAmount, "VoidRound", out bonusId, conn, trans, AllowNegative.Allow);
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

                            result = SettleTicket(txn.tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                            if (result > 0)
                            {
                                trans.Commit();
                            }
                        }
                    }
                    else
                    {
                        trans.Rollback();
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

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var p = new DynamicParameters(new { txId = txn.txId, userId = txn.userId, updateTime = updateTime.ToUnixTimeSeconds() });
                p.Add("betAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("winLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("settleWinLoss", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("validBet", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = conn.Execute("Service_BettransmLC_Sexy_VoidTxn_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

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

                        result = SettleTicket(txn.tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
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
                    trans.Rollback();
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
        #endregion

        #region EBet
        public int PayoutLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;

            int ticketStatus = ticketStatus = (int)TicketStatus.Win;

            if (bettingData_EBet.detail.payout == 0)
            {
                ticketStatus = (int)TicketStatus.Lost;
            }
            else if (bettingData_EBet.detail.payout == bettingData_EBet.detail.totalBet)
            {
                ticketStatus = (int)TicketStatus.Draw;
            }

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
                betTime = bettingData_EBet.detail.betTime,
                totalBet = bettingData_EBet.detail.totalBet,
                validBet = bettingData_EBet.detail.validBet,
                detail = JsonConvert.SerializeObject(bettingData_EBet.detail),
                betList = JsonConvert.SerializeObject(bettingData_EBet.detail.betList),
                payoutTime = bettingData_EBet.detail.payoutTime,
                payout = bettingData_EBet.detail.payout,
                srcResults = JsonConvert.SerializeObject(bettingData_EBet.detail.srcResults),
                results = JsonConvert.SerializeObject(bettingData_EBet.detail.results),
                TicketStatus = ticketStatus,
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransLC_EBet_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

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

        public int CancelBetLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;

            afterAmount = 0;
            int result = 0;
            string mixUniqueId = $"{bettingData_EBet.detail.roundCode}_{bettingData_EBet.username}";

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var refBettransLC = conn.Execute("Service_BettransLC_Ebet_CancelBet_Set", new
            {
                seqNo = bettingData_EBet.seqNo
            },
            transaction: trans,
            commandType: CommandType.StoredProcedure
            );
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
                    trans.Commit();
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        public BettingData_EBet CheckPayoutLC_EBet(string seqNo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BettingData_EBet>("Service_BettransLC_EBet_Payout_Get", new { seqNo = seqNo }, commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<CreditRecord_EBet> GetCreditRecordLC_EBet(string[] seqNo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var jsonSeqNo = JsonConvert.SerializeObject(seqNo);
            var bet = conn.Query<CreditRecord_EBet>("Service_BettransLC_EBet_Bets_Get", new { seqNos = jsonSeqNo }, commandType: CommandType.StoredProcedure);
            var payout = conn.Query<CreditRecord_EBet>("Service_BettransLC_EBet_Payouts_Get", new { seqNos = jsonSeqNo }, commandType: CommandType.StoredProcedure);
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

        public IEnumerable<SettleData_EBet> GetUnsettledListLC_EBet()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleData_EBet>("Service_BettransLC_EBet_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_EBet(string TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_EBet_Settle_Set", new { TransId = TransId, SettleFlag = 1 }, commandType: CommandType.StoredProcedure);
        }
        #endregion
    }
}
