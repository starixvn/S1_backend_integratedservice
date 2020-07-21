using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
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
            p.Add("type", (int)BettransType.Bet);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRNG_GPI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0 && bettingDataRNG_GPI.amount != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_GPI.amount, (int)Tp.GPI, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_GPI.amount).ToString("#,##0.##")}", bettingDataRNG_GPI.trx_id, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        public int PlaceBetRNG_Joker(TpCustInfo tpCustInfo, BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_Joker.amount *= -1;
            bettingDataRNG_Joker.type = "bet";
            bettingDataRNG_Joker.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(bettingDataRNG_Joker);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRng_Joker_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0 && bettingDataRNG_Joker.amount != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_Joker.amount, (int)Tp.Joker, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_Joker.amount).ToString("#,##0.##")}", bettingDataRNG_Joker.id, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PlaceBetRNG_YGG(TpCustInfo tpCustInfo, BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_YGG.amount *= -1;
            bettingDataRNG_YGG.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(bettingDataRNG_YGG);
            p.Add("tickets", (int?)null);
            p.Add("bonusprize", (decimal?)null);
            p.Add("type", (int)BettransType.Bet);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = conn.Execute("Service_BettransRng_YGG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataRNG_YGG.amount < 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_YGG.amount, (int)Tp.YGG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_YGG.amount).ToString("#,##0.##")}", bettingDataRNG_YGG.subreference, out afterAmount, conn, trans);
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
        public int PlaceBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_SG.amount *= -1;
            bettingDataRNG_SG.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, acctId = bettingDataRNG_SG.acctId, currency = bettingDataRNG_SG.currency, amount = bettingDataRNG_SG.amount, type = bettingDataRNG_SG.type, channel = bettingDataRNG_SG.channel, playerIp = bettingDataRNG_SG.playerIp, transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(), gameCode = bettingDataRNG_SG.gameCode, ticketId = bettingDataRNG_SG.ticketId, referenceId = bettingDataRNG_SG.referenceId, specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame), refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds), TicketStatus = bettingDataRNG_SG.TicketStatus });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRng_SG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0 && bettingDataRNG_SG.amount != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_SG.amount, (int)Tp.SG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_SG.amount).ToString("#,##0.##")}", bettingDataRNG_SG.transferId, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PlaceBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_PP.amount *= -1;
            bettingDataRNG_PP.TicketStatus = (int)TicketStatus.UnSettled;
            bettingDataRNG_PP.type = BettransType.Bet;
            var p = new DynamicParameters(new { reference = bettingDataRNG_PP.reference, roundId = bettingDataRNG_PP.roundId, gameId = bettingDataRNG_PP.gameId, amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, roundDetails = bettingDataRNG_PP.roundDetails, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode, jackpotId = bettingDataRNG_PP.jackpotId, jackpotContribution = bettingDataRNG_PP.jackpotContribution, type = (int)bettingDataRNG_PP.type, TicketStatus = (int)bettingDataRNG_PP.TicketStatus, promoWinAmount = (decimal?)null, promoWinReference = (string)null, promoCampaignID = (int?)null, promoCampaignType = (string)null });
            /*p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);*/
            result = conn.Execute("Service_BettransRng_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0 && bettingDataRNG_PP.amount != 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.amount, (int)Tp.PP, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_PP.amount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PayoutRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            bettingDataRNG_PP.TicketStatus = bettingDataRNG_PP.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new { reference = bettingDataRNG_PP.reference, roundId = bettingDataRNG_PP.roundId, gameId = bettingDataRNG_PP.gameId ?? "", amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, roundDetails = bettingDataRNG_PP.roundDetails, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode, jackpotId = bettingDataRNG_PP.jackpotId, jackpotContribution = bettingDataRNG_PP.jackpotContribution, type = (int)bettingDataRNG_PP.type, TicketStatus = (int)bettingDataRNG_PP.TicketStatus, promoWinAmount = bettingDataRNG_PP.promoWinAmount, promoWinReference = bettingDataRNG_PP.promoWinReference, promoCampaignID = bettingDataRNG_PP.promoCampaignID, promoCampaignType = bettingDataRNG_PP.promoCampaignType });
            int result = 0;
            try
            {
                result = conn.Execute("Service_BettransRng_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataRNG_PP.amount > 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.amount, (int)Tp.PP, TransType.Settled, $"{bettingDataRNG_PP.type}: {Math.Abs(bettingDataRNG_PP.amount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                    }

                    if (bettingDataRNG_PP.promoWinAmount > 0)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.promoWinAmount, (int)Tp.PP, TransType.Settled, $"{"promoWin"}: {Math.Abs(bettingDataRNG_PP.promoWinAmount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                    }

                    if (result > 0)
                    {
                        trans.Commit();
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

        public int PlaceBetRNG_PG(TpCustInfo tpCustInfo, BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingDataRNG_PG.TransferAmount *= -1;
            bettingDataRNG_PG.UpdatedTime /= 1000;
            bettingDataRNG_PG.CreateTime /= 1000;
            var p = new DynamicParameters(bettingDataRNG_PG);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = conn.Execute("Service_BettransRng_PG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataRNG_PG.TransferAmount != 0)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_PG.TransferAmount).ToString("#,##0.##")}", bettingDataRNG_PG.TransactionId, out afterAmount, conn, trans);
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
