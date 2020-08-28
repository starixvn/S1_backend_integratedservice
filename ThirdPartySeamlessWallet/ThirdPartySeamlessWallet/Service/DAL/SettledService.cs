using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using System.Linq;
using System.Collections.Generic;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly BettingService bettingService;
        private readonly Log2File log2File;
        private readonly int[] hasBonusWallet;

        public SettledService(IConfiguration configuration, WalletService walletService, Tools tools, BettingService bettingService, Log2File log2File)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
            this.bettingService = bettingService;
            this.log2File = log2File;
            hasBonusWallet = configuration.GetSection("HasBonusWallet").Get<int[]>();
        }
        public int SettledLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
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

                if (p.Get<decimal?>("Stake") == null)
                {
                    result = 0;
                }

                if (result > 0)
                {
                    decimal Stake = p.Get<decimal>("Stake");

                    if (bettingDataLC_DG.Amount != 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_DG.Amount, (int)Tp.DG, TransType.Settled, $"Payout: {bettingDataLC_DG.Amount.ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                    }

                    if (result > 0)
                    {
                        decimal WinLostAmount = p.Get<decimal?>("WinLostAmount") ?? 0;

                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp.DG,
                            Stake = Stake,
                            WinlostAmount = WinLostAmount,
                            TransId = bettingDataLC_DG.TransId,
                            GameType = GameType.DG_LiveCasino,
                            TpUserName = bettingDataLC_DG.Username,
                            SettledTime = 0,
                            SettlementType = SettlementType.SettledForValidNotRollover
                        };

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        conn.Execute("Service_BettransLC_DGV2_Set", new { TransId = bettingDataLC_DG.TransId, SettleFlag = 1 }, transaction: trans, commandType: CommandType.StoredProcedure);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
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

        public int SettledLC_AG_Merger(PayoutDataLC_AG payoutDataLC_AG, string relTransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = conn.Execute("Service_TpSettleBet_AG_Set", new { TpId = (int)Tp.AG, TransId = payoutDataLC_AG.TransactionID, RelTransId = relTransId, GameType = (int)GameType.AG_LiveCasino, SettledTime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4) }, trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            return result;
        }
        public int SettledLC_AG(TpCustInfo tpCustInfo, PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);

            try
            {
                result = conn.Execute("Service_BettransLC_AGV2_Payout_Ins", new
                {
                    billNo = payoutDataLC_AG.BillNo,
                    transactionID = payoutDataLC_AG.TransactionID,
                    transactionIDs = payoutDataLC_AG.TransactionIDs.Count() > 1 ? JsonConvert.SerializeObject(payoutDataLC_AG.TransactionIDs) : null,
                    currency = payoutDataLC_AG.Currency,
                    netAmount = payoutDataLC_AG.NetAmount,
                    validBetAmount = payoutDataLC_AG.ValidBetAmount,
                    playname = payoutDataLC_AG.Playname,
                    settletime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4),
                    gametype = payoutDataLC_AG.Gametype,
                    gameCode = payoutDataLC_AG.GameCode,
                    transactionType = payoutDataLC_AG.TransactionType,
                    transactionCode = payoutDataLC_AG.TransactionCode,
                    gameResult = payoutDataLC_AG.GameResult,
                    TicketStatus = payoutDataLC_AG.TransactionType.Equals("LOSE", StringComparison.OrdinalIgnoreCase) ? (int)TicketStatus.Lost : (int)TicketStatus.Win
                }, transaction: trans, commandType: CommandType.StoredProcedure);

                decimal payoutAmount = payoutDataLC_AG.ValidBetAmount + payoutDataLC_AG.NetAmount;

                if (result > 0 && payoutAmount > 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, payoutAmount, (int)Tp.AG, TransType.Settled, $"Payout: {payoutAmount.ToString("#,##0.##")}", payoutDataLC_AG.BillNo, out afterAmount, conn, trans);
                }

                var p = new DynamicParameters(new { TransactionID = payoutDataLC_AG.TransactionID, BillNo = payoutDataLC_AG.BillNo, TpUserName = username, ValidBetAmount = payoutDataLC_AG.ValidBetAmount, NetAmount = payoutDataLC_AG.NetAmount, Settletime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4), OriginalState = payoutDataLC_AG.TransactionType, Memo = payoutDataLC_AG.Memo, TicketStatus = payoutDataLC_AG.TransactionType.Equals("Win", StringComparison.OrdinalIgnoreCase) ? (int)TicketStatus.Win : (int)TicketStatus.Lost });
                p.Add("OutValidBetAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("OutNetAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                decimal OutValidBetAmount = p.Get<decimal>("OutValidBetAmount");
                decimal OutNetAmount = p.Get<decimal>("OutNetAmount");

                decimal backAmount = OutValidBetAmount + OutNetAmount;

                BetRecordInfo betRecordInfo = new BetRecordInfo();
                betRecordInfo.TpId = (int)Tp.AG;
                betRecordInfo.Stake = OutValidBetAmount;
                betRecordInfo.WinlostAmount = backAmount;
                betRecordInfo.TransId = payoutDataLC_AG.TransactionID.ToString();
                betRecordInfo.GameType = GameType.AG_LiveCasino;
                betRecordInfo.TpUserName = username;
                betRecordInfo.SettledTime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4);
                betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;

                if (backAmount == 0)
                {
                    betRecordInfo.TicketStatus = TicketStatus.UnSettled;
                }

                result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);
                //result = SettleTicket(tpCustInfo, betRecordInfo, backAmount, out afterAmount, conn, trans);/*Not necessarily Settled, Need the all stake of reference bill No is correct*/
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

        public int SettledFish_YL(TpCustInfo tpCustInfo, BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            long transTime = 0;
            DateTime.TryParse(bettingDataFish_YL.transTime, out DateTime dtTransTime);
            transTime = tools.ConvertLocTime(dtTransTime, 8);
            try
            {
                result = conn.Execute("Service_BettransFish_YL_Ins", new { bettingDataFish_YL.txId, bettingDataFish_YL.userId, gameNumier = bettingDataFish_YL.gameNumber, bettingDataFish_YL.gameId, bettingDataFish_YL.chair, bettingDataFish_YL.roomId, bettingDataFish_YL.roomLevel, bettingDataFish_YL.currency, transTime, bettingDataFish_YL.betAmount, bettingDataFish_YL.payAmount, bettingDataFish_YL.commission, bettingDataFish_YL.profit, bettingDataFish_YL.validBet }, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataFish_YL.profit != 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataFish_YL.profit, (int)Tp.YL, TransType.Settled, $"Settled: {Math.Abs(bettingDataFish_YL.profit).ToString("#,##0.##")}", bettingDataFish_YL.txId, out afterAmount, conn, trans);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.YL;
                        betRecordInfo.Stake = bettingDataFish_YL.validBet;
                        betRecordInfo.WinlostAmount = bettingDataFish_YL.profit;
                        betRecordInfo.TransId = bettingDataFish_YL.txId;
                        betRecordInfo.GameType = GameType.YL_Fish;
                        betRecordInfo.TpUserName = bettingDataFish_YL.userId;
                        betRecordInfo.SettledTime = 0;
                        betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }
                else
                {
                    result = -1;
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
        public int RefundFish_YL(TpCustInfo tpCustInfo, RefundDataFish_YL refundDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = 0;
            long updateTime = 0;
            DateTime.TryParse(refundDataFish_YL.updateTime, out DateTime dtUpdateTime);
            updateTime = tools.ConvertLocTime(dtUpdateTime, 8);
            BettingDataFish_YL bettingDataFish_YL = conn.QueryFirstOrDefault<BettingDataFish_YL>("Service_BettransFish_YL_Get", new { refundDataFish_YL.txId }, transaction: trans, commandType: CommandType.StoredProcedure);

            if (bettingDataFish_YL != null && bettingDataFish_YL.TicketStatus != TicketStatus.Refund)
            {
                if (bettingDataFish_YL.profit != 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataFish_YL.profit * -1, (int)Tp.YL, TransType.Refunds, $"Void: {bettingDataFish_YL.profit.ToString("#,##0.##")}", bettingDataFish_YL.txId, out afterAmount, conn, trans);
                }

                if (result > 0)
                {
                    result = conn.Execute("Service_BettransFish_YL_Set", new { refundDataFish_YL.txId, updateTime = updateTime }, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.YL;
                        betRecordInfo.Stake = bettingDataFish_YL.validBet;
                        betRecordInfo.WinlostAmount = bettingDataFish_YL.profit;
                        betRecordInfo.TransId = bettingDataFish_YL.txId;
                        betRecordInfo.GameType = GameType.YL_Fish;
                        betRecordInfo.TpUserName = bettingDataFish_YL.userId;
                        betRecordInfo.SettledTime = updateTime;
                        betRecordInfo.SettlementType = SettlementType.SettledForInvalid;
                        betRecordInfo.TicketStatus = TicketStatus.Refund;

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }

                if (result > 0 && bettingDataFish_YL.profit == 0)
                {
                    result = -1;
                }
            }
            else
            {
                result = -1;
            }

            return result;
        }
        public IEnumerable<BettingDataRNG_PP> GetUnsettledListRNG_PP()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<BettingDataRNG_PP>("Service_UnsettledListRng_PP_Get", commandType: CommandType.StoredProcedure);
        }
        public int SettledLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
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

                    if ((bettingDataLC_Allbet.Amount + Stake) != 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount + Stake, (int)Tp.Allbet, TransType.Settled, $"Settle: {(bettingDataLC_Allbet.Amount + Stake).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
                    }

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp.Allbet,
                            Stake = Stake,
                            WinlostAmount = bettingDataLC_Allbet.Amount,
                            TransId = bettingDataLC_Allbet.Details[0].BetNum.ToString(),
                            GameType = GameType.Allbet_LiveCasino,
                            TpUserName = bettingDataLC_Allbet.Client,
                            SettledTime = 0,
                            SettlementType = SettlementType.SettledForValidNotRollover
                        };

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
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
                            result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, (int)Tp.Allbet, TransType.Settled, $"ReSettle: {backAmount.ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
                        }

                        if (result > 0)
                        {
                            BetRecordInfo betRecordInfo = new BetRecordInfo
                            {
                                TpId = (int)Tp.Allbet,
                                Stake = Stake,
                                WinlostAmount = bettingDataLC_Allbet.Amount,
                                TransId = bettingDataLC_Allbet.Details[0].BetNum.ToString(),
                                GameType = GameType.Allbet_LiveCasino,
                                TpUserName = bettingDataLC_Allbet.Client,
                                SettledTime = 0,
                                SettlementType = SettlementType.SettledForValidNotRollover
                            };

                            result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                            if (result > 0)
                            {
                                trans.Commit();
                            }
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
        public int SettledLC_Sexy(BettingDataLC_Sexy_Settle bettingDataLC_Sexy_Settle, out decimal afterAmount)
        {
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
                        result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, txn.SettleWinLoss, (int)Tp.Sexy, TransType.Settled, $"Payout: {Math.Abs(txn.SettleWinLoss).ToString("#,##0.##")}", txn.TxId, out afterAmount, conn, trans, AllowNegative.Allow);
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
                        result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, settleWinLoss * -1, (int)Tp.Sexy, TransType.Settled, $"Unsettle: {(settleWinLoss * -1).ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
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
                            result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, betAmount, (int)Tp.Sexy, TransType.Refunds, $"VoidRound: {betAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
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
                        result = walletService.SetBalance(txn.tpCustInfo.CustId, txn.tpCustInfo.SiteId, txn.tpCustInfo.UserName, fixAmount, (int)Tp.Sexy, TransType.Refunds, $"VoidTxn: {fixAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans, AllowNegative.Allow);
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
        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, decimal backAmount, out decimal afterAmount)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = SettleTicket(tpCustInfo, betRecord, backAmount, out afterAmount, conn, trans);
            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, decimal backAmount, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans, bool doPayout = true)
        {
            int result = 0;
            afterAmount = 0;
            var isBonusWallet = configuration.GetValue<bool>("BonusWallet");

            var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);
            #region Payout Process
            Enum.TryParse(betRecord.TpId.ToString(), out Tp tp);

            if (doPayout && RefBetRecord == null)
            {
                return -1;
            }
            else if (doPayout && RefBetRecord.TicketStatus != TicketStatus.UnSettled)
            {
                return -1;
            }
            else if (tp == Tp.AG && RefBetRecord.Stake != betRecord.Stake)
            {
                return -1;
            }
            else
            {
                if (betRecord.Stake == 0 && RefBetRecord != null)
                {
                    betRecord.Stake = RefBetRecord.Stake;
                }

                if (betRecord.TicketStatus == TicketStatus.Lost && doPayout && backAmount == 0)
                {
                    betRecord.WinlostAmount = -betRecord.Stake;
                }
                else if (betRecord.TicketStatus != TicketStatus.Refund && betRecord.SettlementType != SettlementType.UnSettled)
                {
                    if (doPayout)
                    {
                        betRecord.WinlostAmount = backAmount - betRecord.Stake;
                    }

                    if (betRecord.WinlostAmount > 0)
                    {
                        if (betRecord.Stake != 0)
                        {
                            betRecord.TicketStatus = TicketStatus.Win;
                        }
                        else
                        {
                            betRecord.TicketStatus = TicketStatus.FreeBetWin;
                        }
                    }
                    else if (betRecord.WinlostAmount == 0)
                    {
                        betRecord.TicketStatus = TicketStatus.Draw;
                    }
                    else
                    {
                        betRecord.TicketStatus = TicketStatus.Lost;
                    }
                }

                if (backAmount > 0 && doPayout)
                {
                    if (betRecord.TicketStatus == TicketStatus.Refund)
                    {
                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Refunds, $"Refund: {backAmount}", betRecord.TransId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        string sMemo = $"Payout: {backAmount.ToString("#,##0.##")}";
                        if (betRecord.TicketStatus == TicketStatus.FreeBetWin)
                        {
                            sMemo = $"Free Bet Payout: {backAmount.ToString("#,##0.##")}";
                        }

                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Settled, sMemo, betRecord.TransId, out afterAmount, conn, trans);
                    }
                }
            }

            #endregion
            decimal validBet = betRecord.ValidBet ?? betRecord.Stake;
            bool hasBonusRunning = false;

            if (RefBetRecord != null && RefBetRecord.BonusId > 0)
            {
                betRecord.BonusId = RefBetRecord.BonusId;
            }

            int bonusId = betRecord.BonusId;
            #region Set TurnOver of RollOver
            if (RefBetRecord == null)
            {
                if (betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans) > 0;
                }
            }
            else
            {
                decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", RefBetRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"UnSettled", betRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, "Re-Settled", RefBetRecord.TransId, conn, trans) > 0;
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && refValidBet != validBet)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    hasBonusRunning = SetRollOverTurnOver(ref bonusId, betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans) > 0;
                }
            }
            #endregion

            #region Set TurnOver of WithdrawalLimit
            if (!hasBonusRunning)
            {
                if (RefBetRecord == null)
                {
                    if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                }
                else
                {
                    decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                    if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet, "Settled", betRecord.TransId, conn, trans);
                    }
                    else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                        && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled))
                    {
                        SetWithdrawalLimitTurnOver(RefBetRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refValidBet * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
                    }
                    else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Draw)
                        && refValidBet != validBet)
                    {
                        SetWithdrawalLimitTurnOver(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, validBet - refValidBet, "Re-Settled", betRecord.TransId, conn, trans);
                    }
                }
            }
            #endregion

            #region Set WinLostAmount of Cust
            decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
            decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;
            if (RefBetRecord == null && betRecord.WinlostAmount != 0)
            {
                if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin)
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        AutoCancelBonus(tpCustInfo.CustId, conn, trans);

                        if (hasBonusRunning)
                            AutoCancelBonusV2(bonusId, conn, trans);
                    }
                }
            }
            else if (RefBetRecord != null)
            {
                decimal refWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                decimal refLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", bonusId, betRecord.TransId, conn, trans);

                    if (Losses != 0)
                    {
                        AutoCancelBonus(tpCustInfo.CustId, conn, trans);

                        if (hasBonusRunning)
                            AutoCancelBonusV2(bonusId, conn, trans);
                    }
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", betRecord.TransId, conn, trans);

                    if (hasBonusRunning)
                        SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, refWinnings * -1, refLosses * -1, "Settled To Refund", bonusId, betRecord.TransId, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                {
                    if (refWinnings != Winnings || refLosses != Losses)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", betRecord.TransId, conn, trans);

                        if (hasBonusRunning)
                            SetBonusWinLost(betRecord.TpId, (int)betRecord.GameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - refWinnings, Losses - refLosses, "Settled", bonusId, betRecord.TransId, conn, trans);

                        if (Losses != 0)
                        {
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                            if (hasBonusRunning)
                                AutoCancelBonusV2(bonusId, conn, trans);
                        }
                    }
                }
            }
            #endregion

            #region Set EffectiveBet of Cust
            if (RefBetRecord == null)
            {
                if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet, conn, trans);
                }
            }
            else
            {
                decimal refValidBet = RefBetRecord.ValidBet ?? RefBetRecord.Stake;

                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                && (betRecord.TicketStatus == TicketStatus.UnSettled || betRecord.TicketStatus == TicketStatus.Refund))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, refValidBet * -1, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    if (refValidBet != validBet)
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, validBet - refValidBet, conn, trans);
                    }
                }
            }
            #endregion

            if (RefBetRecord == null)
            {
                result = AddTpBetRecord(betRecord, tpCustInfo, conn, trans);
            }
            else
            {
                result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
            }

            return result;
        }
        public BetRecordInfo GetTpBetRecord(int TpId, string TransId, int GameType, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.QueryFirstOrDefault<BetRecordInfo>("Service_TpBetRecord_Get", new { TpId = TpId, TransId = TransId, GameType = GameType }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, ValidBet = betRecordInfo.ValidBet, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, ValidBet = betRecordInfo.ValidBet, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;
            return conn.Execute("Common_RollOverTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int gameType, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (TurnOver == 0)
                return 0;
            return conn.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, GameType = gameType, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;
            return conn.Execute("Common_WinLostAmount_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal validBet, MySqlConnection conn, MySqlTransaction trans)
        {
            if (validBet == 0)
                return 0;
            return conn.Execute("Service_EffectiveBetSum_Set", new { CustId = CustId, SiteId = SiteId, Stake = validBet }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelBonus(int CustId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelBonus_Set", new { CustId = CustId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelBonusV2(int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            return 1;
            return conn.Execute("Service_AutoCancelBonusV2_Set", new { BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(ref int BonusId, int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Turnover, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Turnover == 0)
                return 0;

            var p = new DynamicParameters(new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Turnover = Turnover, Memo, RefTransId });
            p.Add("BonusId", value: BonusId, dbType: DbType.Int32, direction: ParameterDirection.InputOutput);
            var result = conn.Execute("Service_RolloverTurnover_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            BonusId = p.Get<int>("BonusId");
            return BonusId > 0 ? 1 : 0;
        }
        public int SetBonusWinLost(int TpId, int GameType, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, int BonusId, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            if (Winnings == 0 && Losses == 0)
                return 0;

            return conn.Execute("Service_BonusWinLost_Set", new { TpId = TpId, GameType = GameType, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, BonusId = BonusId, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public IEnumerable<dynamic> GetTpBetRecordUnsettle(int tpId, int gameType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<dynamic>("Service_TpBetRecordUnsettle_Get", new { TpId = tpId, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }
    }
}
