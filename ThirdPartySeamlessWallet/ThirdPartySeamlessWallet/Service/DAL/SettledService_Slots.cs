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
    public partial class SettledService
    {
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
                        result = PayoutV2(tpCustInfo, bettingDataRNG_Joker.amount, (int)Tp.Joker, bettingDataRNG_Joker.roundid, gameType, TpCategory.Slots, bettingDataRNG_Joker.id, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.Joker, bettingDataRNG_Joker.roundid, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        decimal Stake = p.Get<decimal>("Stake");
                        decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp.Joker,
                            Stake = Stake,
                            ValidBet = Stake,
                            WinlostAmount = WinLostAmount,
                            TransId = bettingDataRNG_Joker.roundid,
                            GameType = GameType.Joker_RNG,
                            TpUserName = bettingDataRNG_Joker.username,
                            SettledTime = bettingDataRNG_Joker.timestamp / 1000,
                            SettlementType = GetSettlementType(GameType.Joker_RNG, bettingDataRNG_Joker.gamecode),
                            BonusId = bonusId
                        };

                        result = SettleTicket(tpCustInfo, betRecordInfo, bettingDataRNG_Joker.amount, out _, conn, trans, false/*Don't need Payout*/);

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
        public int SettledRNG_YGG(TpCustInfo tpCustInfo, SettledDataRNG_YGG settledDataRNG_YGG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.YGG_RNG;

            afterAmount = 0;
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            settledDataRNG_YGG.TicketStatus = settledDataRNG_YGG.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(settledDataRNG_YGG);
            p.Add("type", (int)BettransType.Payout);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = conn.Execute("Service_BettransRng_YGG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (settledDataRNG_YGG.amount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, settledDataRNG_YGG.amount, (int)Tp.YGG, TransType.Settled, $"Payout: {Math.Abs(settledDataRNG_YGG.amount).ToString("#,##0.##")}", settledDataRNG_YGG.subreference, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, settledDataRNG_YGG.amount, (int)Tp.YGG, settledDataRNG_YGG.reference, gameType, TpCategory.Slots, settledDataRNG_YGG.subreference, TransType.Payout, null, out afterAmount, "Payout", out bonusId, conn, trans);
                    }
                    else if (settledDataRNG_YGG.bonusprize == 0)
                    {
                        GetTpBonus((int)Tp.YGG, settledDataRNG_YGG.reference, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (settledDataRNG_YGG.bonusprize > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, settledDataRNG_YGG.bonusprize, (int)Tp.YGG, TransType.Settled, $"Bonus: {Math.Abs(settledDataRNG_YGG.bonusprize).ToString("#,##0.##")}", settledDataRNG_YGG.subreference, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, settledDataRNG_YGG.bonusprize, (int)Tp.YGG, settledDataRNG_YGG.reference, gameType, TpCategory.Slots, settledDataRNG_YGG.subreference, TransType.Prize, null, out afterAmount, "Bonus", out bonusId, conn, trans);
                    }

                    if (result > 0)
                    {
                        decimal Stake = p.Get<decimal>("Stake");
                        decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp.YGG,
                            Stake = Stake,
                            ValidBet = Stake,
                            WinlostAmount = WinLostAmount,
                            TransId = settledDataRNG_YGG.reference,
                            GameType = GameType.YGG_RNG,
                            TpUserName = settledDataRNG_YGG.playerid,
                            SettledTime = 0,
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
                    result = PayoutV2(tpCustInfo, WinLostAmount, (int)Tp.YGG, bettingDataRNG_YGG.reference, gameType, TpCategory.Slots, bettingDataRNG_YGG.subreference, TransType.Refunds, null, out afterAmount, "Refund", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.YGG,
                        Stake = Stake,
                        ValidBet = 0,
                        WinlostAmount = WinLostAmount,
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
                    //category = TpCategory.LiveCasino;
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
        public IEnumerable<BettingDataRNG_PP> GetUnsettledListRNG_PP(int gameType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            if (gameType == (int)GameType.PP_RNG)
            {
                return conn.Query<BettingDataRNG_PP>("Service_UnsettledListRng_PP_Get", commandType: CommandType.StoredProcedure);
            }
            else
            {
                return conn.Query<BettingDataRNG_PP>("Service_BettransLC_PP_Unsettle_Get", commandType: CommandType.StoredProcedure);
            }
        }
        public int SettledRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            using var connPre = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            connPre.Execute("Service_SettledRng_PPV2_Set", new { bettingDataRNG_PP.roundId }, commandType: CommandType.StoredProcedure);

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_PP.roundId });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("Timestamp", dbType: DbType.Int64, direction: ParameterDirection.Output);
            result = conn.Execute("Service_SettledRng_PP_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal stake = p.Get<decimal>("Stake");
                decimal winLostAmount = p.Get<decimal>("WinLostAmount");
                long timestamp = p.Get<long>("Timestamp");

                if (result > 0)
                {
                    GameType gameType = GameType.PP_RNG;

                    if (bettingDataRNG_PP.gameId.Length == 3 && int.TryParse(bettingDataRNG_PP.gameId, out int tempGameId))
                    {
                        gameType = GameType.PP_LiveCasino;
                    }

                    int bonusId;

                    GetTpBonus((int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), (int)gameType, TpCategory.Slots, out int custId, out bonusId);
                    decimal validBet = stake;

                    if (gameType == GameType.PP_LiveCasino)
                    {
                        if (winLostAmount >= 0 && winLostAmount < stake)
                        {
                            validBet = winLostAmount;
                        }
                        else if (winLostAmount < 0)
                        {
                            validBet = Math.Abs(winLostAmount);
                        }
                    }

                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.PP,
                        Stake = stake,
                        ValidBet = validBet,
                        WinlostAmount = winLostAmount,
                        TransId = bettingDataRNG_PP.roundId.ToString(),
                        GameType = gameType,
                        TpUserName = bettingDataRNG_PP.userId,
                        SettledTime = timestamp / 1000,
                        SettlementType = GetSettlementType(gameType, bettingDataRNG_PP.gameId),
                        TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                        BonusId = bonusId
                    };

                    if (betRecordInfo.WinlostAmount == 0)
                        betRecordInfo.TicketStatus = TicketStatus.Draw;

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
        public int SettledLC_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, out decimal afterAmount)
        {
            using var connPre = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            connPre.Execute("Service_BettransLC_PP_Settle_Pre_Set", new { bettingDataRNG_PP.roundId }, commandType: CommandType.StoredProcedure);

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_PP.roundId });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("Timestamp", dbType: DbType.Int64, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransLC_PP_Settle_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal stake = p.Get<decimal>("Stake");
                decimal winLostAmount = p.Get<decimal>("WinLostAmount");
                long timestamp = p.Get<long>("Timestamp");

                if (result > 0)
                {
                    GameType gameType = GameType.PP_LiveCasino;

                    int bonusId;

                    GetTpBonus((int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), (int)gameType, TpCategory.Slots, out int custId, out bonusId);
                    decimal validBet = stake;

                    if (winLostAmount >= 0 && winLostAmount < stake)
                    {
                        validBet = winLostAmount;
                    }
                    else if (winLostAmount < 0)
                    {
                        validBet = Math.Abs(winLostAmount);
                    }

                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.PP,
                        Stake = stake,
                        ValidBet = validBet,
                        WinlostAmount = winLostAmount,
                        TransId = bettingDataRNG_PP.roundId.ToString(),
                        GameType = gameType,
                        TpUserName = bettingDataRNG_PP.userId,
                        SettledTime = timestamp / 1000,
                        SettlementType = GetSettlementType(gameType, bettingDataRNG_PP.gameId),
                        TicketStatus = winLostAmount > 0 ? TicketStatus.Win : TicketStatus.Lost,
                        BonusId = bonusId
                    };

                    if (betRecordInfo.WinlostAmount == 0)
                        betRecordInfo.TicketStatus = TicketStatus.Draw;

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
        public int CancelBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            //GameType gameType = GameType.PP_RNG;
            int bonusId = 0;

            //if (bettingDataRNG_PP.gameId.Length == 3 && int.TryParse(bettingDataRNG_PP.gameId, out int tempGameId))
            //{
            //    gameType = GameType.PP_LiveCasino;
            //}

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
                //category = TpCategory.LiveCasino;
                result = conn.Execute("Service_BettransLC_PP_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }


            if (result > 0)
            {
                decimal cancelStake = p.Get<decimal>("CancelStake");

                if (cancelStake != bettingDataRNG_PP.amount)
                {
                    throw new Exception($"Cancel amount {bettingDataRNG_PP.amount.ToString("#,##0.##")} it not equal {cancelStake.ToString("#,##0.##")}");
                }

                if (cancelStake != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, cancelStake, (int)Tp.PP, TransType.Refunds, $"Refund: {cancelStake.ToString("#,##0.##")}", bettingDataRNG_PP.reference, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, cancelStake, (int)Tp.PP, bettingDataRNG_PP.roundId.ToString(), (int)gameType, category, bettingDataRNG_PP.reference, TransType.Refunds, null, out afterAmount, "Refund", out bonusId, conn, trans);
                }

                if (result > 0)
                {
                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.PP,
                        Stake = cancelStake,
                        ValidBet = 0,
                        WinlostAmount = 0,
                        TransId = bettingDataRNG_PP.roundId.ToString(),
                        GameType = (GameType)gameType,
                        TpUserName = bettingDataRNG_PP.userId,
                        SettledTime = bettingDataRNG_PP.timestamp / 1000,
                        SettlementType = SettlementType.SettledForInvalid,
                        TicketStatus = TicketStatus.Refund,
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

                bool cash = false;
                var category = TpCategory.Slots;
                if (gameType == (int)GameType.SG_RNG)
                {
                    result = conn.Execute("Service_BettransRng_SG_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    cash = true;
                    category = TpCategory.Fish;
                    result = conn.Execute("Service_BettransFish_SG_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }

                if (result > 0)
                {
                    decimal cancelStake = p.Get<decimal>("CancelStake");
                    int bonusId = 0;
                    if (cancelStake != bettingDataRNG_SG.amount)
                    {
                        throw new Exception($"SG Cancel amount {bettingDataRNG_SG.amount.ToString("#,##0.##")} it not equal {cancelStake.ToString("#,##0.##")}");
                    }

                    if (cancelStake != 0)
                    {
                        if (cash == false)
                        {
                            //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, cancelStake, (int)Tp.SG, TransType.Refunds, $"Refund: {cancelStake.ToString("#,##0.##")}", bettingDataRNG_SG.referenceId, out afterAmount, conn, trans);
                            result = PayoutV2(
                                tpCustInfo,
                                bettingDataRNG_SG.amount,
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
                        else
                        {
                            result = walletService.SetBalance(
                                tpCustInfo.CustId,
                                tpCustInfo.SiteId,
                                tpCustInfo.UserName,
                                Math.Abs(bettingDataRNG_SG.amount),
                                gameType,
                                TransType.Refunds,
                                $"Cancel: {Math.Abs(bettingDataRNG_SG.amount):#,##0.##}",
                                bettingDataRNG_SG.referenceId,
                                out afterAmount,
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
                    //    );
                    //}

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
            catch (MySqlException MyEx)
            {
                log2File.Error($"{MyEx}", "CancelBetRNG_SG");
                throw MyEx;
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
                                gameType,
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

        public IEnumerable<SettleData_SG> GetUnsettledList_SG(int gameType)
        {
            if (gameType == (int)GameType.SG_RNG)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                return conn.Query<SettleData_SG>("Service_BettransRng_SG_Unsettle_Get", commandType: CommandType.StoredProcedure);
            }
            else
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
                return conn.Query<SettleData_SG>("Service_TpWagerSG_Unsettle_Get", commandType: CommandType.StoredProcedure);
            }
        }

        public int SetSettleFlag_SG(int gameType, string referenceId)
        {
            var result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            if (gameType == (int)GameType.SG_RNG)
            {
                result = conn.Execute("Service_BettransRng_SG_Settle_Set", new { transferIdList = referenceId }, commandType: CommandType.StoredProcedure);
            }
            else
            {
                result = conn.Execute("Service_BettransFish_SG_Settle_Set", new { referenceId = referenceId }, commandType: CommandType.StoredProcedure);
            }

            return result;
        }

        public int SettledFish_SG(TpCustInfo tpCustInfo, SettleData_SG settleData_SG, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

            BetRecordInfo betRecordInfo = new BetRecordInfo
            {
                TpId = (int)Tp.SG,
                Stake = settleData_SG.Stake,
                ValidBet = settleData_SG.Stake,
                WinlostAmount = settleData_SG.WinLostAmount,
                TransId = settleData_SG.referenceId,
                GameType = GameType.SG_Fish,
                TpUserName = settleData_SG.Account,
                SettledTime = settleData_SG.SettleTime,
                SettlementType = SettlementType.SettledForValid
            };

            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = SettleTicket(
                tpCustInfo,
                betRecordInfo,
                0,
                out _,
                conn,
                trans,
                false/*Don't need Payout*/
            );

            if (result > 0)
            {
                trans.Commit();
            }

            using var conn_wag = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            conn_wag.Execute("Service_TpWagerSG_Settle_Set", new { ticketId = settleData_SG.ticketId }, commandType: CommandType.StoredProcedure);

            using var conn_sec = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn_sec.Execute("Service_BettransFish_SG_Settle_Set", new { referenceId = settleData_SG.referenceId }, commandType: CommandType.StoredProcedure);

            return result;
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
            //p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            //p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransRng_PGV2_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataRNG_PG.TransferAmount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, TransType.Settled, $"{bettingDataRNG_PG.type}: {Math.Abs(bettingDataRNG_PG.TransferAmount).ToString("#,##0.##")}", bettingDataRNG_PG.TransactionId, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, TpCategory.Slots, bettingDataRNG_PG.TransactionId, TransType.Payout, null, out afterAmount, $"{bettingDataRNG_PG.type}", out bonusId, conn, trans);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    //if (result > 0)
                    //{
                    //    if (bettingDataRNG_PG.IsEndRound)
                    //    {
                    //        decimal Stake = p.Get<decimal>("Stake");
                    //        decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                    //        BetRecordInfo betRecordInfo = new BetRecordInfo
                    //        {
                    //            TpId = (int)Tp.PG,
                    //            Stake = Stake,
                    //            ValidBet = Stake,
                    //            WinlostAmount = WinLostAmount,
                    //            TransId = bettingDataRNG_PG.ParentBetId,
                    //            GameType = GameType.PG_RNG,
                    //            TpUserName = bettingDataRNG_PG.PlayerName,
                    //            SettledTime = bettingDataRNG_PG.UpdatedTime,
                    //            SettlementType = GetSettlementType(GameType.PG_RNG, bettingDataRNG_PG.GameId.ToString()),
                    //            BonusId = bonusId
                    //        };

                    //        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);
                    //    }

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                    //}
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
        public IEnumerable<SettleData_PG> GetUnsettledList_PG()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleData_PG>("Service_UnsettledListRng_PG_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlag_PG(string TransactionIdList)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = conn.Execute("Service_BettransRng_PG_Settle_Set", new { TransactionIdList = TransactionIdList }, commandType: CommandType.StoredProcedure);
            return result;
        }

        #endregion

        private int PayoutV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, string betData, out decimal afterAmount, string memoHead, out int bonusId, MySqlConnection conn, MySqlTransaction trans, AllowNegative allowNegative = AllowNegative.NotAllow)
        {
            afterAmount = 0;
            bonusId = 0;
            int result = 0;
            bool bHasBonusWallet = true;
            if (bHasBonusWallet)
            {
                result = GetTpBonus(tpId, tpTransId, gameType, tpCategory, out int custId, out bonusId);

                if (bonusId > 0 || transType == TransType.Prize)
                {
                    if (bonusId > 0 && custId != tpCustInfo.CustId)
                    {
                        return 0;
                    }

                    result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out decimal afterBonus, ref bonusId, conn, trans);

                    //var bonusMinBet = thirdPartyBLL.GetBonusMinBet(gameType, tpCustInfo.CurrencyId);

                    if ((transType == TransType.Prize && bonusId == 0) || afterBonus < 0 || bonusId == -1)
                    {
                        result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, afterBonus, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                    }
                    else
                    {
                        afterAmount = afterBonus + walletService.GetBalance(tpCustInfo.CustId);
                    }
                }
                else
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
                }
            }
            else
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans, allowNegative);
            }

            return result;
        }
        private int GetTpBonus(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId)
        {
            CustId = 0;
            BonusId = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { TpId = tpId, TransId = tpTransId, GameType = gameType });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute($"Service_Tp{tpCategory}Bonus_Get", p, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                CustId = p.Get<int>("CustId");
                BonusId = p.Get<int>("BonusId");
            }

            return result;
        }

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

        public int RewardRNG_CQ9(TpCustInfo tpCustInfo, PayoffDataRNG_CQ9 payoffDataRNG_CQ9, out decimal afterAmount)
        {
            int bonusId = 0;
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
                    result = PayoutV2(
                        tpCustInfo,
                        payoffDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        payoffDataRNG_CQ9.mtcode,
                        gameType,
                        TpCategory.Slots,
                        payoffDataRNG_CQ9.mtcode,
                        TransType.Prize,
                        null,
                        out afterAmount,
                        "Payout",
                        out bonusId,
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

        public IEnumerable<SettleDataRNG_CQ9> GetUnsettledListRNG_CQ9()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleDataRNG_CQ9>("Service_BettransRng_CQ9_Unsettle_Get", commandType: CommandType.StoredProcedure);
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

        public IEnumerable<Record_CQ9> GetRecordRNG_CQ9(string mtcode)
        {
            string sp = "Service_BettransRng_CQ9_Get";
            string[] sType = mtcode.Split("-");
            if (sType.Count() > 1 && sType[1] == "payoff")
            {
                sp = "Service_RewardRNG_CQ9_Get";
            }
            string mtcodeType = null;

            if (sType.Count() > 1)
                mtcodeType = $"{sType[0]}-{sType[1]}";

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<Record_CQ9>(sp, new { mtcode = mtcode, mtcodeType }, commandType: CommandType.StoredProcedure);
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

        public IEnumerable<SettleData_Mega> GetUnsettledList_Mega()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleData_Mega>("Service_TpWagerMega_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerMega(long TransId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerMega_Settle_Set", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
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
                            gameType,
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
                            gameType,
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

    }
}
