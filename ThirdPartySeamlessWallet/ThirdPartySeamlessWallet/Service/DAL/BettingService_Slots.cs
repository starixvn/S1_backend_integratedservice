using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using System.Globalization;

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

        public int WithdrawFish_Joker(TpCustInfo tpCustInfo, BettingDataFish_Joker bettingDataFish_Joker, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataFish_Joker.timestamp).DateTime;
                var check = _partitionService.Query<string>(partitionTime, "Service_BettransFish_Joker_Check", bettingDataFish_Joker);
                if (check.Any())
                {
                    return -1;//Duplicated
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();



                bettingDataFish_Joker.TicketType = (int)BettransType.Bet;
                result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", bettingDataFish_Joker, trans);

                if (result > 0 && bettingDataFish_Joker.amount != 0)
                {
                    var bonusId = 0;
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingDataFish_Joker.amount) * -1,
                        (int)Tp.Joker,
                        bettingDataFish_Joker.roundid,
                        (int)GameType.Joker_Fish,
                        bettingDataFish_Joker.id.Length > 45 ? bettingDataFish_Joker.id.Substring(0, 45) : bettingDataFish_Joker.id,
                        null,
                        TpCategory.Fish,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                result = 0;
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
                category = TpCategory.LiveCasino;
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
            var result = 0;
            afterAmount = 0;
            int bonusId = 0;
            int gameType = (int)GameType.YGG_RNG;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString")))
                {
                    conn.Open();
                    var p = new DynamicParameters(insertData);
                    p.Add("tickets", (int?)null);
                    p.Add("bonusprize", (decimal?)null);
                    p.Add("type", (int)BettransType.Bet);

                    using (var trans = conn.BeginTransaction())
                    {
                        result = conn.Execute("Service_BettransRngV2_YGG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                        if (result > 0 && insertData.amount < 0)
                        {
                            result = PlaceBetV2(tpCustInfo, insertData.amount, (int)Tp.YGG, insertData.reference, gameType, insertData.subreference, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
                        }

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                        else
                        {
                            result = -1;
                            trans.Rollback();
                        }
                    }
                    conn.Close();
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
                        (int)Tp.SG,
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
            p.Add("BetAmount", bettingDataRNG_PG.TransferAmount);
            p.Add("WinAmount", 0);
            try
            {
                result = conn.Execute("Service_BettransRng_PG_Ins_V2", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataRNG_PG.TransferAmount != 0)
                {
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
                    throw;
                }
            }

            return result;
        }

        #region MG
        public async Task<MGPlaceBetResult> PlaceBetRNG_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            var result = 0;
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();
            bettingDataRNG_MG.amount *= -1;
            bettingDataRNG_MG.TransType = "D";
            bettingDataRNG_MG.TicketType = (int)BettransType.Bet;
            bettingDataRNG_MG.TicketStatus = (int)TicketStatus.UnSettled;
            bettingDataRNG_MG.SettleFlag = (int)SettleFlag.Running;

            try
            {
                var bettransRngMG = await conn.QueryFirstOrDefaultAsync<BettransRng_MG>("Service_BettransRngByBet_MG_Get", new { bettingDataRNG_MG.betId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (bettransRngMG != null)
                {
                    return new MGPlaceBetResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                        ExtTxnId = bettransRngMG.ExtTxnId
                    };
                }

                var p = new DynamicParameters(bettingDataRNG_MG);
                p.Add("extTxnId", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = conn.Execute("Service_BettransRng_MG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var extTxnId = p.Get<long>("extTxnId");

                if (result > 0 && bettingDataRNG_MG.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingDataRNG_MG.amount,
                        (int)Tp.MG,
                        bettingDataRNG_MG.betId,
                        gameType,
                        bettingDataRNG_MG.txnId,
                        null,
                        TpCategory.Slots,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }

                return new MGPlaceBetResult()
                {
                    Result = result,
                    AfterAmount = afterAmount,
                    ExtTxnId = extTxnId
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, $"PlaceBetRNG_MG failed: {JsonConvert.SerializeObject(bettingDataRNG_MG)}");
                throw;
            }
        }
        #endregion

        #region CQ9

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

        public int RolloutFish_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
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
                    result = walletService.SetBalance(
                       tpCustInfo.CustId,
                       tpCustInfo.SiteId,
                       tpCustInfo.UserName,
                       bettingDataRNG_CQ9.amount,
                       (int)Tp.CQ9,
                       TransType.Wagers,
                       $"Stake: {Math.Abs(bettingDataRNG_CQ9.amount):#,##0.##}",
                       bettingDataRNG_CQ9.mtcode,
                       out afterAmount,
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
                            Actions = "rollout",
                            TicketStatus = (int)TicketStatus.UnSettled
                        });

                        result = conn.Execute("Service_BettransFish_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

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

        public int TakeAllFish_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
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
                    result = walletService.SetBalance(
                       tpCustInfo.CustId,
                       tpCustInfo.SiteId,
                       tpCustInfo.UserName,
                       bettingDataRNG_CQ9.amount,
                       (int)Tp.CQ9,
                       TransType.Wagers,
                       $"Stake: {Math.Abs(bettingDataRNG_CQ9.amount):#,##0.##}",
                       bettingDataRNG_CQ9.mtcode,
                       out afterAmount,
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
                            Actions = "takeall",
                            TicketStatus = (int)TicketStatus.UnSettled,
                            SettleFlag = 2
                        });

                        result = conn.Execute("Service_BettransFish_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
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

        #endregion

        #region Iconic

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
                        (int)Tp.Iconic,
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

        #endregion

        #region Netent

        public int PlaceBetRNG_Netent(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingData_EVO.transaction.amount *= -1;
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
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            try
            {
                var category = TpCategory.Slots;
                var gameType = (int)GameType.Netent_RNG;
                result = conn.Execute("Service_BettransRng_Netent_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingData_EVO.transaction.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingData_EVO.transaction.amount,
                        (int)Tp.Netent,
                        bettingData_EVO.transaction.refId,
                        gameType,
                        bettingData_EVO.transaction.id,
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
                    throw;
                }
            }

            return result;
        }

        #endregion

        #region RedTiger

        public int PlaceBetRNG_RedTiger(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingData_EVO.transaction.amount *= -1;
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
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            try
            {
                var category = TpCategory.Slots;
                var gameType = (int)GameType.RedTiger_RNG;
                result = conn.Execute("Service_BettransRng_RedTiger_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingData_EVO.transaction.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingData_EVO.transaction.amount,
                        (int)Tp.RedTiger,
                        bettingData_EVO.transaction.refId,
                        gameType,
                        bettingData_EVO.transaction.id,
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
                    throw;
                }
            }

            return result;
        }

        #endregion

        #region AP
        public int PlaceBet_AP(TpCustInfo tpCustInfo, BettingDataRNG_AP bettingDataRNG_AP, out decimal afterAmount, out long sq)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AP_RNG;
            bettingDataRNG_AP.TicketStatus = (int)TicketStatus.UnSettled;
            bettingDataRNG_AP.TicketType = (int)BettransType.Bet;
            afterAmount = 0;
            sq = 0;
            CultureInfo culture = CultureInfo.InvariantCulture;
            DateTime partitionTime = DateTime.Parse(bettingDataRNG_AP.timestamp, culture, DateTimeStyles.None);
            long unixbettime = ((DateTimeOffset)DateTime.Parse(bettingDataRNG_AP.bettime, culture, DateTimeStyles.None)).ToUnixTimeSeconds();
            long unixtimestamp = ((DateTimeOffset)DateTime.Parse(bettingDataRNG_AP.timestamp, culture, DateTimeStyles.None)).ToUnixTimeSeconds();

            var p = new DynamicParameters(new
            {
                seq = bettingDataRNG_AP.seq,
                sitecode = bettingDataRNG_AP.sitecode,
                gamecode = bettingDataRNG_AP.gamecode,
                gameroundid = bettingDataRNG_AP.gameroundid,
                stake = bettingDataRNG_AP.stake,
                totalstake = bettingDataRNG_AP.totalstake,
                totalwin = bettingDataRNG_AP.totalwin,
                jpwin = bettingDataRNG_AP.jpwin,
                bettime = unixbettime,
                settletime = 0,
                jpsettletime = 0,
                timestamp = unixtimestamp,
                playerid = bettingDataRNG_AP.playerid,
                hitjackpot = bettingDataRNG_AP.hitjackpot,
                ip = bettingDataRNG_AP.ip,
                TicketType = bettingDataRNG_AP.TicketType,
                TicketStatus = bettingDataRNG_AP.TicketStatus,
            });
            p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            var bettransAP = _partitionService.Query<BettransRNG_AP>(partitionTime, "Service_BettransRng_AP_Get", new { bettingDataRNG_AP.seq, bettingDataRNG_AP.sitecode, bettingDataRNG_AP.gamecode, bettingDataRNG_AP.gameroundid });
            if (bettransAP.Any())
            {
                return 0;
            }

            var result = 0;

            result = _partitionService.Execute(partitionTime, "Service_BettransRng_AP_Ins", p, trans);

            if (result > 0 && bettingDataRNG_AP.stake != 0)
            {
                result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataRNG_AP.stake) * -1, (int)Tp.AP, bettingDataRNG_AP.gameroundid, gameType, bettingDataRNG_AP.seq, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                sq = p.Get<long>("SQ");
                trans.Commit();
            }

            return result;
        }
        #endregion

        #region Habanero

        public int PlaceBet_HB(TpCustInfo tpCustInfo, PlaceBetRngDto_HB placeBetRngDto, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            afterAmount = 0m;
            try
            {
                var partitionTime = placeBetRngDto.dtevent;
                var query = new
                {
                    placeBetRngDto.transferid
                };
                var check = _partitionService.Query<BettransData_HB>(partitionTime, "Service_BettransRng_HB_Check", query);
                if (check.Any())
                {
                    return 1;
                }

                placeBetRngDto.TicketType = (int)BettransType.Bet;
                placeBetRngDto.TicketStatus = (int)TicketStatus.UnSettled;
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_HB_Ins", placeBetRngDto, trans);

                if (result > 0 && placeBetRngDto.amount != 0)
                {
                    var roundId = placeBetRngDto.friendlygameinstanceid.ToString();
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(placeBetRngDto.amount) * -1,
                        (int)Tp.HB,
                        roundId,
                        (int)GameType.HB_RNG,
                        placeBetRngDto.transferid,
                        null,
                        TpCategory.Slots,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public bool QueryBettrans(string transferid)
        {
            var partitionTime = DateTime.UtcNow;
            var query = new
            {
                transferid
            };
            var check = _partitionService.Query<BettransData_HB>(partitionTime, "Service_BettransRng_HB_Check", query);
            return check.Any();
        }
        #endregion

        #region AMB Slot

        public bool IsExist_AMB(DateTime partitionTime, BettingDataRNG_AMB data, MySqlConnection conn, MySqlTransaction trans)
        {
            var query = new { transactionId = data.transactionId ,roundId = data.roundId};
            var isExist = _partitionService.Query<BettingDataRNG_AMB>(partitionTime, "Service_BettransRng_AMB_Check", query);

            if (isExist.Any())
            {
                return true;
            }
            return false;
        }

        public bool PlaceBet_AMB(DateTime partitionTime, BettingDataRNG_AMB data, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_AMB_Ins", data, trans);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool SetBalance_AMB(TpCustInfo tpCustInfo, BettingDataRNG_AMB data,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PlaceBetV2(tpCustInfo,
                                data.amount,
                                (int)Tp.AMB,
                                data.transactionId,  //TpTransId
                                (int)GameType.AMB_RNG,
                                data.roundId,  //RefId
                                null,
                                TpCategory.Slots,
                                out afterAmount,
                                ref bonusId,
                                conn,
                                trans);
            if (result == 0)
            {
                return false;
            }
            return true;
        }

        #endregion

    }
}
