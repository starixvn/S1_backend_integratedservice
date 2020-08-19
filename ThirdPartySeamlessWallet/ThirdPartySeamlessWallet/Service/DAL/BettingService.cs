using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
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
            else if (result == -1)
            {
                trans.Rollback();
            }

            return result;
        }
        public int PlaceBet(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo, out decimal afterAmount, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;

            int result;

            try
            {
                result = conn.Execute("Service_TpInsertBet_Ins", new { TpId = betRecordInfo.TpId, TransId = betRecordInfo.TransId, GameType = (int)betRecordInfo.GameType, CustId = tpCustInfo.CustId, UserName = tpCustInfo.UserName, CurrencyId = tpCustInfo.CurrencyId, SiteId = tpCustInfo.SiteId, TpUserName = betRecordInfo.TpUserName, Stake = betRecordInfo.Stake }, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    return -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            if (result > 0 && betRecordInfo.Stake != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betRecordInfo.Stake * -1, betRecordInfo.TpId, TransType.Wagers, $"Stake: {betRecordInfo.Stake.ToString("#,##0.##")}", betRecordInfo.TransId, out afterAmount, conn, trans);
            }
            else
            {
                /*Free Bet*/
                result = 1;
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
                betRecordInfo.GameType = GameType.DG_LiveCasino;
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
        public int PlaceBetLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
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

                if (result > 0 && bettingDataLC_Allbet.Amount != 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount * -1, (int)Tp.Allbet, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_Allbet.Amount).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
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
        public int CancelBetLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
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
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount, (int)Tp.Allbet, TransType.Refunds, $"Cancel: {Math.Abs(bettingDataLC_Allbet.Amount).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);
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
        public int PlaceBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_Bet bettingDataLC_Sexy_Bet, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            foreach (var txn in bettingDataLC_Sexy_Bet.Txns)
            {
                DateTime.TryParse(txn.BetTime, out DateTime bBet);
                DateTime.TryParse(txn.UpdateTime, out DateTime uT);
                DateTimeOffset betTime = new DateTimeOffset(bBet, new TimeSpan(8, 0, 0));
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                var p = new DynamicParameters(new { txId = txn.TxId, userId = bettingDataLC_Sexy_Bet.UserId, IP = bettingDataLC_Sexy_Bet.Ip, currency = bettingDataLC_Sexy_Bet.Currency, roundId = bettingDataLC_Sexy_Bet.RoundId, gameType = bettingDataLC_Sexy_Bet.RoundId, betAmount = txn.BetAmount, betTime = betTime.ToUnixTimeSeconds(), updateTime = updateTime.ToUnixTimeSeconds(), category = txn.Category });
                result = conn.Execute("Service_BettransLC_Sexy_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                if (result > 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, txn.BetAmount * -1, (int)Tp.Sexy, TransType.Wagers, $"Stake: {Math.Abs(txn.BetAmount).ToString("#,##0.##")}", txn.TxId, out afterAmount, conn, trans);
                    if (result == 0)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
                }
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        public int CancelBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_CancelBet cancelBetTxn, out decimal afterAmount)
        {
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
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, betAmount, (int)Tp.Sexy, TransType.Refunds, $"Cancel Bet: {betAmount.ToString("#,##0.##")}", txn.txId, out afterAmount, conn, trans);
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
    }
}
