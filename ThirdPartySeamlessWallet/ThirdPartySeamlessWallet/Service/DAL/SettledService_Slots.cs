using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using System.Globalization;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        #region GPI

        public int PayoutRNG_GPI(TpCustInfo tpCustInfo, SettledDataRNG_GPI settledDataRNG_GPI, out decimal afterAmount, out string TransId)
        {
            int bonusId = 0;
            int gameType = (int)GameType.GPI_RNG;

            afterAmount = 0;
            TransId = settledDataRNG_GPI.trx_id;

            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(settledDataRNG_GPI);
            p.Add("jackpot_contribution", 0);
            p.Add("type", (int)BettransType.Payout);

            try
            {
                result = conn.Execute("Service_BettransRNG_GPI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (settledDataRNG_GPI.amount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, settledDataRNG_GPI.amount, (int)Tp.GPI, TransType.Settled, $"Payout: {Math.Abs(settledDataRNG_GPI.amount).ToString("#,##0.##")}", settledDataRNG_GPI.trx_id, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, settledDataRNG_GPI.amount, (int)Tp.GPI, settledDataRNG_GPI.round_id, gameType, TpCategory.Slots, settledDataRNG_GPI.trx_id, TransType.Payout, null, out afterAmount, $"Payout", out bonusId, conn, trans);
                    }
                    else if ((settledDataRNG_GPI.jackpot_win ?? 0m) == 0m)
                    {
                        GetTpBonus((int)Tp.GPI, settledDataRNG_GPI.round_id, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if ((settledDataRNG_GPI.jackpot_win ?? 0m) > 0m)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, settledDataRNG_GPI.jackpot_win ?? 0, (int)Tp.GPI, TransType.Settled, $"Jackpot: {Math.Abs(settledDataRNG_GPI.jackpot_win ?? 0).ToString("#,##0.##")}", settledDataRNG_GPI.trx_id, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, settledDataRNG_GPI.jackpot_win ?? 0, (int)Tp.GPI, settledDataRNG_GPI.round_id, gameType, TpCategory.Slots, settledDataRNG_GPI.trx_id, TransType.Prize, null, out afterAmount, $"Jackpot", out bonusId, conn, trans);
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

        public IEnumerable<SettleDataRNG_GPI> GetUnsettledListRNG_GPI()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataRNG_GPI>("Service_BettransRNG_GPI_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlagRNG_GPI(string roundId, string custId, int SettleFlag)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new
            {
                round_id = roundId,
                cust_id = custId,
                SettleFlag = SettleFlag
            });

            var result = conn.Execute("Service_BettransRNG_GPI_Settle_Set", p, commandType: CommandType.StoredProcedure);
            return result;
        }

        #endregion

        #region Joker

        public int SettledRNG_Joker(TpCustInfo tpCustInfo, BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Joker_RNG;

            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            bettingDataRNG_Joker.TicketStatus = bettingDataRNG_Joker.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(bettingDataRNG_Joker);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransRng_Joker_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataRNG_Joker.amount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_Joker.amount, (int)Tp.Joker, TransType.Settled, $"Payout: {Math.Abs(bettingDataRNG_Joker.amount).ToString("#,##0.##")}", bettingDataRNG_Joker.id, out afterAmount, conn, trans);
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_Joker.amount,
                            (int)Tp.Joker,
                            bettingDataRNG_Joker.roundid,
                            gameType,
                            TpCategory.Slots,
                            bettingDataRNG_Joker.id,
                            (bettingDataRNG_Joker.type == "bonus") ? TransType.Prize : TransType.Payout,
                            null,
                            out afterAmount,
                            (bettingDataRNG_Joker.type == "bonus") ? $"{bettingDataRNG_Joker.id} Bonus Prize" : "Payout",
                            out bonusId,
                            conn,
                            trans
                        );
                    }
                    else
                    {
                        GetTpBonus((int)Tp.Joker, bettingDataRNG_Joker.roundid, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.Joker, GameType.Joker_RNG, bettingDataRNG_Joker.roundid, trans);
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
                    result = -2;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int CancelBetRNG_Joker(TpCustInfo tpCustInfo, CancelBetDataRNG_Joker cancelBetDataRNG_Joker, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Joker_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(cancelBetDataRNG_Joker);
            p.Add("CancelStatus", dbType: DbType.Int16, direction: ParameterDirection.Output);
            p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("HasSettled", dbType: DbType.Int16, direction: ParameterDirection.Output);
            result = conn.Execute("Service_CancelBetRng_Joker_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal CancelStake = p.Get<decimal>("CancelStake");
                short CancelStatus = p.Get<short>("CancelStatus");

                if (CancelStatus == 1 && CancelStake != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, CancelStake, (int)Tp.Joker, TransType.Refunds, $"Refund: {CancelStake.ToString("#,##0.##")}", cancelBetDataRNG_Joker.betid, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, CancelStake, (int)Tp.Joker, cancelBetDataRNG_Joker.roundid, gameType, TpCategory.Slots, cancelBetDataRNG_Joker.betid, TransType.Refunds, null, out afterAmount, "CancelBet", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }
                short hasSettled = p.Get<short>("HasSettled");
                if (hasSettled == 1)
                {
                    return -2;
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        public int DepositFish_Joker(TpCustInfo tpCustInfo, BettingDataFish_Joker bettingDataFish_Joker, out decimal afterAmount)
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

                bettingDataFish_Joker.TicketType = (int)BettransType.Payout;
                result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", bettingDataFish_Joker, trans);

                if (result > 0 && bettingDataFish_Joker.amount != 0)
                {
                    result = PayoutV2(
                         tpCustInfo: tpCustInfo,
                         amount: bettingDataFish_Joker.amount,
                         tpId: (int)Tp.Joker,
                         tpTransId: bettingDataFish_Joker.roundid,
                         gameType: (int)GameType.Joker_Fish,
                         tpCategory: TpCategory.Fish,
                         refId: bettingDataFish_Joker.id.Length > 45 ? bettingDataFish_Joker.id.Substring(0, 45) : bettingDataFish_Joker.id,
                         TransType.Payout,
                         betData: null,
                         out afterAmount,
                         "Payout",
                         out var bonusId,
                         conn,
                         trans,
                         AllowNegative.NotAllow);
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

        public int TransactionFish_Joker(TpCustInfo tpCustInfo, BettingDataFish_Joker bettingDataFish_Joker)
        {
            var result = 0;
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

                bettingDataFish_Joker.TicketType = (int)BettransType.EndRound;
                result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", bettingDataFish_Joker, trans);

                if (result > 0)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.Joker, GameType.Joker_Fish, bettingDataFish_Joker.roundid, trans, partitionTime);
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

        #endregion

        #region YGG

        public int PayoutRNG_YGG(TpCustInfo tpCustInfo, PayoutDataRNG_YGG insertData, out decimal afterAmount)
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

                    using (var trans = conn.BeginTransaction())
                    {
                        var parameters = new DynamicParameters(insertData);
                        parameters.Add("type", (int)BettransType.Payout);

                        string SpName = "Service_BettransRngV2_YGG_Ins";
                        result = conn.Execute(SpName, parameters, transaction: trans, commandType: CommandType.StoredProcedure);


                        if (result > 0)
                        {
                            if (insertData.amount > 0)
                            {
                                result = PayoutV2(tpCustInfo, insertData.amount, (int)Tp.YGG, insertData.reference, gameType, TpCategory.Slots, insertData.subreference, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                            }
                            else if (insertData.bonusprize == 0)
                            {
                                GetTpBonus((int)Tp.YGG, insertData.reference, gameType, TpCategory.Slots, out int custId, out bonusId);
                            }

                            if (insertData.bonusprize > 0)
                            {
                                result = PayoutV2(tpCustInfo, insertData.bonusprize, (int)Tp.YGG, insertData.reference, gameType, TpCategory.Slots, insertData.subreference, TransType.Prize, null, out afterAmount, "Bonus", out bonusId, conn, trans);
                            }

                            if (insertData.SettleFlag == (int)SettleFlag.EndGame)
                            {
                                result = _tpUnsettleService.InsertTpUnsettle(Tp.YGG, GameType.YGG_RNG, insertData.reference, trans);
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

        public int CancelBetRNG_YGG(TpCustInfo tpCustInfo, BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.YGG_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_YGG.reference, bettingDataRNG_YGG.subreference });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_CancelBetRng_YGG_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal Stake = p.Get<decimal>("Stake");
                decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                if (WinLostAmount != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, WinLostAmount * -1, (int)Tp.YGG, TransType.Refunds, $"Refund: {(WinLostAmount * -1).ToString("#,##0.##")}", bettingDataRNG_YGG.subreference, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, WinLostAmount * -1, (int)Tp.YGG, bettingDataRNG_YGG.reference, gameType, TpCategory.Slots, bettingDataRNG_YGG.subreference, TransType.Refunds, null, out afterAmount, "Refund", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.YGG,
                        Stake = Stake,
                        ValidBet = 0,
                        WinlostAmount = 0,
                        TransId = bettingDataRNG_YGG.reference,
                        GameType = GameType.YGG_RNG,
                        TpUserName = bettingDataRNG_YGG.playerid,
                        SettledTime = 0,
                        SettlementType = SettlementType.SettledForInvalid,
                        TicketStatus = TicketStatus.Refund
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
                return -1;
            }

            return result;
        }

        #endregion

        #region PP

        public int PayoutRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            //int gameType = (int)GameType.PP_RNG;

            //if (bettingDataRNG_PP.gameId?.Length == 3 && int.TryParse(bettingDataRNG_PP.gameId, out int tempGameId))
            //{
            //    gameType = (int)GameType.PP_LiveCasino;
            //}

            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            bettingDataRNG_PP.TicketStatus = bettingDataRNG_PP.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new { reference = bettingDataRNG_PP.reference, roundId = bettingDataRNG_PP.roundId, gameId = bettingDataRNG_PP.gameId ?? "", amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, roundDetails = bettingDataRNG_PP.roundDetails, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode, jackpotId = bettingDataRNG_PP.jackpotId, jackpotContribution = bettingDataRNG_PP.jackpotContribution, type = (int)bettingDataRNG_PP.type, TicketStatus = (int)bettingDataRNG_PP.TicketStatus, promoWinAmount = bettingDataRNG_PP.promoWinAmount, promoWinReference = bettingDataRNG_PP.promoWinReference, promoCampaignID = bettingDataRNG_PP.promoCampaignID, promoCampaignType = bettingDataRNG_PP.promoCampaignType });
            int result = 0;
            try
            {
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

                if (result > 0)
                {
                    if (bettingDataRNG_PP.amount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.amount, (int)Tp.PP, TransType.Settled, $"{bettingDataRNG_PP.type}: {Math.Abs(bettingDataRNG_PP.amount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataRNG_PP.amount, (int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), gameType, category, bettingDataRNG_PP.reference, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }

                    if (bettingDataRNG_PP.promoWinAmount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PP.promoWinAmount, (int)Tp.PP, TransType.Settled, $"{"promoWin"}: {Math.Abs(bettingDataRNG_PP.promoWinAmount).ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataRNG_PP.promoWinAmount, (int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), gameType, category, bettingDataRNG_PP.reference, TransType.Prize, null, out afterAmount, "Bonus", out bonusId, conn, trans);
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
        public int BonusWinRNG_PP(BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = 0;
            try
            {
                result = conn.Execute("Service_BettransRng_PP_BonusWin_Ins", new { reference = bettingDataRNG_PP.reference, amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode }, transaction: trans, commandType: CommandType.StoredProcedure);
                trans.Commit();
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

        public int SetSettleFlag_BettransLC_PP_Payout(long roundId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransLC_PP_Settle_Pre_Set_V2", new { roundId = roundId }, commandType: CommandType.StoredProcedure);
        }

        public int CancelBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_PP.reference });
            p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var category = TpCategory.Slots;
            if (gameType == (int)GameType.PP_RNG)
            {
                result = conn.Execute("Service_CancelBetRng_PP_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else
            {
                category = TpCategory.LiveCasino;
                result = conn.Execute("Service_BettransLC_PP_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }


            if (result > 0)
            {
                decimal cancelStake = p.Get<decimal>("CancelStake");

                if (cancelStake != bettingDataRNG_PP.amount)
                {
                    throw new Exception($"Cancel amount {bettingDataRNG_PP.amount:#,##0.##} it not equal {cancelStake:#,##0.##}");
                }

                if (cancelStake != 0)
                {
                    result = PayoutV2(tpCustInfo, cancelStake, (int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), (int)gameType, category, bettingDataRNG_PP.reference, TransType.Refunds, null, out afterAmount, "Refund", out bonusId, conn, trans);
                }

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
        public int PromoBonus_PP(TpCustInfo tpCustInfo, PromoBonus promoBonus, out decimal afterAmount)
        {
            GameType gameType = GameType.PP_RNG;
            int bonusId = 0;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = conn.Execute("Service_PromoBonus_PP_Ins", promoBonus, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                if (promoBonus.Amount > 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, promoBonus.Amount, (int)Tp.PP, TransType.Settled, $"PromoWin: {Math.Abs(promoBonus.Amount).ToString("#,##0.##")}", promoBonus.Reference, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, promoBonus.Amount, (int)Tp.PP, promoBonus.Reference, (int)gameType, TpCategory.Slots, promoBonus.Reference, TransType.Prize, null, out afterAmount, "Bonus", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.PP,
                        Stake = 0,
                        ValidBet = 0,
                        WinlostAmount = promoBonus.Amount,
                        TransId = promoBonus.Reference,
                        GameType = GameType.PP_RNG,
                        TpUserName = promoBonus.UserId,
                        SettledTime = promoBonus.Timestamp / 1000,
                        SettlementType = SettlementType.SettledForValid,
                        BonusId = bonusId
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
                result = -1;
            }

            return result;
        }
        public int AdjustmentRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var p = new DynamicParameters(new
            {
                reference = bettingDataRNG_PP.reference,
                roundId = bettingDataRNG_PP.roundId,
                gameId = bettingDataRNG_PP.gameId ?? "",
                amount = bettingDataRNG_PP.amount,
                userId = bettingDataRNG_PP.userId,
                roundDetails = bettingDataRNG_PP.roundDetails,
                timestamp = bettingDataRNG_PP.timestamp,
                bonusCode = bettingDataRNG_PP.bonusCode,
                jackpotId = bettingDataRNG_PP.jackpotId,
                jackpotContribution = bettingDataRNG_PP.jackpotContribution,
                type = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.Refund,
                promoWinAmount = bettingDataRNG_PP.promoWinAmount,
                promoWinReference = bettingDataRNG_PP.promoWinReference,
                promoCampaignID = bettingDataRNG_PP.promoCampaignID,
                promoCampaignType = bettingDataRNG_PP.promoCampaignType
            }
            );
            int result = 0;
            try
            {
                var category = TpCategory.Slots;
                var tpGameType = GameType.PP_RNG;
                if (gameType == (int)GameType.PP_RNG)
                {
                    result = conn.Execute("Service_BettransRng_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    category = TpCategory.LiveCasino;
                    tpGameType = GameType.PP_LiveCasino;
                    result = conn.Execute("Service_BettransLC_PP_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }

                if (result > 0)
                {
                    if (bettingDataRNG_PP.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_PP.amount,
                            (int)Tp.PP,
                            bettingDataRNG_PP.roundId.ToString(),
                            gameType,
                            category,
                            bettingDataRNG_PP.reference,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Adjustment",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        _tpUnsettleService.InsertTpUnsettle(Tp.PP, tpGameType, bettingDataRNG_PP.roundId.ToString());
                        trans.Commit();
                        var setFlag = SetSettleFlag_BettransLC_PP_Payout(bettingDataRNG_PP.roundId);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
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

        #region SG

        public int CancelBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var result = 0;
                var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, referenceId = bettingDataRNG_SG.referenceId });
                p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                var category = (gameType == (int)GameType.SG_Fish) ? TpCategory.Fish : TpCategory.Slots;
                var cancelSetSp = (gameType == (int)GameType.SG_RNG) ? "Service_BettransRng_SG_CancelBet_Set" : "Service_BettransFish_SG_CancelBet_Set";

                result = conn.Execute(cancelSetSp, p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    decimal cancelStake = p.Get<decimal>("CancelStake");
                    int bonusId = 0;
                    if (cancelStake != bettingDataRNG_SG.amount)
                    {
                        Exception exception = new Exception($"SG Cancel amount {bettingDataRNG_SG.amount:#,##0.##} it not equal {cancelStake:#,##0.##}");
                        throw exception;
                    }

                    if (cancelStake != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingDataRNG_SG.amount),
                            (int)Tp.SG,
                            bettingDataRNG_SG.referenceId,
                            gameType,
                            category,
                            bettingDataRNG_SG.referenceId,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Cancel",
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
                else
                {
                    p = new DynamicParameters(new
                    {
                        transferId = bettingDataRNG_SG.transferId,
                        acctId = bettingDataRNG_SG.acctId,
                        currency = bettingDataRNG_SG.currency,
                        amount = bettingDataRNG_SG.amount,
                        type = bettingDataRNG_SG.type,
                        channel = bettingDataRNG_SG.channel,
                        playerIp = bettingDataRNG_SG.playerIp,
                        transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        gameCode = bettingDataRNG_SG.gameCode,
                        referenceId = bettingDataRNG_SG.referenceId,
                        TicketStatus = (int)TicketStatus.Refund,
                        SettleFlag = (int)SettleFlag.Settled
                    });

                    p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                    var cancelInsSp = (gameType == (int)GameType.SG_RNG) ? "Service_BettransRng_SG_CancelBet_Ins" : "Service_BettransFish_SG_CancelBet_Ins";
                    conn.Execute(cancelInsSp, p, transaction: trans, commandType: CommandType.StoredProcedure);
                    var refLC = p.Get<int>("out_result");
                    if (refLC == 2)
                    {
                        // insert cancel record
                        trans.Commit();
                    }

                    return -1;
                }

                return result;
            }
            catch (MySqlException MyEx)
            {
                if (MyEx.Number == 1062)
                {
                    return -1;
                }
                throw;
            }
        }

        public int PayoutRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_SG.TicketStatus = bettingDataRNG_SG.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, acctId = bettingDataRNG_SG.acctId, currency = bettingDataRNG_SG.currency, amount = bettingDataRNG_SG.amount, type = bettingDataRNG_SG.type, channel = bettingDataRNG_SG.channel, playerIp = bettingDataRNG_SG.playerIp, transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(), gameCode = bettingDataRNG_SG.gameCode, ticketId = bettingDataRNG_SG.ticketId, referenceId = bettingDataRNG_SG.referenceId, specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame), refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds), TicketStatus = bettingDataRNG_SG.TicketStatus });

            try
            {
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

                int bonusId = 0;
                if (result > 0)
                {
                    if (bettingDataRNG_SG.amount > 0)
                    {
                        if (cash == false)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                bettingDataRNG_SG.amount,
                                (int)Tp.SG,
                                bettingDataRNG_SG.referenceId,
                                gameType,
                                category,
                                bettingDataRNG_SG.transferId,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"{bettingDataRNG_SG.type}",
                                out bonusId,
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
                                TransType.Payout,
                                $"Payout: {Math.Abs(bettingDataRNG_SG.amount):#,##0.##}",
                                bettingDataRNG_SG.transferId,
                                out _,
                                conn,
                                trans
                            );
                        }
                    }
                    //else
                    //{
                    //    GetTpBonus(
                    //        (int)Tp.SG,
                    //        bettingDataRNG_SG.referenceId,
                    //        gameType,
                    //        category,
                    //        out int custId,
                    //        out bonusId
                    //   );
                    //}

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

        public SettleData_SG GetSettle_TpWagerRng_SG(string TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<SettleData_SG>("Service_TpWagerRng_SG_Settle_Get", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region PG

        public int SettledRNG_PG(TpCustInfo tpCustInfo, BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_PG.UpdatedTime /= 1000;
            bettingDataRNG_PG.CreateTime /= 1000;
            var p = new DynamicParameters(bettingDataRNG_PG);
            p.Add("BetAmount", 0);
            p.Add("WinAmount", bettingDataRNG_PG.TransferAmount);
            try
            {
                result = conn.Execute("Service_BettransRng_PG_Ins_V3", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataRNG_PG.TransferAmount > 0)
                    {
                        result = PayoutV2(tpCustInfo, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, TpCategory.Slots, bettingDataRNG_PG.TransactionId, TransType.Payout, null, out afterAmount, $"{bettingDataRNG_PG.type}", out bonusId, conn, trans);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (bettingDataRNG_PG.IsEndRound)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.PG, GameType.PG_RNG, bettingDataRNG_PG.ParentBetId, trans);
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
                    throw;
                }
            }

            return result;
        }

        public int RewardRNG_PG(TpCustInfo tpCustInfo, AdjustmentDataRNG_PG adjustmentDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                adjustment_transaction_id = adjustmentDataRNG_PG.AdjustmentTransactionId,
                adjustment_id = adjustmentDataRNG_PG.AdjustmentId,
                player_name = adjustmentDataRNG_PG.PlayerName,
                currency_code = adjustmentDataRNG_PG.CurrencyCode,
                transfer_amount = adjustmentDataRNG_PG.TransferAmount,
                adjustment_time = adjustmentDataRNG_PG.AdjustmentTime / 1000,
                transaction_type = adjustmentDataRNG_PG.TransactionType,
                bet_type = adjustmentDataRNG_PG.BetType
            });

            try
            {
                result = conn.Execute("Service_RewardRNG_PG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (adjustmentDataRNG_PG.TransferAmount != 0)
                    {
                        string memo = $"{adjustmentDataRNG_PG.AdjustmentId} Prize";
                        if (adjustmentDataRNG_PG.TransferAmount < 0)
                        {
                            memo = $"{memo} Adjustment";
                        }

                        result = PayoutV2(
                            tpCustInfo,
                            adjustmentDataRNG_PG.TransferAmount,
                            (int)Tp.PG,
                            adjustmentDataRNG_PG.AdjustmentId,
                            gameType,
                            TpCategory.Slots,
                            adjustmentDataRNG_PG.AdjustmentTransactionId,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            memo,
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (afterAmount < 0)
                    {
                        trans.Rollback();
                        return 0;
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

        public int BetAndPayoutRNG_PG(TpCustInfo tpCustInfo, TransferInOutDataRNG_PG transferInOutDataRNG_PG, ThirdPartyBLL thirdPartyBLL, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int bonusId = 0;
            var category = TpCategory.Slots;
            int gameType = (int)GameType.PG_RNG;

            transferInOutDataRNG_PG.BetAmount = Math.Abs(transferInOutDataRNG_PG.BetAmount) * -1;
            transferInOutDataRNG_PG.UpdatedTime /= 1000;
            transferInOutDataRNG_PG.CreateTime /= 1000;
            var p = new DynamicParameters(transferInOutDataRNG_PG);

            try
            {
                result = conn.Execute("Service_BettransRng_PG_Ins_V3", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (transferInOutDataRNG_PG.BetAmount != 0)
                    {
                        result = _bettingService.PlaceBetV2(
                            tpCustInfo,
                            transferInOutDataRNG_PG.BetAmount,
                            (int)Tp.PG,
                            transferInOutDataRNG_PG.ParentBetId,
                            gameType,
                            transferInOutDataRNG_PG.TransactionId,
                            null,
                            category,
                            out afterAmount,
                            ref bonusId,
                            conn,
                            trans
                       );
                    }

                    bool getBonus = false;
                    if (transferInOutDataRNG_PG.WinAmount != 0)
                    {
                        if (bonusId > 0)
                        {
                            getBonus = true;
                            result = PayoutWithBonusId(
                                tpCustInfo,
                                transferInOutDataRNG_PG.WinAmount,
                                (int)Tp.PG,
                                transferInOutDataRNG_PG.ParentBetId,
                                gameType,
                                category,
                                transferInOutDataRNG_PG.TransactionId,
                                TransType.Payout,
                                out afterAmount,
                                $"{transferInOutDataRNG_PG.type}",
                                bonusId,
                                conn,
                                trans
                            );
                        }
                        else
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                transferInOutDataRNG_PG.WinAmount,
                                (int)Tp.PG,
                                transferInOutDataRNG_PG.ParentBetId,
                                gameType,
                                category,
                                transferInOutDataRNG_PG.TransactionId,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"{transferInOutDataRNG_PG.type}",
                                out bonusId,
                                conn,
                                trans
                           );
                        }
                    }

                    if (transferInOutDataRNG_PG.IsEndRound)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.PG, GameType.PG_RNG, transferInOutDataRNG_PG.ParentBetId, trans);
                    }

                    if (result > 0)
                    {
                        trans.Commit();
                        if (getBonus)
                        {
                            //call Service_TpSlotsBonus_Get to set flag
                            GetTpBonus((int)Tp.PG, transferInOutDataRNG_PG.ParentBetId, gameType, category, out int custId, out bonusId);
                        }
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
                    throw;
                }
            }

            return result;
        }

        #endregion

        #region CQ9

        public int PayoutRNG_CQ9(TpCustInfo tpCustInfo, EndRoundDataRNG_CQ9 endRoundDataRNG_CQ9, ref decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;
            var result = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                int cntSucc = 0;
                foreach (var item in endRoundDataRNG_CQ9.detail)
                {
                    if (item.amount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            item.amount,
                            (int)Tp.CQ9,
                            endRoundDataRNG_CQ9.roundid,
                            gameType,
                            TpCategory.Slots,
                            item.mtcode,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
                            conn,
                            trans
                         );
                    }
                    else
                    {
                        result = 1;
                    }

                    if (result > 0)
                    {
                        var ticketStatus = item.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;

                        var p = new DynamicParameters(new
                        {
                            account = endRoundDataRNG_CQ9.account,
                            gamecode = endRoundDataRNG_CQ9.gamecode,
                            gamehall = endRoundDataRNG_CQ9.gamehall,
                            roundid = endRoundDataRNG_CQ9.roundid,
                            mtcode = item.mtcode,
                            amount = item.amount,
                            validbet = item.validbet,
                            eventUnix = item.eventUnix,
                            createUnix = endRoundDataRNG_CQ9.createUnix,
                            before = afterAmount - (Math.Abs(item.amount)),
                            balance = afterAmount,
                            Actions = "endround",
                            TicketStatus = ticketStatus,
                            TicketType = (int)BettransType.Payout,
                            SettleFlag = 2
                        });

                        int insertRes = conn.Execute("Service_BettransRng_CQ9_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                        if (insertRes > 0)
                        {
                            cntSucc++;
                        }
                    }
                }

                if (cntSucc == endRoundDataRNG_CQ9.detail.Count())
                {
                    trans.Commit();
                }
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
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

        public int RefundRNG_CQ9(TpCustInfo tpCustInfo, RefundDataRNG_CQ9 refundDataRNG_CQ9, decimal amount, string roundid, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            int result = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            if (amount != 0)
            {
                result = PayoutV2(
                    tpCustInfo,
                    Math.Abs(amount),
                    (int)Tp.CQ9,
                    roundid,
                    gameType,
                    TpCategory.Slots,
                    refundDataRNG_CQ9.mtcode,
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
                    var insertRes = conn.Execute(
                          "Service_BettransRng_CQ9_Refund_Set",
                          new
                          {
                              mtcode = refundDataRNG_CQ9.mtcode,
                              before = afterAmount - Math.Abs(amount),
                              balance = afterAmount,
                          },
                          transaction: trans,
                          commandType: CommandType.StoredProcedure
                      );
                    if (insertRes > 0)
                    {
                        trans.Commit();
                    }
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        public int RefundFish_CQ9(TpCustInfo tpCustInfo, RefundDataRNG_CQ9 refundDataRNG_CQ9, out decimal afterAmount)
        {
            int gameType = (int)GameType.CQ9_Fish;

            afterAmount = 0;
            int result = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            if (refundDataRNG_CQ9.amount != 0)
            {
                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    Math.Abs(refundDataRNG_CQ9.amount),
                    (int)Tp.CQ9,
                    TransType.Refunds,
                    $"Refund: {Math.Abs(refundDataRNG_CQ9.amount):#,##0.##}",
                    refundDataRNG_CQ9.mtcode,
                    out afterAmount,
                    conn,
                    trans
                );

                if (result > 0)
                {
                    var insertRes = conn.Execute(
                          "Service_BettransFish_CQ9_Refund_Set",
                          new
                          {
                              mtcode = refundDataRNG_CQ9.mtcode,
                              before = afterAmount - Math.Abs(refundDataRNG_CQ9.amount),
                              balance = afterAmount,
                          },
                          transaction: trans,
                          commandType: CommandType.StoredProcedure
                      );
                    if (insertRes > 0)
                    {
                        trans.Commit();
                    }
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        public int DebitRNG_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount) * -1;

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (bettingDataRNG_CQ9.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        bettingDataRNG_CQ9.roundid,
                        gameType,
                        TpCategory.Slots,
                        bettingDataRNG_CQ9.mtcode,
                        TransType.Undo,
                        null,
                        out afterAmount,
                        "Resettle",
                        out bonusId,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        var p = new DynamicParameters(new
                        {
                            account = bettingDataRNG_CQ9.account,
                            gamecode = bettingDataRNG_CQ9.gamecode,
                            gamehall = bettingDataRNG_CQ9.gamehall,
                            roundid = bettingDataRNG_CQ9.roundid,
                            mtcode = bettingDataRNG_CQ9.mtcode,
                            amount = bettingDataRNG_CQ9.amount,
                            eventUnix = bettingDataRNG_CQ9.eventUnix,
                            before = afterAmount + (Math.Abs(bettingDataRNG_CQ9.amount)),
                            balance = afterAmount,
                            Actions = "debit",
                            TicketStatus = (int)TicketStatus.Lost,
                            TicketType = (int)BettransType.Payout
                        });

                        var insertRes = conn.Execute("Service_BettransRng_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                        if (insertRes > 0)
                        {
                            var settleRes = conn.Execute("Service_BettransRng_CQ9_Settle_Set", new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
                            if (settleRes > 0)
                            {
                                trans.Commit();
                            }
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

        public int DebitFish_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int gameType = (int)GameType.CQ9_Fish;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount) * -1;

            conn.Open();
            using var trans = conn.BeginTransaction();
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
                        TransType.Undo,
                        $"Resettle: {Math.Abs(bettingDataRNG_CQ9.amount):#,##0.##}",
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
                            gamecode = bettingDataRNG_CQ9.gamecode,
                            gamehall = bettingDataRNG_CQ9.gamehall,
                            roundid = bettingDataRNG_CQ9.roundid,
                            mtcode = bettingDataRNG_CQ9.mtcode,
                            amount = bettingDataRNG_CQ9.amount,
                            eventUnix = bettingDataRNG_CQ9.eventUnix,
                            before = afterAmount + (Math.Abs(bettingDataRNG_CQ9.amount)),
                            balance = afterAmount,
                            Actions = "debit",
                            TicketStatus = (int)TicketStatus.Lost,
                            TicketType = (int)BettransType.Payout
                        });

                        var insertRes = conn.Execute("Service_BettransFish_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                        if (insertRes > 0)
                        {
                            var settleRes = conn.Execute("Service_BettransFish_CQ9_Settle_Set", new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
                            if (settleRes > 0)
                            {
                                trans.Commit();
                            }
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

        public int CreditRNG_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount);

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (bettingDataRNG_CQ9.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        bettingDataRNG_CQ9.roundid,
                        gameType,
                        TpCategory.Slots,
                        bettingDataRNG_CQ9.mtcode,
                        TransType.Undo,
                        null,
                        out afterAmount,
                        "Resettle",
                        out bonusId,
                        conn,
                        trans
                    );
                    if (result > 0)
                    {
                        var p = new DynamicParameters(new
                        {
                            account = bettingDataRNG_CQ9.account,
                            gamecode = bettingDataRNG_CQ9.gamecode,
                            gamehall = bettingDataRNG_CQ9.gamehall,
                            roundid = bettingDataRNG_CQ9.roundid,
                            mtcode = bettingDataRNG_CQ9.mtcode,
                            amount = bettingDataRNG_CQ9.amount,
                            eventUnix = bettingDataRNG_CQ9.eventUnix,
                            before = afterAmount - (Math.Abs(bettingDataRNG_CQ9.amount)),
                            balance = afterAmount,
                            Actions = "credit",
                            TicketStatus = (int)TicketStatus.Win,
                            TicketType = (int)BettransType.Payout
                        });

                        var insertRes = conn.Execute("Service_BettransRng_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                        if (insertRes > 0)
                        {
                            var settleRes = conn.Execute("Service_BettransRng_CQ9_Settle_Set", new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
                            if (settleRes > 0)
                            {
                                trans.Commit();
                            }
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

        public int CreditFish_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int gameType = (int)GameType.CQ9_Fish;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount);

            conn.Open();
            using var trans = conn.BeginTransaction();
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
                        TransType.Undo,
                        $"Resettle: {Math.Abs(bettingDataRNG_CQ9.amount):#,##0.##}",
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
                            gamecode = bettingDataRNG_CQ9.gamecode,
                            gamehall = bettingDataRNG_CQ9.gamehall,
                            roundid = bettingDataRNG_CQ9.roundid,
                            mtcode = bettingDataRNG_CQ9.mtcode,
                            amount = bettingDataRNG_CQ9.amount,
                            eventUnix = bettingDataRNG_CQ9.eventUnix,
                            before = afterAmount - (Math.Abs(bettingDataRNG_CQ9.amount)),
                            balance = afterAmount,
                            Actions = "credit",
                            TicketStatus = (int)TicketStatus.Win,
                            TicketType = (int)BettransType.Payout
                        });

                        var insertRes = conn.Execute("Service_BettransFish_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                        if (insertRes > 0)
                        {
                            var settleRes = conn.Execute("Service_BettransFish_CQ9_Settle_Set", new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 }, transaction: trans, commandType: CommandType.StoredProcedure);
                            if (settleRes > 0)
                            {
                                trans.Commit();
                            }
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

        public int RewardRNG_CQ9(TpCustInfo tpCustInfo, PayoffDataRNG_CQ9 payoffDataRNG_CQ9, out decimal afterAmount)
        {
            // third party did not provide gametype use default cq9 slot 
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            var insertRes = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (payoffDataRNG_CQ9.amount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        payoffDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        TransType.Prize,
                        $"Prize: {Math.Abs(payoffDataRNG_CQ9.amount):#,##0.##}",
                        payoffDataRNG_CQ9.mtcode,
                        out afterAmount,
                        conn,
                        trans
                    );

                    var p = new DynamicParameters(new
                    {
                        account = payoffDataRNG_CQ9.account,
                        mtcode = payoffDataRNG_CQ9.mtcode,
                        amount = payoffDataRNG_CQ9.amount,
                        eventUnix = payoffDataRNG_CQ9.eventUnix,
                        remark = payoffDataRNG_CQ9.remark,
                        before = afterAmount - (Math.Abs(payoffDataRNG_CQ9.amount)),
                        balance = afterAmount,
                        Actions = "payoff",
                        TicketStatus = (int)TicketStatus.Related,
                        TicketType = (int)BettransType.Bonus
                    });

                    insertRes = conn.Execute("Service_RewardRNG_CQ9_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }

                if (insertRes > 0)
                {
                    trans.Commit();
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

        public int RollinFish_CQ9(TpCustInfo tpCustInfo, RollinDataFish_CQ9 rollinDataFish_CQ9, int gameType, ref decimal afterAmount)
        {
            var result = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                if (rollinDataFish_CQ9.amount > 0)
                {
                    result = walletService.SetBalance(
                       tpCustInfo.CustId,
                       tpCustInfo.SiteId,
                       tpCustInfo.UserName,
                       rollinDataFish_CQ9.amount,
                       (int)Tp.CQ9,
                       TransType.Payout,
                       $"Payout: {Math.Abs(rollinDataFish_CQ9.amount):#,##0.##}",
                       rollinDataFish_CQ9.mtcode,
                       out afterAmount,
                       conn,
                       trans
                   );
                }

                if (result > 0 || rollinDataFish_CQ9.amount == 0)
                {
                    decimal winlose = rollinDataFish_CQ9.bet - rollinDataFish_CQ9.win;
                    var ticketStatus = (int)TicketStatus.Win;
                    if (winlose < 0)
                    {
                        ticketStatus = (int)TicketStatus.Lost;
                    }
                    else if (winlose == 0)
                    {
                        ticketStatus = (int)TicketStatus.Draw;
                    }

                    var p = new DynamicParameters(new
                    {
                        account = rollinDataFish_CQ9.account,
                        gamehall = rollinDataFish_CQ9.gamehall,
                        gamecode = rollinDataFish_CQ9.gamecode,
                        roundid = rollinDataFish_CQ9.roundid,
                        validbet = rollinDataFish_CQ9.validbet,
                        bet = Math.Abs(rollinDataFish_CQ9.bet) * -1,
                        win = rollinDataFish_CQ9.win,
                        roomfee = rollinDataFish_CQ9.roomfee,
                        amount = rollinDataFish_CQ9.amount,
                        mtcode = rollinDataFish_CQ9.mtcode,
                        rake = rollinDataFish_CQ9.rake,
                        gametype = rollinDataFish_CQ9.gametype,
                        eventUnix = rollinDataFish_CQ9.eventUnix,
                        createUnix = rollinDataFish_CQ9.createUnix,
                        before = afterAmount - (Math.Abs(rollinDataFish_CQ9.amount)),
                        balance = afterAmount,
                        Actions = "rollin",
                        TicketStatus = ticketStatus,
                        TicketType = (int)BettransType.Payout,
                        SettleFlag = (rollinDataFish_CQ9.bet == 0 && rollinDataFish_CQ9.win == 0) ? 1 : 2
                    });

                    result = conn.Execute("Service_BettransFish_CQ9_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }

            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
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

        public IEnumerable<SettleDataRNG_CQ9> GetUnsettledListRNG_CQ9()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataRNG_CQ9>("Service_BettransRng_CQ9_Unsettle_Get_V2", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlagRNG_CQ9(string roundid, int SettleFlag)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = conn.Execute("Service_BettransRng_CQ9_Settle_Set", new { roundid = roundid, SettleFlag = SettleFlag }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            return result;
        }

        public IEnumerable<SettleDataRNG_CQ9> GetUnsettledListFish_CQ9()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataRNG_CQ9>("Service_BettransFish_CQ9_Unsettle_Get_V2", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlagFish_CQ9(string roundid, int SettleFlag)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = conn.Execute("Service_BettransFish_CQ9_Settle_Set", new { roundid = roundid, SettleFlag = SettleFlag }, transaction: trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
            return result;
        }

        public IEnumerable<Record_CQ9> GetRecordRNG_CQ9(string mtcode)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var data = conn.Query<Record_CQ9>("Service_BettransRng_CQ9_Get_V2", new { mtcode = mtcode }, commandType: CommandType.StoredProcedure);
            if (data.Count() > 0)
            {
                data.ElementAt(0).TableType = (int)GameType.CQ9_RNG;
                return data;
            }

            if (data == null || data.Count() == 0)
            {
                data = conn.Query<Record_CQ9>("Service_BettransFish_CQ9_Get_V2", new { mtcode = mtcode }, commandType: CommandType.StoredProcedure);
                if (data.Count() > 0)
                {
                    data.ElementAt(0).TableType = (int)GameType.CQ9_Fish;
                    return data;
                }
            }

            if (data == null || data.Count() == 0)
            {
                data = conn.Query<Record_CQ9>("Service_RewardRNG_CQ9_Get", new { mtcode = mtcode }, commandType: CommandType.StoredProcedure);
            }
            return data;
        }

        #endregion

        #region Kiss918

        public IEnumerable<SettleData_Kiss918> GetUnsettledList_Kiss918()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleData_Kiss918>("Service_TpWagerKiss918_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerKiss918(string uuid)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerKiss918_Settle_Set", new { uuid = uuid }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region Mega

        public SettleData GetSettle_TpReportMega(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData>("Service_TpReportRng_Mega_Settle_Get", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        public int SetSettleFlag_TpReportMega(string TransId)
        {
            return _wagerDbClient.Execute("Service_TpReportRng_Mega_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        #endregion

        #region Iconic

        public int CancelBetRNG_Iconic(TpCustInfo tpCustInfo, BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var p = new DynamicParameters(new
                {
                    transactionId = bettingDataRNG_Iconic.transactionId,
                    amount = bettingDataRNG_Iconic.amount,
                    UpdateUnix = new DateTimeOffset(bettingDataRNG_Iconic.roundUpdatedAt).ToUnixTimeSeconds(),
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = 1
                });

                var category = TpCategory.Slots;
                var gameType = (int)GameType.Iconic_RNG;
                result = conn.Execute("Service_BettransRng_Iconic_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingDataRNG_Iconic.amount != 0)
                    {

                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_Iconic.amount,
                            (int)Tp.Iconic,
                            bettingDataRNG_Iconic.transactionId.ToString(),
                            gameType,
                            category,
                            bettingDataRNG_Iconic.transactionId.ToString(),
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Cancel",
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
                else
                {
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }

            return result;
        }

        public int PayoutRNG_Iconic(TpCustInfo tpCustInfo, BettingDataRNG_Iconic bettingDataRNG_Iconic, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var p = new DynamicParameters(new
                {
                    transactionId = bettingDataRNG_Iconic.transactionId,
                    payout = bettingDataRNG_Iconic.amount,
                    UpdateUnix = new DateTimeOffset(bettingDataRNG_Iconic.roundUpdatedAt).ToUnixTimeSeconds(),
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = bettingDataRNG_Iconic.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                    SettleFlag = 2
                });

                var category = TpCategory.Slots;
                var gameType = (int)GameType.Iconic_RNG;
                result = conn.Execute("Service_BettransRng_Iconic_Payout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingDataRNG_Iconic.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_Iconic.amount,
                            (int)Tp.Iconic,
                            bettingDataRNG_Iconic.transactionId.ToString(),
                            gameType,
                            category,
                            bettingDataRNG_Iconic.transactionId.ToString(),
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
                else
                {
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }

            return result;
        }

        public IEnumerable<SettleDataRng_Iconic> GetUnsettledListRng_Iconic()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataRng_Iconic>("Service_BettransRng_Iconic_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_BettransRng_Iconic(long transactionId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_BettransRng_Iconic_Settle_Set", new { transactionId = transactionId }, commandType: CommandType.StoredProcedure);
        }

        public int CancelBetFish_Iconic(TpCustInfo tpCustInfo, BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var p = new DynamicParameters(new
                {
                    transactionId = bettingDataFish_Iconic.transactionId,
                    amount = bettingDataFish_Iconic.amount,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = 1
                });

                var gameType = (int)GameType.Iconic_Fish;
                result = conn.Execute("Service_BettransFish_Iconic_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataFish_Iconic.amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(bettingDataFish_Iconic.amount),
                            (int)Tp.Iconic,
                            TransType.Refunds,
                            $"Cancel: {Math.Abs(bettingDataFish_Iconic.amount):#,##0.##}",
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
                else
                {
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }

            return result;
        }

        public int PayoutFish_Iconic(TpCustInfo tpCustInfo, BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var p = new DynamicParameters(new
                {
                    transactionId = bettingDataFish_Iconic.transactionId,
                    username = bettingDataFish_Iconic.username,
                    amount = bettingDataFish_Iconic.amount,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = bettingDataFish_Iconic.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                    SettleFlag = 2
                });


                var gameType = (int)GameType.Iconic_Fish;
                result = conn.Execute("Service_BettransFish_Iconic_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataFish_Iconic.amount != 0)
                    {

                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(bettingDataFish_Iconic.amount),
                            (int)Tp.Iconic,
                            TransType.Payout,
                            $"Payout: {Math.Abs(bettingDataFish_Iconic.amount):#,##0.##}",
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

        public IEnumerable<SettleDataFish_Iconic> GetUnsettledListFish_Iconic()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleDataFish_Iconic>("Service_TpWagerFish_Iconic_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerFish_Iconic(string transactionId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerFish_Iconic_Settle_Set", new { transactionId = transactionId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region YL Fishing

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
                result = conn.Execute("Service_BettransFish_YL_Ins", new
                {
                    bettingDataFish_YL.txId,
                    bettingDataFish_YL.userId,
                    gameNumier = bettingDataFish_YL.gameNumber,
                    bettingDataFish_YL.gameId,
                    bettingDataFish_YL.chair,
                    bettingDataFish_YL.roomId,
                    bettingDataFish_YL.roomLevel,
                    bettingDataFish_YL.currency,
                    transTime,
                    bettingDataFish_YL.betAmount,
                    bettingDataFish_YL.payAmount,
                    bettingDataFish_YL.commission,
                    bettingDataFish_YL.profit,
                    bettingDataFish_YL.validBet,
                    SettleFlag = (int)SettlementType.SettledForInvalid
                },
                transaction: trans,
                commandType: CommandType.StoredProcedure
                );

                if (result > 0)
                {
                    if (bettingDataFish_YL.profit != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            bettingDataFish_YL.profit,
                            (int)Tp.YL,
                            TransType.Payout,
                            $"Settled: {Math.Abs(bettingDataFish_YL.profit).ToString("#,##0.##")}",
                            bettingDataFish_YL.txId,
                            out afterAmount,
                            conn,
                            trans,
                            AllowNegative.NotAllow
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
                    throw;
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
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataFish_YL.profit * -1, (int)Tp.YL, TransType.Refunds, $"Void: {bettingDataFish_YL.profit.ToString("#,##0.##")}", bettingDataFish_YL.txId, out afterAmount, conn, trans, AllowNegative.NotAllow);
                }
                else
                {
                    // need exec update bettrans to refund status
                    result = 1;
                }

                if (result > 0)
                {
                    result = conn.Execute("Service_BettransFish_YL_Set", new
                    {
                        refundDataFish_YL.txId,
                        updateTime = updateTime
                    },
                    transaction: trans,
                    commandType: CommandType.StoredProcedure
                    );

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }

                if (result > 0 && bettingDataFish_YL.profit == 0)
                {
                    result = -1;
                }
            }
            else if (bettingDataFish_YL == null)
            {
                var p = new DynamicParameters(new
                {
                    refundDataFish_YL.txId,
                    refundDataFish_YL.userId,
                    gameNumier = refundDataFish_YL.gameNumber,
                    refundDataFish_YL.gameId,
                    updateTime,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                p.Add("out_result", dbType: DbType.Int32, direction: ParameterDirection.Output);
                conn.Execute("Service_BettransFish_YL_CancelBet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var refLC = p.Get<int>("out_result");
                if (refLC == 2)
                {
                    // insert cancel record
                    trans.Commit();
                }
                result = -1;
            }
            else
            {
                result = -1;
            }

            return result;
        }
        public IEnumerable<SettleData> GetUnsettledListFish_YL()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleData>("Service_BettransFish_YL_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlagFish_YL(string TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Service_BettransFish_YL_Settle_Set", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
            return result;
        }

        public TpBetFailureRecord GetTpBetFailureParam_YL(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, BettingDataFish_YL bettingDataFish_YL, TpCheckRecordCode Code, decimal AfterAmount)
        {
            DateTime.TryParse(bettingDataFish_YL.transTime, out DateTime dtTransTime);
            var TransTime = tools.ConvertLocTime(dtTransTime, 8);

            // BLL會經過匯率換算 下列還原為原始資料
            TpBetFailureRecordInfo_YL InfoData = new TpBetFailureRecordInfo_YL
            {
                CurrentBalance = custBalance.Balance / tpCurrency.ExchangeRate,
                BetAmount = bettingDataFish_YL.betAmount / tpCurrency.ExchangeRate,
                PayAmount = bettingDataFish_YL.payAmount / tpCurrency.ExchangeRate,
                Profit = bettingDataFish_YL.profit / tpCurrency.ExchangeRate,
                InsufficientBalance = AfterAmount,
                ExchangeRate = tpCurrency.ExchangeRate,
            };

            return new TpBetFailureRecord
            {
                TransId = bettingDataFish_YL.txId,
                TpId = (int)Tp.YL,
                TpGameType = (int)GameType.YL_Fish,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = TransTime,
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }

        #endregion

        #region PT

        public IEnumerable<SettleData> GetUnsettledListRng_PT()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleData>("Service_TpWagerRng_PT_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerRng_PT(long TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerRng_PT_Settle_Set", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region MG
        public async Task<MGPayoutResult> PayoutRNG_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            bettingDataRNG_MG.TransType = "C";
            bettingDataRNG_MG.TicketType = (int)BettransType.Payout;
            bettingDataRNG_MG.TicketStatus = bettingDataRNG_MG.amount == 0 ? (int)TicketStatus.Draw : (int)TicketStatus.Win;
            bettingDataRNG_MG.SettleFlag = (int)SettleFlag.EndGame;

            if (string.IsNullOrEmpty(bettingDataRNG_MG.betId))
            {
                bettingDataRNG_MG.betId = bettingDataRNG_MG.txnId;
            }

            var p = new DynamicParameters(bettingDataRNG_MG);
            p.Add("extTxnId", dbType: DbType.Int64, direction: ParameterDirection.Output);
            try
            {
                // 檢查該局是否已經結算
                var isCompleted = await conn.QueryFirstOrDefaultAsync<bool>("Service_BettransRng_MG_Check", new { bettingDataRNG_MG.betId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (isCompleted)
                {
                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }

                // 檢查注單是否已經存在
                var bettransRngMg = await conn.QueryFirstOrDefaultAsync<BettransRng_MG>("Service_BettransRng_MG_Get", new { bettingDataRNG_MG.txnId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (bettransRngMg != null)
                {
                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                        ExtTxnId = bettransRngMg.ExtTxnId
                    };
                }

                var result = conn.Execute("Service_BettransRng_MG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var extTxnId = p.Get<long>("extTxnId");

                if (result > 0 && bettingDataRNG_MG.amount > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_MG.amount,
                        (int)Tp.MG,
                        bettingDataRNG_MG.betId,
                        gameType,
                        TpCategory.Slots,
                        bettingDataRNG_MG.txnId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans);
                }

                if (bettingDataRNG_MG.completed)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.MG, GameType.MG_RNG, bettingDataRNG_MG.betId, trans);
                }

                if (result > 0)
                {
                    trans.Commit();
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
                    logger.LogError(mySqlEx, $"PayoutRNG_MG failed: {JsonConvert.SerializeObject(bettingDataRNG_MG)}");
                    throw;
                }
            }
        }

        public async Task<MGCancelResult> CancelBetRNG_MG(TpCustInfo tpCustInfo, CancelDataRNG_MG cancelDataRng_MG, int gameType)
        {
            int bonusId = 0;
            var afterAmount = 0M;

            await using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                var bettransMg = await conn.QueryAsync<BettransRng_MG>("Service_BettransRngByBet_MG_Get_V2",
                    new { cancelDataRng_MG.betId }, transaction: trans, commandType: CommandType.StoredProcedure);
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

                var bet = bettrans.FirstOrDefault();
                // 玩家或金額不符
                if (bet != null && (bet.PlayerId != cancelDataRng_MG.playerId || Math.Abs(bet.Amount) != cancelDataRng_MG.amount))
                {
                    logger.LogError($"player or amount doesn't match:{JsonConvert.SerializeObject(bet)}");
                    throw new MGException(MGErrorCode.TransactionIncompatible, $"{JsonConvert.SerializeObject(cancelDataRng_MG)}");
                }

                //已取消
                if (bettrans.Any(a => a.TxnId == cancelDataRng_MG.txnId && a.TicketType == (int)BettransType.CancelBet))
                {
                    logger.LogError($"betId:{cancelDataRng_MG.betId}, txnId:{cancelDataRng_MG.txnId} is canceled");
                    return new MGCancelResult() { Result = 1 };
                }

                var result = conn.Execute("Service_BettransRng_MG_CancelBet_Set", new { cancelDataRng_MG.betId, cancelDataRng_MG.txnId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (result > 0)
                {
                    if (cancelDataRng_MG.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            cancelDataRng_MG.amount,
                            (int)Tp.MG,
                            cancelDataRng_MG.betId,
                            gameType,
                            TpCategory.Slots,
                            cancelDataRng_MG.txnId,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            $"Refund",
                            out bonusId,
                            conn,
                            trans);
                    }
                }

                if (result > 0)
                {
                    trans.Commit();
                }
                return new MGCancelResult()
                {
                    Result = result,
                    AfterAmount = afterAmount
                };
            }
            catch (Exception e)
            {
                logger.LogError(e, $"CancelBetRNG_MG failed: {JsonConvert.SerializeObject(cancelDataRng_MG)}");
                throw;
            }
        }
        public IEnumerable<SettleData> GetUnsettledList_MG(string spName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleData>(spName, commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_MG(string spName, string transId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute(spName, new { betId = transId }, commandType: CommandType.StoredProcedure);
        }

        public int GetTpGameType_MG(string betId, long creationTime)
        {
            var tpGameType = 0;
            var p = new DynamicParameters(new
            {
                betId = betId
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

            var slot = conn.QueryFirstOrDefault("Service_BettransRngByBet_MG_Get_V2", p, commandType: CommandType.StoredProcedure);
            if (slot != null)
            {
                tpGameType = (int)GameType.MG_RNG;
                return tpGameType;
            }

            long unixTimestamp = tools.GetUnixTimestampByTicks(creationTime);
            var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestamp).DateTime;

            var live = _partitionService.Query<BettingDataRNG_MG>(partitionTime, "Service_BettransLCByBet_MG_Get", p);
            if (live.Any())
            {
                tpGameType = (int)GameType.MG_LiveCasino;
                return tpGameType;
            }

            return tpGameType;
        }

        #endregion

        #region JILI

        public int BetAndPayoutRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, ThirdPartyBLL thirdPartyBLL, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = conn.QueryFirstOrDefault<BettransData_JILI>("Service_BettransRng_JILI_Get", new { round = bettingData_JILI.round }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check != null)
                {
                    return -1;
                }

                var betAmount = bettingData_JILI.betAmount * -1;

                var iTicketStatus = (int)TicketStatus.Win;
                var winlose = betAmount + bettingData_JILI.winloseAmount;

                if (winlose > 0)
                {
                    iTicketStatus = (int)TicketStatus.Win;
                }
                else if (winlose < 0)
                {
                    iTicketStatus = (int)TicketStatus.Lost;
                }
                else if (winlose == 0)
                {
                    iTicketStatus = (int)TicketStatus.Draw;
                }

                // Special Bets
                if (betAmount == 0 && bettingData_JILI.winloseAmount >= 0)
                {
                    iTicketStatus = (int)TicketStatus.FreeBetWin;
                }

                var p = new DynamicParameters(new
                {
                    reqId = bettingData_JILI.reqId,
                    userId = bettingData_JILI.userId,
                    currency = bettingData_JILI.currency,
                    game = bettingData_JILI.game,
                    round = bettingData_JILI.round,
                    wagersTime = bettingData_JILI.wagersTime,
                    betAmount = betAmount,
                    winloseAmount = bettingData_JILI.winloseAmount,
                    isFreeRound = bettingData_JILI.isFreeRound,
                    TransactionID = bettingData_JILI.TransactionID,
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = iTicketStatus,
                    SettleFlag = SettleFlag.EndGame
                });

                var category = TpCategory.Slots;
                var gameType = (int)GameType.JILI_RNG;

                result = conn.Execute("Service_BettransRng_JILI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    int bonusId = 0;
                    if (betAmount != 0)
                    {
                        result = _bettingService.PlaceBetV2(
                            tpCustInfo,
                            betAmount,
                            (int)Tp.JILI,
                            bettingData_JILI.round.ToString(),
                            gameType,
                            bettingData_JILI.round.ToString(),
                            null,
                            category,
                            out afterAmount,
                            ref bonusId,
                            conn,
                            trans
                        );
                    }

                    var transId = (bettingData_JILI.TransactionID != 0) ? bettingData_JILI.TransactionID.ToString() : bettingData_JILI.round.ToString();

                    bool getBonus = false;
                    if (bettingData_JILI.winloseAmount != 0)
                    {
                        if (bonusId > 0)
                        {
                            getBonus = true;
                            result = PayoutWithBonusId(
                                tpCustInfo,
                                bettingData_JILI.winloseAmount,
                                (int)Tp.JILI,
                                transId,
                                gameType,
                                category,
                                bettingData_JILI.round.ToString(),
                                TransType.Payout,
                                out afterAmount,
                                "Payout",
                                bonusId,
                                conn,
                                trans
                            );
                        }
                        else
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                bettingData_JILI.winloseAmount,
                                (int)Tp.JILI,
                                transId,
                                gameType,
                                category,
                                bettingData_JILI.round.ToString(),
                                TransType.Payout,
                                null,
                                out afterAmount,
                                "Payout",
                                out bonusId,
                                conn,
                                trans
                           );
                        }
                    }

                    if (result > 0)
                    {
                        trans.Commit();

                        if (getBonus)
                        {
                            //call Service_TpSlotsBonus_Get to set flag
                            GetTpBonus((int)Tp.JILI, transId, gameType, category, out int custId, out bonusId);
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
                throw mySqlEx;
            }

            return result;
        }

        public int CancelBetRNG_JILI(TpCustInfo tpCustInfo, CancelBetData_JILI cancelBetData_JILI, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = conn.QueryFirstOrDefault<BettransData_JILI>("Service_BettransRng_JILI_Get", new { round = cancelBetData_JILI.round }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check == null)
                {
                    return -2;
                }
                else if (check.TicketStatus == 3)
                {
                    return -1;
                }

                var p = new DynamicParameters(new
                {
                    round = cancelBetData_JILI.round,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = TicketStatus.Refund,
                    SettleFlag = SettleFlag.EndGame
                });

                var category = TpCategory.Slots;
                var gameType = (int)GameType.JILI_RNG;

                result = conn.Execute("Service_BettransRng_JILI_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                var refundAmount = Math.Abs(cancelBetData_JILI.betAmount) + (Math.Abs(cancelBetData_JILI.winloseAmount) * -1);

                if (result > 0)
                {
                    if (refundAmount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            refundAmount,
                            (int)Tp.JILI,
                            cancelBetData_JILI.round.ToString(),
                            gameType,
                            category,
                            cancelBetData_JILI.round.ToString(),
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
                result = -99;
                throw mySqlEx;
            }

            return result;
        }

        public int BetAndPayoutFish_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = conn.QueryFirstOrDefault<BettransData_JILI>("Service_BettransFish_JILI_Get", new { round = bettingData_JILI.round }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check != null)
                {
                    return -1;
                }

                var betAmount = bettingData_JILI.betAmount * -1;
                var iTicketStatus = (int)TicketStatus.Win;
                var winlose = betAmount + bettingData_JILI.winloseAmount;

                if (winlose > 0)
                {
                    iTicketStatus = (int)TicketStatus.Win;
                }
                else if (winlose < 0)
                {
                    iTicketStatus = (int)TicketStatus.Lost;
                }
                else if (winlose == 0)
                {
                    iTicketStatus = (int)TicketStatus.Draw;
                }

                // Special Bets
                if (betAmount == 0 && bettingData_JILI.winloseAmount >= 0)
                {
                    iTicketStatus = (int)TicketStatus.FreeBetWin;
                }

                var p = new DynamicParameters(new
                {
                    reqId = bettingData_JILI.reqId,
                    userId = bettingData_JILI.userId,
                    currency = bettingData_JILI.currency,
                    game = bettingData_JILI.game,
                    round = bettingData_JILI.round,
                    wagersTime = bettingData_JILI.wagersTime,
                    betAmount = betAmount,
                    winloseAmount = bettingData_JILI.winloseAmount,
                    isFreeRound = bettingData_JILI.isFreeRound,
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = iTicketStatus,
                    SettleFlag = SettleFlag.EndGame
                });

                result = conn.Execute("Service_BettransFish_JILI_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    var amount = betAmount + bettingData_JILI.winloseAmount;
                    if (amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            amount,
                            (int)Tp.JILI,
                            TransType.Payout,
                            $"Settled: {amount:#,##0.##}",
                            bettingData_JILI.round.ToString(),
                            out afterAmount,
                            conn,
                            trans
                        );
                    }
                    //if (betAmount != 0)
                    //{
                    //    result = walletService.SetBalance(
                    //        tpCustInfo.CustId,
                    //        tpCustInfo.SiteId,
                    //        tpCustInfo.UserName,
                    //        betAmount,
                    //        (int)Tp.JILI,
                    //        TransType.Wagers,
                    //        $"Stake: {betAmount:#,##0.##}",
                    //        bettingData_JILI.round.ToString(),
                    //        out afterAmount,
                    //        conn,
                    //        trans
                    //    );
                    //}

                    //if (winlose != 0)
                    //{
                    //    result = walletService.SetBalance(
                    //        tpCustInfo.CustId,
                    //        tpCustInfo.SiteId,
                    //        tpCustInfo.UserName,
                    //        Math.Abs(bettingData_JILI.winloseAmount),
                    //        (int)Tp.JILI,
                    //        TransType.Payout,
                    //        $"Payout: {Math.Abs(bettingData_JILI.winloseAmount):#,##0.##}",
                    //        bettingData_JILI.round.ToString(),
                    //        out afterAmount,
                    //        conn,
                    //        trans
                    //    );
                    //}

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
                throw mySqlEx;
            }

            return result;
        }

        public int CancelBetFish_JILI(TpCustInfo tpCustInfo, CancelBetData_JILI cancelBetData_JILI, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = conn.QueryFirstOrDefault<BettransData_JILI>("Service_BettransFish_JILI_Get", new { round = cancelBetData_JILI.round }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (check == null)
                {
                    return -2;
                }
                else if (check.TicketStatus == 3)
                {
                    return -1;
                }

                var p = new DynamicParameters(new
                {
                    round = cancelBetData_JILI.round,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = TicketStatus.Refund,
                    SettleFlag = SettleFlag.EndGame
                });

                result = conn.Execute("Service_BettransFish_JILI_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                var refundAmount = Math.Abs(cancelBetData_JILI.betAmount) + (Math.Abs(cancelBetData_JILI.winloseAmount) * -1);

                if (result > 0)
                {
                    if (refundAmount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            refundAmount,
                            (int)Tp.JILI,
                            TransType.Refunds,
                            $"CancelBet: {refundAmount:#,##0.##}",
                            cancelBetData_JILI.round.ToString(),
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
                else
                {
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = -99;
                throw mySqlEx;
            }

            return result;
        }

        public IEnumerable<SettleData_JILI> GetUnsettledList_JILI(int category)
        {
            string sp = (category == (int)TpCategory.Fish) ? "Service_BettransFish_JILI_Unsettle_Get" : "Service_BettransRng_JILI_Unsettle_Get";
            return _mainDbClient.Query<SettleData_JILI>(sp);
        }

        public int SetSettleFlag_JILI(int category, long TransId, int TicketStatus)
        {
            string sp = (category == (int)TpCategory.Fish) ? "Service_BettransFish_JILI_Settle_Set" : "Service_BettransRng_JILI_Settle_Set_V2";
            return _mainDbClient.Execute(sp, parameters: new DynamicParameters(new { TransId = TransId, TicketStatus = TicketStatus }));
        }

        #endregion

        #region Netent

        public int CancelBetRNG_Netent(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

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

            result = conn.Execute("Service_BettransRng_Netent_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                if (bettingData_EVO.transaction.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettingData_EVO.transaction.amount),
                        (int)Tp.Netent,
                        bettingData_EVO.transaction.refId,
                        (int)GameType.Netent_RNG,
                        TpCategory.Slots,
                        bettingData_EVO.transaction.refId,
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
                    trans.Commit();
                }
            }
            else
            {
                var check = conn.Query<BettransData_EVO>("Service_BettransRng_Netent_Get", new { TransactionRefId = bettingData_EVO.transaction.refId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (!check.Any())
                {
                    // no bet data
                    return -2;
                }
                var subChk = check.Where(x => x.TicketType == 2);
                if (subChk.Any())
                {
                    // had cancel
                    return -1;
                }
            }
            return result;
        }

        public int PayoutRNG_Netent(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = conn.Query<BettransData_EVO>("Service_BettransRng_Netent_Get", new { TransactionRefId = bettingData_EVO.transaction.refId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (!check.Any())
                {
                    // no bet or cancel data
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

                result = conn.Execute("Service_BettransRng_Netent_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingData_EVO.transaction.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_EVO.transaction.amount),
                            (int)Tp.Netent,
                            bettingData_EVO.transaction.refId,
                            (int)GameType.Netent_RNG,
                            TpCategory.Slots,
                            bettingData_EVO.transaction.refId,
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
                    throw;
                }
            }

            return result;
        }

        public int PromoPayoutRNG_Netent(TpCustInfo tpCustInfo, Reward_EVO data, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                            (int)Tp.Netent,
                            data.PromoTransactionId,
                            (int)GameType.Netent_RNG,
                            TpCategory.Slots,
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
                        trans.Commit();
                    }
                }
                trans.Rollback();
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

        public IEnumerable<SettleData> GetUnsettledList_Netent()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransRng_Netent_Unsettle_Get");
        }

        public int SetSettleFlag_Netent(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransRng_Netent_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        #endregion

        #region RedTiger

        public int CancelBetRNG_RedTiger(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();


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

            result = conn.Execute("Service_BettransRng_RedTiger_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                if (bettingData_EVO.transaction.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettingData_EVO.transaction.amount),
                        (int)Tp.RedTiger,
                        bettingData_EVO.transaction.refId,
                        (int)GameType.RedTiger_RNG,
                        TpCategory.Slots,
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
                    trans.Commit();
                }
            }
            else
            {
                var check = conn.Query<BettransData_EVO>("Service_BettransRng_RedTiger_Get", new { TransactionRefId = bettingData_EVO.transaction.refId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (!check.Any())
                {
                    // no bet data
                    return -2;
                }
                var subChk = check.Where(x => x.TicketType == 2);
                if (subChk.Any())
                {
                    // had cancel
                    return -1;
                }
            }
            return result;
        }

        public int PayoutRNG_RedTiger(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = conn.Query<BettransData_EVO>("Service_BettransRng_RedTiger_Get", new { TransactionRefId = bettingData_EVO.transaction.refId }, transaction: trans, commandType: CommandType.StoredProcedure);
                if (!check.Any())
                {
                    // no bet or cancel data
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

                result = conn.Execute("Service_BettransRng_RedTiger_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingData_EVO.transaction.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_EVO.transaction.amount),
                            (int)Tp.RedTiger,
                            bettingData_EVO.transaction.refId,
                            (int)GameType.RedTiger_RNG,
                            TpCategory.Slots,
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
                    throw;
                }
            }

            return result;
        }

        public int PromoPayoutRNG_RedTiger(TpCustInfo tpCustInfo, Reward_EVO data, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                            (int)Tp.RedTiger,
                            data.PromoTransactionId,
                            (int)GameType.RedTiger_RNG,
                            TpCategory.Slots,
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
                    throw;
                }
            }
            return result;
        }

        public IEnumerable<SettleData> GetUnsettledList_RedTiger()
        {
            return _mainDbClient.Query<SettleData>("Service_BettransRng_RedTiger_Unsettle_Get");
        }

        public int SetSettleFlag_RedTiger(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransRng_RedTiger_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        #endregion

        #region AP
        public int CancelBetRNG_AP(TpCustInfo tpCustInfo, CancelBetDataRNG_AP cancelBetDataRNG_AP, out decimal afterAmount)
        {
            var result = 0;
            int bonusId = 0;
            //int gameType = (int)GameType.AP_RNG;
            CultureInfo culture = CultureInfo.InvariantCulture;
            DateTime partitionTime = DateTime.Parse(cancelBetDataRNG_AP.timestamp, culture, DateTimeStyles.None);
            afterAmount = 0;


            var bettransAP = _partitionService.Query<BettransRNG_AP>(partitionTime, "Service_BettransRng_AP_Check", cancelBetDataRNG_AP)?.FirstOrDefault();
            if (bettransAP == null)
            {
                return -2;
            }
            else if (bettransAP.TicketStatus != 0)
            {
                return -1;
            }
            var para = new DynamicParameters(new
            {
                SQ = bettransAP.SQ
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            result = _partitionService.Execute(partitionTime, "Service_BettransRng_AP_CancelBet_Set", para, trans);


            if (result > 0)
            {
                if (cancelBetDataRNG_AP.totalstake != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(cancelBetDataRNG_AP.totalstake),
                        (int)Tp.AP,
                        cancelBetDataRNG_AP.gameroundid,
                        (int)GameType.AP_RNG,
                        TpCategory.Slots,
                        cancelBetDataRNG_AP.seq,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "Refund",
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

            return result;
        }

        public int SettledRNG_AP(TpCustInfo tpCustInfo, BettingDataRNG_AP bettingDataRNG_AP, out decimal afterAmount)
        {
            var bonusId = 0;
            var result = 0;
            afterAmount = 0;
            try
            {
                CultureInfo culture = CultureInfo.InvariantCulture;
                DateTime partitionTime = DateTime.Parse(bettingDataRNG_AP.timestamp, culture, DateTimeStyles.None);
                long unixsettletime = string.IsNullOrWhiteSpace(bettingDataRNG_AP.settletime) ? 0 : ((DateTimeOffset)DateTime.Parse(bettingDataRNG_AP.settletime, culture, DateTimeStyles.None)).ToUnixTimeSeconds();
                long unixjpsettletime = string.IsNullOrWhiteSpace(bettingDataRNG_AP.jpsettletime) ? 0 : ((DateTimeOffset)DateTime.Parse(bettingDataRNG_AP.jpsettletime, culture, DateTimeStyles.None)).ToUnixTimeSeconds();
                long unixtimestamp = ((DateTimeOffset)DateTime.Parse(bettingDataRNG_AP.timestamp, culture, DateTimeStyles.None)).ToUnixTimeSeconds();
                var check = _partitionService.Query<BettransRNG_AP>(partitionTime, "Service_BettransRng_AP_Get", new { bettingDataRNG_AP.seq, bettingDataRNG_AP.sitecode, bettingDataRNG_AP.gamecode, bettingDataRNG_AP.gameroundid });
                if (check.Any())
                {
                    return -1;//Duplicated
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                var win = bettingDataRNG_AP.jpwin > 0 ? bettingDataRNG_AP.jpwin : bettingDataRNG_AP.totalwin;
                bettingDataRNG_AP.TicketStatus = win > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
                bettingDataRNG_AP.TicketType = (int)BettransType.EndRound;

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
                    bettime = 0,
                    settletime = unixsettletime,
                    jpsettletime = unixjpsettletime,
                    timestamp = unixtimestamp,
                    playerid = bettingDataRNG_AP.playerid,
                    hitjackpot = bettingDataRNG_AP.hitjackpot,
                    ip = bettingDataRNG_AP.ip,
                    TicketType = bettingDataRNG_AP.TicketType,
                    TicketStatus = bettingDataRNG_AP.TicketStatus

                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_AP_Ins", p, trans);

                if (result > 0 && (bettingDataRNG_AP.totalwin > 0 || bettingDataRNG_AP.jpwin > 0))
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_AP.jpwin > 0 ? bettingDataRNG_AP.jpwin : bettingDataRNG_AP.totalwin,
                        (int)Tp.AP,
                        bettingDataRNG_AP.gameroundid,
                        (int)GameType.AP_RNG,
                        TpCategory.Slots,
                        bettingDataRNG_AP.seq,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans);
                }

                if (result > 0)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.AP, GameType.AP_RNG, bettingDataRNG_AP.gameroundid, trans, partitionTime);
                }

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }

            return result;
        }
        #endregion

        #region HB

        public int PayoutRNG_HB(TpCustInfo tpCustInfo, PayoutRngDto_HB payoutRngDto, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            afterAmount = 0m;
            try
            {
                var partitionTime = payoutRngDto.dtevent;
                var check = _partitionService.Query<BettransData_HB>(partitionTime, "Service_BettransRng_HB_Check", payoutRngDto);
                if (check.Any())
                {
                    return 1;
                }
                payoutRngDto.TicketType = (int)BettransType.Payout;
                payoutRngDto.TicketStatus = (int)TicketStatus.Win;
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_HB_Ins", payoutRngDto, trans);
                var transType = payoutRngDto.isbonus ? TransType.Prize : TransType.Payout;

                var roundId = payoutRngDto.friendlygameinstanceid.ToString();
                if (result > 0 && payoutRngDto.amount != 0)
                {
                    if (bonusId > 0)
                    {
                        result = PayoutWithBonusId(
                            tpCustInfo: tpCustInfo,
                            amount: payoutRngDto.amount,
                            tpId: (int)Tp.HB,
                            tpTransId: roundId,
                            gameType: (int)GameType.HB_RNG,
                            tpCategory: TpCategory.Slots,
                            refId: payoutRngDto.transferid,
                            TransType.Payout,
                            out afterAmount,
                            "Payout",
                            bonusId,
                            conn,
                            trans);
                    }
                    else
                    {
                        result = PayoutV2(
                            tpCustInfo: tpCustInfo,
                            amount: payoutRngDto.amount,
                            tpId: (int)Tp.HB,
                            tpTransId: roundId,
                            gameType: (int)GameType.HB_RNG,
                            tpCategory: TpCategory.Slots,
                            refId: payoutRngDto.transferid,
                            transType,
                            betData: null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
                            conn,
                            trans);
                    }
                }

                if (result > 0 && payoutRngDto.gamestatemode == 2)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.HB, GameType.HB_RNG, roundId, trans, partitionTime);
                }

                return result;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return -1;
            }
        }

        public void UpdateTpBonusFlag_HB(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId)
        {
            GetTpBonus(tpId, tpTransId, gameType, tpCategory, out CustId, out BonusId);
        }

        public TransactionResult_HB CancelBetRNG_HB(TpCustInfo tpCustInfo, CancelBetRngDto_HB cancelBetRngDto, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            var transactionResult = new TransactionResult_HB();
            var partitionTime = cancelBetRngDto.dtevent;
            var query = new
            {
                transferid = cancelBetRngDto.BetId
            };
            var check = _partitionService.Query<BettransData_HB>(partitionTime, "Service_BettransRng_HB_Check", query)
                ?.FirstOrDefault();
            // 此筆下注不存在
            if (check == null)
            {
                transactionResult.result = -2;
                return transactionResult;
            }
            // 已退過款
            else if (check.TicketStatus == (int)TicketStatus.Refund)
            {
                transactionResult.result = 1;
                return transactionResult;
            }

            var set = new DynamicParameters(new
            {
                SQ = check.SQ
            });

            result = _partitionService.Execute(partitionTime, "Service_BettransRng_HB_CancelBet_Set", set, trans);

            var afterAmount = 0m;
            var bonusId = 0;
            if (result > 0)
            {
                if (cancelBetRngDto.Amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        cancelBetRngDto.Amount,
                        (int)Tp.HB,
                        cancelBetRngDto.BetId,
                        (int)GameType.HB_RNG,
                        TpCategory.Slots,
                        cancelBetRngDto.TransferId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "CancelBet",
                        out bonusId,
                        conn,
                        trans
                    );
                }
            }

            return new TransactionResult_HB()
            {
                result = result,
                afterAmount = afterAmount,
                BonusId = bonusId
            };
        }
        #endregion

        #region AMB Slot

        public bool Payout_AMB(DateTime partitionTime, BettingDataRNG_AMB data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_AMB_Ins", data, trans);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool Cancel_AMB(DateTime partitionTime, int sq, MySqlTransaction trans)
        {
            var query = new { SQ = sq, TicketType = BettransType.CancelBet, TicketStatus = TicketStatus.Refund };
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_AMB_Set", query, trans);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool InsertUnsettle_AMB(DateTime partitionTime, string transactionId, MySqlTransaction trans)
        {
            var result = _tpUnsettleService.InsertTpUnsettle(Tp.AMB, GameType.AMB_RNG, transactionId, trans, partitionTime);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool SetBalance_AMB(TpCustInfo tpCustInfo, BettingDataRNG_AMB data,
            TransType transType, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {

            var result = PayoutV2(
                tpCustInfo,
                data.amount,
                (int)Tp.AMB,
                data.roundId,
                (int)GameType.AMB_RNG,
                TpCategory.Slots,
                data.transactionId,
                transType,
                null,
                out afterAmount,
                memo,
                out bonusId,
                conn,
                trans
            );


            if (result == 0)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
