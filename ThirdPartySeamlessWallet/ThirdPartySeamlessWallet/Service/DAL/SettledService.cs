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

        public SettledService(IConfiguration configuration, WalletService walletService, Tools tools)
        {
            this.configuration = configuration;
            this.walletService = walletService;
            this.tools = tools;
        }
        public int SettledLC_AG(TpCustInfo tpCustInfo, PayoutDataLC_AG payoutDataLC_AG, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            string username = payoutDataLC_AG.Playname.Substring(payoutDataLC_AG.AgentCode.Length);
            var p = new DynamicParameters(new { TransactionID = payoutDataLC_AG.TransactionID, BillNo = payoutDataLC_AG.BillNo, TpUserName = username, ValidBetAmount = payoutDataLC_AG.ValidBetAmount, NetAmount = payoutDataLC_AG.NetAmount, Settletime = tools.ConvertLocTime(payoutDataLC_AG.Settletime, 8), OriginalState = payoutDataLC_AG.TicketStatus, Memo = (string)null, TicketStatus = payoutDataLC_AG.TicketStatus.Equals("Win", StringComparison.OrdinalIgnoreCase) ? (int)TicketStatus.Win : (int)TicketStatus.Lost });
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
            if (backAmount == 0)
            {
                betRecordInfo.TicketStatus = TicketStatus.UnSettled;
            }

            result = SettleTicket(tpCustInfo, betRecordInfo, backAmount, out afterAmount);

            return result;
        }
        public int SettleTicket(TpCustInfo tpCustInfo, BetRecordInfo betRecord, decimal backAmount, out decimal afterAmount)
        {
            int result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var RefBetRecord = GetTpBetRecord(betRecord.TpId, betRecord.TransId, (int)betRecord.GameType, conn, trans);
            Tp tp = Tp.None;
            Enum.TryParse(betRecord.TpId.ToString(), out tp);
            if (RefBetRecord == null)
            {
                return -1;
            }
            else if (RefBetRecord.TicketStatus != TicketStatus.UnSettled)
            {
                return -1;
            }
            else if (tp == Tp.AG && RefBetRecord.Stake != betRecord.Stake)
            {
                return -2;
            }
            else
            {
                if (betRecord.Stake == 0)
                {
                    betRecord.Stake = RefBetRecord.Stake;
                }

                if (betRecord.TicketStatus == TicketStatus.Lost && backAmount == 0)
                {
                    betRecord.WinlostAmount = -betRecord.Stake;
                }
                else if (betRecord.TicketStatus != TicketStatus.Refund)
                {
                    betRecord.WinlostAmount = backAmount - betRecord.Stake;
                    if (betRecord.WinlostAmount > 0)
                    {
                        betRecord.TicketStatus = TicketStatus.Win;
                    }
                    else
                    {
                        betRecord.TicketStatus = TicketStatus.Lost;
                    }
                }

                if (backAmount > 0)
                {
                    if (betRecord.TicketStatus == TicketStatus.Refund)
                    {
                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Refunds, $"Refund: {backAmount}", betRecord.TransId, out afterAmount, conn, trans);
                    }
                    else
                    {
                        walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, backAmount, betRecord.TpId, TransType.Settled, $"Settled: {backAmount}", betRecord.TransId, out afterAmount, conn, trans);
                    }
                }

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

            if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
            {
                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake, "Settled", betRecord.TransId, conn, trans);
            }
            else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                && (betRecord.TicketStatus == TicketStatus.Refund || betRecord.TicketStatus == TicketStatus.UnSettled))
            {
                SetWithdrawalLimitTurnOver(RefBetRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, RefBetRecord.Stake * -1, betRecord.TicketStatus == TicketStatus.UnSettled ? "UnSettled" : "Re-Settled", RefBetRecord.TransId, conn, trans);
            }
            else if ((betRecord.TicketStatus == TicketStatus.Win || betRecord.TicketStatus == TicketStatus.Lost)
                && RefBetRecord.Stake != betRecord.Stake)
            {
                SetWithdrawalLimitTurnOver(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, "Re-Settled", betRecord.TransId, conn, trans);
            }

            decimal Winnings = betRecord.WinlostAmount > 0 ? betRecord.WinlostAmount : 0;
            decimal Losses = betRecord.WinlostAmount < 0 ? betRecord.WinlostAmount : 0;

            if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
            {
                SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings, Losses, "Settled", betRecord.TransId, conn, trans);
                AutoCancelBonus(tpCustInfo.CustId, conn, trans);
            }
            else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
            {
                decimal RefWinnings = RefBetRecord.WinlostAmount > 0 ? RefBetRecord.WinlostAmount : 0;
                decimal RefLosses = RefBetRecord.WinlostAmount < 0 ? RefBetRecord.WinlostAmount : 0;

                if (RefWinnings != Winnings || RefLosses != Losses)
                {
                    SetWinLostAmount(betRecord.TpId, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, Winnings - RefWinnings, Losses - RefLosses, "Settled", betRecord.TransId, conn, trans);
                    AutoCancelBonus(tpCustInfo.CustId, conn, trans);
                }
            }

            if ((RefBetRecord.TicketStatus == TicketStatus.UnSettled || RefBetRecord.TicketStatus == TicketStatus.Refund)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
            {
                SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake, conn, trans);
            }
            else if ((RefBetRecord.TicketStatus == TicketStatus.Lost || RefBetRecord.TicketStatus == TicketStatus.Win)
                && (betRecord.TicketStatus == TicketStatus.Lost || betRecord.TicketStatus == TicketStatus.Win))
            {
                if (RefBetRecord.Stake != betRecord.Stake)
                {
                    SetEffectiveBetSum(tpCustInfo.CustId, tpCustInfo.SiteId, betRecord.Stake - RefBetRecord.Stake, conn, trans);
                }
            }

            result = SetTpBetRecord(betRecord, tpCustInfo, conn, trans);
            if (result > 0)
            {
                trans.Commit();
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
