using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class SettledService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;
        private readonly Tools tools;
        private readonly BettingService bettingService;

        public SettledService(IConfiguration configuration, WalletService walletService, Tools tools, BettingService bettingService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
            this.bettingService = bettingService;
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
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var p = new DynamicParameters(new { TransactionID = payoutDataLC_AG.TransactionID, BillNo = payoutDataLC_AG.BillNo, TpUserName = username, ValidBetAmount = payoutDataLC_AG.ValidBetAmount, NetAmount = payoutDataLC_AG.NetAmount, Settletime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, -4), OriginalState = payoutDataLC_AG.TransactionType, Memo = payoutDataLC_AG.Memo, TicketStatus = payoutDataLC_AG.TransactionType.Equals("Win", StringComparison.OrdinalIgnoreCase) ? (int)TicketStatus.Win : (int)TicketStatus.Lost });
            p.Add("OutValidBetAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("OutNetAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransLC_AG_Ins", p, commandType: CommandType.StoredProcedure);
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

            result = SettleTicket(tpCustInfo, betRecordInfo, backAmount, out afterAmount);/*Not necessarily Settled, Need the all stake of reference bill No is correct*/

            return result;
        }

        public int SettledRNG_GPI(TpCustInfo tpCustInfo, SettledDataRNG_GPI settledDataRNG_GPI, out decimal afterAmount, out string TransId)
        {
            afterAmount = 0;
            TransId = settledDataRNG_GPI.trx_id;

            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(settledDataRNG_GPI);
            p.Add("jackpot_contribution", 0);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRNG_GPI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                if (settledDataRNG_GPI.amount > 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, settledDataRNG_GPI.amount, (int)Tp.GPI, TransType.Settled, $"Payout: {Math.Abs(settledDataRNG_GPI.amount).ToString("#,##0.##")}", settledDataRNG_GPI.trx_id, out afterAmount, conn, trans);
                }

                if (result > 0)
                {
                    decimal Stake = p.Get<decimal>("Stake");
                    decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                    BetRecordInfo betRecordInfo = new BetRecordInfo();
                    betRecordInfo.TpId = (int)Tp.GPI;
                    betRecordInfo.Stake = Stake;
                    betRecordInfo.WinlostAmount = WinLostAmount;
                    betRecordInfo.TransId = settledDataRNG_GPI.round_id;
                    betRecordInfo.GameType = GameType.GPI_RNG;
                    betRecordInfo.TpUserName = settledDataRNG_GPI.cust_id;
                    betRecordInfo.SettledTime = 0;
                    betRecordInfo.SettlementType = SettlementType.SettledForValidNotRollover;

                    result = SettleTicket(tpCustInfo, betRecordInfo, settledDataRNG_GPI.amount, out _, conn, trans, false/*Don't need Payout*/);

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

            return result;
        }

        public int SettledFish_YL(TpCustInfo tpCustInfo, BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            long transTime = 0;
            DateTime.TryParse(bettingDataFish_YL.transTime, out DateTime dtTransTime);
            transTime = tools.ConvertLocTime(dtTransTime, 8);

            int result = conn.Execute("Service_BettransFish_YL_Ins", new { bettingDataFish_YL.txId, bettingDataFish_YL.userId, gameNumier = bettingDataFish_YL.gameNumber, bettingDataFish_YL.gameId, bettingDataFish_YL.chair, bettingDataFish_YL.roomId, bettingDataFish_YL.roomLevel, bettingDataFish_YL.currency, transTime, bettingDataFish_YL.betAmount, bettingDataFish_YL.payAmount, bettingDataFish_YL.commission, bettingDataFish_YL.profit, bettingDataFish_YL.validBet }, transaction: trans, commandType: CommandType.StoredProcedure);

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
                    result = conn.Execute("Service_BettransFish_YL_Set", new { refundDataFish_YL.txId, updateTime = dtUpdateTime }, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        BetRecordInfo betRecordInfo = new BetRecordInfo();
                        betRecordInfo.TpId = (int)Tp.YL;
                        betRecordInfo.Stake = bettingDataFish_YL.validBet;
                        betRecordInfo.WinlostAmount = 0;
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
                else if (betRecord.TicketStatus != TicketStatus.Refund)
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
            #region Set TurnOver of RollOver
            if (RefBetRecord == null)
            {
                if (betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                }
            }
            else
            {
                if (RefBetRecord.SettlementType != SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.UnSettled)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, $"UnSettled", RefBetRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType != SettlementType.SettledForValid)
                {
                    SetRollOverTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, $"Re-Settled", RefBetRecord.TransId, conn, trans);
                }
                else if (RefBetRecord.SettlementType == SettlementType.SettledForValid && betRecord.SettlementType == SettlementType.SettledForValid && RefBetRecord.Stake != betRecord.Stake)
                {
                    SetRollOverTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, "Re-Settled", betRecord.TransId, conn, trans);
                }
            }
            #endregion

            #region Set TurnOver of WithdrawalLimit
            if (RefBetRecord == null)
            {
                if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw)
                {
                    SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                }
            }
            else
            {
                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                    && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled))
                {
                    SetWithdrawalLimitTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
                }
                else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Draw)
                    && RefBetRecord.Stake != betRecord.Stake)
                {
                    SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, "Re-Settled", betRecord.TransId, conn, trans);
                }
            }
            #endregion

            #region Set WinLostAmount of Cust
            if (betRecord.WinlostAmount != 0)
            {
                decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
                decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;
                if (RefBetRecord == null)
                {
                    if (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin)
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                        AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                    }
                }
                else
                {
                    if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                    {
                        SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                        AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                    }
                    else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw || RefBetRecord.TicketStatus == TicketStatus.FreeBetWin)
                        && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.FreeBetWin))
                    {
                        decimal RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                        decimal RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                        if (RefWinnings != Winnings || RefLosses != Losses)
                        {
                            SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, conn, trans);
                            AutoCancelBonus(tpCustInfo.CustId, conn, trans);
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
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake, conn, trans);
                }
            }
            else
            {
                if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake, conn, trans);
                }
                else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win || RefBetRecord.TicketStatus == TicketStatus.Draw)
                    && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Draw))
                {
                    if (RefBetRecord.Stake != betRecord.Stake)
                    {
                        SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, conn, trans);
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
            return conn.Execute("Service_TpSettleBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AddTpBetRecord(BetRecordInfo betRecordInfo, TpCustInfo tpCustInfo, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_TpSettleBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, SettledTime = betRecordInfo.SettledTime, Stake = betRecordInfo.Stake, WinlostAmount = betRecordInfo.WinlostAmount, SettlementType = (int)betRecordInfo.SettlementType, TicketStatus = (int)betRecordInfo.TicketStatus }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetRollOverTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_RollOverTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWithdrawalLimitTurnOver(int TpId, int CustId, string UserName, int SiteId, decimal TurnOver, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_WithdrawalLimitTurnOver_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, TurnOver = TurnOver, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetWinLostAmount(int TpId, int CustId, string UserName, int SiteId, decimal Winnings, decimal Losses, string Memo, string RefTransId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Common_WinLostAmount_Set", new { TpId = TpId, CustId = CustId, UserName = UserName, SiteId = SiteId, Winnings = Winnings, Losses = Losses, Memo = Memo, RefTransId = RefTransId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int SetEffectiveBetSum(int CustId, int SiteId, decimal Stake, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_EffectiveBetSum_Set", new { CustId = CustId, SiteId = SiteId, Stake = Stake }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
        public int AutoCancelBonus(int CustId, MySqlConnection conn, MySqlTransaction trans)
        {
            return conn.Execute("Service_AutoCancelBonus_Set", new { CustId = CustId }, transaction: trans, commandType: CommandType.StoredProcedure);
        }
    }
}
