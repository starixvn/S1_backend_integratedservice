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
    public partial class BettingService
    {
        public int PlaceBetRNG_GPI(TpCustInfo tpCustInfo, BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string Transid)
        {
            int bonusId = 0;
            int gameType = (int)GameType.GPI_RNG;

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
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_GPI.amount, (int)Tp.GPI, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_GPI.amount).ToString("#,##0.##")}", bettingDataRNG_GPI.trx_id, out afterAmount, conn, trans);
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_GPI.amount, (int)Tp.GPI, bettingDataRNG_GPI.round_id, gameType, bettingDataRNG_GPI.trx_id, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PlaceBetRNG_Joker(TpCustInfo tpCustInfo, BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Joker_RNG;

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
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_Joker.amount, (int)Tp.Joker, bettingDataRNG_Joker.roundid, gameType, bettingDataRNG_Joker.id, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        public int PlaceBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            //int gameType = (int)GameType.PP_RNG;
            //if (bettingDataRNG_PP.gameId.Length == 3 && int.TryParse(bettingDataRNG_PP.gameId, out int tempGameId))
            //{
            //    gameType = (int)GameType.PP_LiveCasino;
            //}

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
            var category = TpCategory.Slots;
            if (gameType == (int)GameType.PP_RNG)
            {
                result = conn.Execute("Service_BettransRng_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else
            {
                //category = TpCategory.LiveCasino;
                result = conn.Execute("Service_BettransLC_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0 && bettingDataRNG_PP.amount != 0)
            {
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.amount, (int)Tp.PP, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_PP.amount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_PP.amount, (int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), gameType, bettingDataRNG_PP.reference, null, category, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        public int PlaceBetRNG_YGG(TpCustInfo tpCustInfo, BettingDataRNG_YGG insertData, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.YGG_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(insertData);
            p.Add("tickets", (int?)null);
            p.Add("bonusprize", (decimal?)null);
            p.Add("type", (int)BettransType.Bet);
            try
            {
                result = conn.Execute("Service_BettransRng_YGG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && insertData.amount < 0)
                {
                    result = PlaceBetV2(tpCustInfo, insertData.amount, (int)Tp.YGG, insertData.reference, gameType, insertData.subreference, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
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

        public int PlaceBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_SG.amount *= -1;
            bettingDataRNG_SG.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, acctId = bettingDataRNG_SG.acctId, currency = bettingDataRNG_SG.currency, amount = bettingDataRNG_SG.amount, type = bettingDataRNG_SG.type, channel = bettingDataRNG_SG.channel, playerIp = bettingDataRNG_SG.playerIp, transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(), gameCode = bettingDataRNG_SG.gameCode, ticketId = bettingDataRNG_SG.ticketId, referenceId = bettingDataRNG_SG.referenceId, specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame), refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds), TicketStatus = bettingDataRNG_SG.TicketStatus });

            bool cash = false;
            var category = TpCategory.Slots;
            if (gameType == (int)GameType.SG_RNG)
            {
                result = conn.Execute("Service_BettransRng_SGV2_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else
            {
                cash = true;
                category = TpCategory.Fish;
                result = conn.Execute("Service_BettransFish_SG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0 && bettingDataRNG_SG.amount != 0)
            {
                if (cash == false)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingDataRNG_SG.amount,
                        (int)Tp.SG,
                        bettingDataRNG_SG.transferId,
                        gameType,
                        bettingDataRNG_SG.transferId,
                        null,
                        category,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );
                }
                else
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataRNG_SG.amount,
                        gameType,
                        TransType.Wagers,
                        $"Stake: {Math.Abs(bettingDataRNG_SG.amount):#,##0.##}",
                        bettingDataRNG_SG.transferId,
                        out afterAmount,
                        conn,
                        trans
                    );
                }
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        public int PlaceBetRNG_PG(TpCustInfo tpCustInfo, BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingDataRNG_PG.TransferAmount *= -1;
            bettingDataRNG_PG.UpdatedTime /= 1000;
            bettingDataRNG_PG.CreateTime /= 1000;
            var p = new DynamicParameters(bettingDataRNG_PG);
            //p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            //p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = conn.Execute("Service_BettransRng_PGV2_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataRNG_PG.TransferAmount != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataRNG_PG.TransferAmount).ToString("#,##0.##")}", bettingDataRNG_PG.TransactionId, out afterAmount, conn, trans);
                    result = PlaceBetV2(tpCustInfo, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, bettingDataRNG_PG.TransactionId, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
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

        public int PlaceBetRNG_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount) * -1;

            try
            {
                if (bettingDataRNG_CQ9.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        bettingDataRNG_CQ9.roundid,
                        gameType,
                        bettingDataRNG_CQ9.mtcode,
                        null,
                        TpCategory.Slots,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );
                    if (result > 0)
                    {
                        var p = new DynamicParameters(new
                        {
                            account = bettingDataRNG_CQ9.account,
                            eventUnix = bettingDataRNG_CQ9.eventUnix,
                            gamehall = bettingDataRNG_CQ9.gamehall,
                            gamecode = bettingDataRNG_CQ9.gamecode,
                            roundid = bettingDataRNG_CQ9.roundid,
                            amount = bettingDataRNG_CQ9.amount,
                            mtcode = bettingDataRNG_CQ9.mtcode,
                            before = afterAmount + (Math.Abs(bettingDataRNG_CQ9.amount)),
                            balance = afterAmount,
                            TicketType = (int)BettransType.Bet,
                            Actions = "bet",
                            TicketStatus = (int)TicketStatus.UnSettled
                        });

                        result = conn.Execute("Service_BettransRng_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
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
                    trans.Rollback();
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int PlaceBetRNG_Iconic(TpCustInfo tpCustInfo, BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_Iconic.amount *= -1;
            var p = new DynamicParameters(new
            {
                transactionId = bettingDataRNG_Iconic.transactionId,
                username = bettingDataRNG_Iconic.username,
                roundId = bettingDataRNG_Iconic.roundId,
                gameId = bettingDataRNG_Iconic.gameId,
                productId = bettingDataRNG_Iconic.productId,
                amount = bettingDataRNG_Iconic.amount,
                CreateUnix = new DateTimeOffset(bettingDataRNG_Iconic.roundCreatedAt).ToUnixTimeSeconds(),
                UpdateUnix = new DateTimeOffset(bettingDataRNG_Iconic.roundUpdatedAt).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 0
            });
            try
            {
                var category = TpCategory.Slots;
                var gameType = (int)GameType.Iconic_RNG;
                result = conn.Execute("Service_BettransRng_Iconic_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataRNG_Iconic.amount != 0)
                {

                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingDataRNG_Iconic.amount,
                        (int)Tp.Iconic,
                        bettingDataRNG_Iconic.transactionId.ToString(),
                        gameType,
                        bettingDataRNG_Iconic.transactionId.ToString(),
                        null,
                        category,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

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

        public int PlaceBetFish_Iconic(TpCustInfo tpCustInfo, BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataFish_Iconic.amount *= -1;
            var p = new DynamicParameters(new
            {
                transactionId = bettingDataFish_Iconic.transactionId,
                username = bettingDataFish_Iconic.username,
                amount = bettingDataFish_Iconic.amount,
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 0
            });
            try
            {
                var gameType = (int)GameType.Iconic_Fish;
                result = conn.Execute("Service_BettransFish_Iconic_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataFish_Iconic.amount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataFish_Iconic.amount,
                        gameType,
                        TransType.Wagers,
                        $"Stake: {Math.Abs(bettingDataFish_Iconic.amount):#,##0.##}",
                        bettingDataFish_Iconic.transactionId.ToString(),
                        out afterAmount,
                        conn,
                        trans
                    );
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

    }
}
