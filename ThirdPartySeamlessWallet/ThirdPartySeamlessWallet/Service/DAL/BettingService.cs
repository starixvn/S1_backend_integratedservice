using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class BettingService
    {
        private readonly IConfiguration configuration;
        private readonly WalletService walletService;

        public BettingService(IConfiguration configuration, WalletService walletService)
        {
            this.configuration = configuration;
            this.walletService = walletService;
        }

        /*Single Bet for Settled*/
        public int PlaceBet(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = PlaceBet(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PlaceBet(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;

            var result = 0;
            if (betRecordInfo.Stake != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betRecordInfo.Stake * -1, betRecordInfo.TpId, TransType.Wagers, $"Stake: {betRecordInfo.Stake}", betRecordInfo.TransId, out afterAmount, conn, trans);
            }
            else
            {
                /*Free Bet*/
                result = 1;
            }

            if (result > 0)
            {
                result = conn.Execute("Service_TpInsertBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, Stake = betRecordInfo.Stake }, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            return result;
        }

        /*Will Merge Bet for Settled, Most for Live Casino*/
        private int PlaceBetByCall(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            var result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betRecordInfo.Stake * -1, betRecordInfo.TpId, TransType.Wagers, $"Stake: {betRecordInfo.Stake}", betRecordInfo.TransId, out afterAmount, conn, trans);

            if (result > 0)
            {
                result = conn.Execute("Service_TpPlaceBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, Stake = betRecordInfo.Stake }, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            return result;
        }
        private int CancelBetByCall(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            var result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betRecordInfo.Stake, betRecordInfo.TpId, TransType.Refunds, $"Refund: {betRecordInfo.Stake}", betRecordInfo.TransId, out afterAmount, conn, trans);

            if (result > 0)
            {
                result = conn.Execute("Service_TpPlaceBet_Set", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, Stake = betRecordInfo.Stake * -1 }, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            return result;
        }

        public int PlaceBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_SA_Ins", new { Txnid = bettingDataLC_SA.txnid, TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", Currency = bettingDataLC_SA.currency, Username = bettingDataLC_SA.username, Gametype = bettingDataLC_SA.gametype, Amount = bettingDataLC_SA.amount, Betdetails = bettingDataLC_SA.Betdetails, Gameid = bettingDataLC_SA.gameid, Memo = (string)null }, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                BetRecordInfo betRecordInfo = new BetRecordInfo();
                betRecordInfo.TpId = (int)Tp.SA;
                betRecordInfo.Stake = bettingDataLC_SA.amount;
                betRecordInfo.TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}";
                betRecordInfo.GameType = GameType.SA_LiveCasino;
                betRecordInfo.TpUserName = bettingDataLC_SA.username;

                result = PlaceBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int CancelBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var refBettransLC = conn.QueryFirstOrDefault<BettingDataLC_SA>("Service_BettransLC_SA_Get", new { Txnid = bettingDataLC_SA.txn_reverse_id }, transaction: trans, commandType: CommandType.StoredProcedure);
            if (refBettransLC == null || refBettransLC.TicketStatus == TicketStatus.Refund)
            {
                return -1;
            }
            else if (refBettransLC.TicketStatus == TicketStatus.UnSettled)
            {
                BetRecordInfo betRecordInfo = new BetRecordInfo();
                betRecordInfo.TpId = (int)Tp.SA;
                betRecordInfo.Stake = bettingDataLC_SA.amount;
                betRecordInfo.TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}";
                betRecordInfo.GameType = GameType.SA_LiveCasino;
                betRecordInfo.TpUserName = bettingDataLC_SA.username;

                result = CancelBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);
                result = conn.Execute("Service_BettransLC_SA_Set", new { Txnid = refBettransLC.txnid, Memo = "Cancel Bet", TicketStatus = (int)TicketStatus.Refund }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (result > 0)
                {
                    trans.Commit();
                }
            }

            return result;
        }

        public int PlaceBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_DG_Ins", new { data = bettingDataLC_DG.data, TransId = bettingDataLC_DG.TransId, Username = bettingDataLC_DG.Username, Amount = bettingDataLC_DG.Amount, Memo = (string)null }, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                BetRecordInfo betRecordInfo = new BetRecordInfo();
                betRecordInfo.TpId = (int)Tp.DG;
                betRecordInfo.Stake = bettingDataLC_DG.Amount;
                betRecordInfo.TransId = bettingDataLC_DG.TransId;
                betRecordInfo.GameType = GameType.DG_LiveCasino;
                betRecordInfo.TpUserName = bettingDataLC_DG.Username;

                result = PlaceBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int CancelBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var refBettransLC = conn.QueryFirstOrDefault<BettingDataLC_DG>("Service_BettransLC_DG_Get", new { data = bettingDataLC_DG.data }, transaction: trans, commandType: CommandType.StoredProcedure);
            if (refBettransLC == null || refBettransLC.TicketStatus == TicketStatus.Refund)
            {
                return -1;
            }
            else if (refBettransLC.TicketStatus == TicketStatus.UnSettled)
            {
                BetRecordInfo betRecordInfo = new BetRecordInfo();
                betRecordInfo.TpId = (int)Tp.DG;
                betRecordInfo.Stake = bettingDataLC_DG.Amount;
                betRecordInfo.TransId = bettingDataLC_DG.TransId;
                betRecordInfo.GameType = GameType.SA_LiveCasino;
                betRecordInfo.TpUserName = bettingDataLC_DG.Username;

                result = CancelBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);
                result = conn.Execute("Service_BettransLC_DG_Set", new { data = bettingDataLC_DG.data, Memo = "Cancel Bet", TicketStatus = (int)TicketStatus.Refund }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (result > 0)
                {
                    trans.Commit();
                }
            }

            return result;
        }
        public int PlaceBetRNG_GPI(TpCustInfo tpCustInfo, BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string Transid)
        {
            afterAmount = 0;
            Transid = bettingDataRNG_GPI.trx_id;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_GPI.amount *= -1;
            var p = new DynamicParameters(bettingDataRNG_GPI);
            p.Add("jackpot_win", 0);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRNG_GPI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_GPI.amount, (int)Tp.GPI, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_GPI.amount).ToString("#,##0.##")}", bettingDataRNG_GPI.trx_id, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
    }
}
