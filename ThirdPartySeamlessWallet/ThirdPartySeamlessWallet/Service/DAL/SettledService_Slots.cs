using System.Data;
using System.Globalization;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(settledDataRNG_GPI);
            p.Add("jackpot_contribution", 0);
            p.Add("type", (int)BettransType.Payout);

            try
            {
                result = _mainDbClient.Execute("Service_BettransRNG_GPI_Ins", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public IEnumerable<SettleDataRNG_GPI> GetUnsettledListRNG_GPI(string transId)
        {
            var p = new DynamicParameters(new
            {
                _TransId = transId
            });

            return _mainDbClient.Query<SettleDataRNG_GPI>("Service_BettransRNG_GPI_Unsettle_Get_V2", commandType: CommandType.StoredProcedure, p);
        }

        public int SetSettleFlagRNG_GPI(string roundId, string custId, int SettleFlag)
        {
            var p = new DynamicParameters(new
            {
                round_id = roundId,
                cust_id = custId,
                SettleFlag = SettleFlag
            });

            var result = _mainDbClient.Execute("Service_BettransRNG_GPI_Settle_Set", commandType: CommandType.StoredProcedure, p);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            bettingDataRNG_Joker.TicketStatus = bettingDataRNG_Joker.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(bettingDataRNG_Joker);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = _mainDbClient.Execute("Service_BettransRng_Joker_Ins", trans, p);

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
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(cancelBetDataRNG_Joker);
            p.Add("CancelStatus", dbType: DbType.Int16, direction: ParameterDirection.Output);
            p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("HasSettled", dbType: DbType.Int16, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_CancelBetRng_Joker_Set", trans, p);

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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                using var trans = conn.BeginTransaction();

                bettingDataFish_Joker.TicketType = (int)BettransType.Payout;
                result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", bettingDataFish_Joker, trans);

                if (result > 0 && bettingDataFish_Joker.amount != 0)
                {
                    var refId = bettingDataFish_Joker.id.Length > 45 ? bettingDataFish_Joker.id.Substring(0, 45) : bettingDataFish_Joker.id;
                    result = PayoutV2(
                         tpCustInfo: tpCustInfo,
                         amount: bettingDataFish_Joker.amount,
                         tpId: (int)Tp.Joker,
                         tpTransId: string.IsNullOrWhiteSpace(bettingDataFish_Joker.roundid) ? refId : bettingDataFish_Joker.roundid,
                         gameType: (int)GameType.Joker_Fish,
                         tpCategory: TpCategory.Fish,
                         refId: refId,
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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                using var trans = conn.BeginTransaction();

                bettingDataFish_Joker.TicketType = (int)BettransType.EndRound;
                result = _partitionService.Execute(partitionTime, "Service_BettransFish_Joker_Ins", bettingDataFish_Joker, trans);

                if (result > 0)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.Joker, GameType.Joker_Fish, bettingDataFish_Joker.roundid, trans, partitionTime);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                result = 0;
            }

            return result;
        }

        public bool InsertReward_Joker(RewardRNG_Joker data, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = _mainDbClient.QueryFirstAsync<int>("Service_RewardRNG_Joker_Ins", trans, new DynamicParameters(data)).Result;

            return (result == 0) ? false : true;
        }

        public bool CancelUpdateReward_Joker(int sq, MySqlConnection conn, MySqlTransaction trans)
        {
            var param = new { SQ = sq, TicketType = BettransType.CancelBet, TicketStatus = TicketStatus.Refund };
            var result = _mainDbClient.QueryFirstAsync<int>("Service_RewardRNG_Joker_Cancel_Set", trans, new DynamicParameters(param)).Result;

            return (result > 0) ? true : false;
        }

        public bool PayoutReward_Joker(TpCustInfo tpCustInfo, RewardRNG_Joker data,
            TransType transType, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PayoutV2(tpCustInfo,
                                  data.amount,
                                  (int)Tp.Joker,
                                  data.id, //TpTransId
                                  (int)GameType.Joker_RNG,
                                  TpCategory.Slots,
                                  data.tournamentid, //refId
                                  transType,
                                  null,
                                  out afterAmount,
                                  memo,
                                  out bonusId,
                                  conn,
                                  trans);

            return (result > 0) ? true : false;
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
                using (MySqlConnection conn = _mainDbClient.GetMySqlConnection())
                {

                    conn.Open();

                    using (var trans = conn.BeginTransaction())
                    {
                        var parameters = new DynamicParameters(insertData);
                        parameters.Add("type", (int)BettransType.Payout);

                        string SpName = "Service_BettransRngV2_YGG_Ins";
                        result = _mainDbClient.Execute(SpName, trans, parameters);


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
                                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                            }
                            else
                            {
                                result = -1;
                                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                            }
                        }
                        else
                        {
                            result = -1;
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int CancelBetRNG_YGG(TpCustInfo tpCustInfo, BettingDataRNG_YGG bettingDataRNG_YGG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.YGG_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_YGG.reference, bettingDataRNG_YGG.subreference });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_CancelBetRng_YGG_Set", trans, p);

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

                    result = SettleTicket(tpCustInfo, betRecordInfo, trans);

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            bettingDataRNG_PP.TicketStatus = bettingDataRNG_PP.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new { reference = bettingDataRNG_PP.reference, roundId = bettingDataRNG_PP.roundId, gameId = bettingDataRNG_PP.gameId ?? "", amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, roundDetails = bettingDataRNG_PP.roundDetails, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode, jackpotId = bettingDataRNG_PP.jackpotId, jackpotContribution = bettingDataRNG_PP.jackpotContribution, type = (int)bettingDataRNG_PP.type, TicketStatus = (int)bettingDataRNG_PP.TicketStatus, promoWinAmount = bettingDataRNG_PP.promoWinAmount, promoWinReference = bettingDataRNG_PP.promoWinReference, promoCampaignID = bettingDataRNG_PP.promoCampaignID, promoCampaignType = bettingDataRNG_PP.promoCampaignType });
            int result = 0;
            try
            {
                var bonusCode = (!string.IsNullOrEmpty(bettingDataRNG_PP.bonusCode)) ? $" ({bettingDataRNG_PP.bonusCode})" : "";
                var category = TpCategory.Slots;
                if (gameType == (int)GameType.PP_RNG)
                {
                    result = _mainDbClient.Execute("Service_BettransRng_PP_Ins", trans, p);
                }
                else
                {
                    category = TpCategory.LiveCasino;
                    result = _mainDbClient.Execute("Service_BettransLC_PP_Ins", trans, p);
                }

                if (result > 0)
                {
                    if (bettingDataRNG_PP.amount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_PP.amount,
                            (int)Tp.PP,
                            bettingDataRNG_PP.roundId.ToString(),
                            gameType,
                            category,
                            bettingDataRNG_PP.reference,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            $"Payout{bonusCode}",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (bettingDataRNG_PP.promoWinAmount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_PP.promoWinAmount,
                            (int)Tp.PP,
                            bettingDataRNG_PP.roundId.ToString(),
                            gameType,
                            category,
                            bettingDataRNG_PP.reference,
                            TransType.Prize,
                            null,
                            out afterAmount,
                            $"Bonus{bonusCode}",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = 0;
            try
            {
                result = _mainDbClient.Execute("Service_BettransRng_PP_BonusWin_Ins", trans, new { reference = bettingDataRNG_PP.reference, amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode });
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
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
            return _mainDbClient.Execute("Service_BettransLC_PP_Settle_Pre_Set_V2", commandType: CommandType.StoredProcedure, new { roundId = roundId });
        }

        public int CancelBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var bonusCode = (!string.IsNullOrEmpty(bettingDataRNG_PP.bonusCode)) ? $" ({bettingDataRNG_PP.bonusCode})" : "";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_PP.reference });
            p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            var category = TpCategory.Slots;
            if (gameType == (int)GameType.PP_RNG)
            {
                result = _mainDbClient.Execute("Service_CancelBetRng_PP_Set", trans, p);
            }
            else
            {
                category = TpCategory.LiveCasino;
                result = _mainDbClient.Execute("Service_BettransLC_PP_CancelBet_Set", trans, p);
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
                    result = PayoutV2(
                        tpCustInfo,
                        cancelStake,
                        (int)Tp.PP,
                        bettingDataRNG_PP.roundId.ToString(),
                        (int)gameType,
                        category,
                        bettingDataRNG_PP.reference,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        $"Refund{bonusCode}",
                        out bonusId,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = _mainDbClient.Execute("Service_PromoBonus_PP_Ins", trans, promoBonus);

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

                    result = SettleTicket(tpCustInfo, betRecordInfo, trans);

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransRng_PP_Ins", trans, p);
                }
                else
                {
                    category = TpCategory.LiveCasino;
                    tpGameType = GameType.PP_LiveCasino;
                    result = _mainDbClient.Execute("Service_BettransLC_PP_Ins", trans, p);
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        var setFlag = SetSettleFlag_BettransLC_PP_Payout(bettingDataRNG_PP.roundId);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var result = 0;
                var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, referenceId = bettingDataRNG_SG.referenceId });
                p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                var category = gameType switch
                {
                    (int)GameType.SG_RNG => TpCategory.Slots,
                    (int)GameType.SG_Fish => TpCategory.Fish,
                    (int)GameType.SG_Table => TpCategory.P2P,
                    _ => throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null)
                };

                string cancelSetSp;
                switch (gameType)
                {
                    case (int)GameType.SG_RNG:
                    case (int)GameType.SG_Table:
                        cancelSetSp = "Service_BettransRng_SG_CancelBet_Set";
                        break;
                    case (int)GameType.SG_Fish:
                        cancelSetSp = "Service_BettransFish_SG_CancelBet_Set";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null);
                }

                result = _mainDbClient.Execute(cancelSetSp, trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

                    string cancelInsSp;
                    switch (gameType)
                    {
                        case (int)GameType.SG_RNG:
                        case (int)GameType.SG_Table:
                            cancelInsSp = "Service_BettransRng_SG_CancelBet_Ins";
                            break;
                        case (int)GameType.SG_Fish:
                            cancelInsSp = "Service_BettransFish_SG_CancelBet_Ins";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(gameType), gameType, null);
                    }

                    _mainDbClient.Execute(cancelInsSp, trans, p);
                    var refLC = p.Get<int>("out_result");
                    if (refLC == 2)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            bettingDataRNG_SG.TicketStatus = bettingDataRNG_SG.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new
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
                ticketId = bettingDataRNG_SG.ticketId,
                referenceId = bettingDataRNG_SG.referenceId,
                specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame),
                refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds),
                TicketStatus = bettingDataRNG_SG.TicketStatus
            });

            try
            {
                if (bettingDataRNG_SG.type == TransferType.Bonus)
                {
                    p.Add("roundId", bettingDataRNG_SG.roundId);
                    result = _mainDbClient.Execute("Service_Reward_SG_Ins", trans, p);
                    if (result > 0 && bettingDataRNG_SG.amount > 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            bettingDataRNG_SG.amount,
                            (int)Tp.SG,
                            TransType.Prize,
                            $"Prize: {Math.Abs(bettingDataRNG_SG.amount):#,##0.##}",
                            bettingDataRNG_SG.ticketId,
                            out afterAmount,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        result = _tpUnRewardService.InsertTpUnReward(Tp.SG, GameType.SG_RNG, bettingDataRNG_SG.ticketId,
                            bettingDataRNG_SG.acctId, trans);
                    }
                }
                else if (gameType is (int)GameType.SG_RNG or (int)GameType.SG_Table)
                {
                    result = _mainDbClient.Execute("Service_BettransRng_SGV2_Ins", trans, p);
                    if (result > 0 && bettingDataRNG_SG.amount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataRNG_SG.amount,
                            (int)Tp.SG,
                            bettingDataRNG_SG.referenceId,
                            gameType,
                            gameType == (int)GameType.SG_RNG ? TpCategory.Slots : TpCategory.P2P,
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
                }
                else
                {
                    result = _mainDbClient.Execute("Service_BettransFish_SG_Ins", trans, p);
                    if (result > 0 && bettingDataRNG_SG.amount > 0)
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
                            out afterAmount,
                            conn,
                            trans
                       );
                    }
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                    throw mySqlEx;
                }
            }

            return result;
        }

        public SettleData_SG GetSettle_TpWagerRng_SG(string TransId)
        {
            return _wagerDbClient.QueryFirst<SettleData_SG>("Service_TpWagerRng_SG_Settle_Get_V2", new { TransId = TransId });
        }

        #endregion

        #region PG

        public int SettledRNG_PG(TpCustInfo tpCustInfo, BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_PG_Ins_V3", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_RewardRNG_PG_Ins", trans, p);

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
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return 0;
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_PG_Ins_V3", trans, p);

                if (result > 0)
                {
                    if (transferInOutDataRNG_PG.BetAmount != 0)
                    {
                        result = _bettingService.PlaceBet(
                            tpCustInfo,
                            transferInOutDataRNG_PG.BetAmount,
                            (int)Tp.PG,
                            transferInOutDataRNG_PG.ParentBetId,
                            gameType,
                            transferInOutDataRNG_PG.TransactionId,
                            out afterAmount,
                            ref bonusId,
                            out _,
                            conn,
                            trans
                        );

                        if (result != 1)
                        {
                            return 0;
                        }
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        if (getBonus)
                        {
                            //call Service_TpSlotsBonus_Get to set flag
                            GetTpBonus((int)Tp.PG, transferInOutDataRNG_PG.ParentBetId, gameType, category, out int custId, out bonusId);
                        }
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int ResettleRNG_PG(TpCustInfo tpCustInfo, ResettleDataRNG_PG resettleDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            decimal adjustAmount = 0;
            var transType = TransType.Refunds;
            if (resettleDataRNG_PG.type == TransferType.PlaceBet)
            {
                // cancel refund
                transType = TransType.Undo;
                adjustAmount = Math.Abs(resettleDataRNG_PG.TransferAmount);
            }
            if (resettleDataRNG_PG.type == TransferType.Payout)
            {
                // win to lose debit(-)
                adjustAmount = resettleDataRNG_PG.TransferAmount * -1;
            }

            var p = new DynamicParameters(new
            {
                TransactionId = $"F-{resettleDataRNG_PG.TransactionId}",
                BetTransactionId = resettleDataRNG_PG.BetTransactionId,
                ParentBetId = resettleDataRNG_PG.ParentBetId,
                BetId = resettleDataRNG_PG.BetId,
                PlayerName = resettleDataRNG_PG.PlayerName,
                GameId = resettleDataRNG_PG.GameId,
                CurrencyCode = resettleDataRNG_PG.CurrencyCode,
                BetAmount = 0,
                WinAmount = 0,
                TransferAmount = adjustAmount, // 抵銷winlose使用
                IsEndRound = resettleDataRNG_PG.IsEndRound,
                UpdatedTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                CreateTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                type = TransferType.CancelBet,
                TraceId = resettleDataRNG_PG.TraceId,
                WalletType = resettleDataRNG_PG.WalletType,
                Flag = SettleFlag.Running,
            });

            try
            {
                result = _mainDbClient.Execute("Service_BettransRng_PG_Ins_V3", trans, p);

                if (result > 0)
                {
                    if (resettleDataRNG_PG.TransferAmount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            adjustAmount,
                            (int)Tp.PG,
                            resettleDataRNG_PG.ParentBetId,
                            gameType,
                            TpCategory.Slots,
                            $"F-{resettleDataRNG_PG.TransactionId}",
                            transType,
                            null,
                            out afterAmount,
                            $"{transType}",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }
                    else
                    {
                        GetTpBonus((int)Tp.PG, resettleDataRNG_PG.ParentBetId, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (resettleDataRNG_PG.IsEndRound == "1")
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.PG, GameType.PG_RNG, resettleDataRNG_PG.ParentBetId, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public TpBetFailureRecord GetTpBetFailureParam_PG(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, TransferInOutDataRNG_PG transferInOutDataRNG_PG, TpCheckRecordCode Code, decimal AfterAmount)
        {
            // BLL會經過匯率換算 下列還原為原始資料
            TpBetFailureRecordInfo_PG InfoData = new TpBetFailureRecordInfo_PG
            {
                CurrentBalance = custBalance.Balance / tpCurrency.ExchangeRate,
                BetAmount = transferInOutDataRNG_PG.BetAmount / tpCurrency.ExchangeRate,
                WinAmount = transferInOutDataRNG_PG.WinAmount / tpCurrency.ExchangeRate,
                TransferAmount = transferInOutDataRNG_PG.TransferAmount / tpCurrency.ExchangeRate,
                InsufficientBalance = AfterAmount,
                ExchangeRate = tpCurrency.ExchangeRate,
            };

            return new TpBetFailureRecord
            {
                TransId = transferInOutDataRNG_PG.TransactionId,
                TpId = (int)Tp.PG,
                TpGameType = (int)GameType.PG_RNG,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }

        #endregion

        #region CQ9
        public int PayoutRNG_CQ9(TpCustInfo tpCustInfo, EndRoundDataRNG_CQ9 endRoundDataRNG_CQ9, ref decimal afterAmount)
        {
            int bonusId = 0;
            var gameType = GameType.CQ9_RNG;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
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
                            (int)gameType,
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

                        int insertRes = _mainDbClient.Execute("Service_BettransRng_CQ9_Payout_Ins", trans, p);

                        if (insertRes > 0)
                        {
                            insertRes = _tpUnsettleService.InsertTpUnsettle(Tp.CQ9, gameType, endRoundDataRNG_CQ9.roundid, trans);
                            if (insertRes > 0)
                            {
                                cntSucc++;
                            }
                        }
                    }
                }

                if (cntSucc == endRoundDataRNG_CQ9.detail.Count())
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

            using var conn = _mainDbClient.GetMySqlConnection();
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
                    var insertRes = _mainDbClient.Execute(
                          "Service_BettransRng_CQ9_Refund_Set", trans, new
                          {
                              mtcode = refundDataRNG_CQ9.mtcode,
                              before = afterAmount - Math.Abs(amount),
                              balance = afterAmount,
                          }
                      );
                    if (insertRes > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            afterAmount = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = _mainDbClient.Execute("Service_BettransFish_CQ9_Refund_Set", trans, new
            {
                mtcode = refundDataRNG_CQ9.mtcode,
                before = afterAmount - Math.Abs(refundDataRNG_CQ9.amount),
                balance = afterAmount,
            });

            if (result > 0)
            {
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
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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

                        var insertRes = _mainDbClient.Execute("Service_BettransRng_CQ9_Ins", trans, p);
                        if (insertRes > 0)
                        {
                            var settleRes = _mainDbClient.Execute("Service_BettransRng_CQ9_Settle_Set", trans, new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 });
                            if (settleRes > 0)
                            {
                                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                            }
                            else
                            {
                                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                            }
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount) * -1;

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
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
                result = _mainDbClient.Execute("Service_BettransFish_CQ9_Ins", trans, p);

                if (result > 0)
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
                    }

                    if (result > 0)
                    {
                        var settleRes = _mainDbClient.Execute("Service_BettransFish_CQ9_Settle_Set", trans, new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 });
                        if (settleRes > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            var gameType = GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                        (int)gameType,
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

                        var insertRes = _mainDbClient.Execute("Service_BettransRng_CQ9_Ins", trans, p);
                        if (insertRes > 0)
                        {
                            insertRes = _tpUnsettleService.InsertTpUnsettle(Tp.CQ9, gameType, bettingDataRNG_CQ9.roundid, trans);
                            if (insertRes > 0)
                            {
                                var settleRes = _mainDbClient.Execute("Service_BettransRng_CQ9_Settle_Set", trans, new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 });
                                if (settleRes > 0)
                                {
                                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                                }
                                else
                                {
                                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                                }
                            }
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount);

            conn.Open();
            using var trans = conn.BeginTransaction();
            try
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

                result = _mainDbClient.Execute("Service_BettransFish_CQ9_Ins", trans, p);

                if (result > 0)
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
                    }

                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.CQ9, GameType.CQ9_Fish, bettingDataRNG_CQ9.roundid, trans);
                        if (result > 0)
                        {
                            var settleRes = _mainDbClient.Execute("Service_BettransFish_CQ9_Settle_Set", trans, new { roundid = bettingDataRNG_CQ9.roundid, SettleFlag = 2 });
                            if (settleRes > 0)
                            {
                                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                            }
                            else
                            {
                                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                            }
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;
            var insertRes = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                if (payoffDataRNG_CQ9.amount != 0)
                {
                    var TransId = payoffDataRNG_CQ9.mtcode;
                    if (payoffDataRNG_CQ9.mtcode.StartsWith("atm-betaward", StringComparison.OrdinalIgnoreCase))
                    {
                        TransId = payoffDataRNG_CQ9.mtcode.Replace("atm-betaward", "atm-award");
                    }

                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        payoffDataRNG_CQ9.amount,
                        (int)Tp.CQ9,
                        TransType.Prize,
                        $"Prize: {Math.Abs(payoffDataRNG_CQ9.amount):#,##0.##}",
                        TransId,
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

                    insertRes = _mainDbClient.Execute("Service_RewardRNG_CQ9_Ins", trans, p);
                }

                if (insertRes > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int RollinFish_CQ9(TpCustInfo tpCustInfo, RollinDataFish_CQ9 rollinDataFish_CQ9, GameType gameType, ref decimal afterAmount)
        {
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
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

                    var settleFlag = (rollinDataFish_CQ9.bet == 0 && rollinDataFish_CQ9.win == 0) ? 1 : 2;
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
                        SettleFlag = settleFlag
                    });

                    result = _mainDbClient.Execute("Service_BettransFish_CQ9_Payout_Ins", trans, p);

                    if (result > 0 && settleFlag == 2)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.CQ9, gameType, rollinDataFish_CQ9.roundid, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }

            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public SettleDataRNG_CQ9 GetUnsettledListRNG_CQ9(string transId)
        {
            return _mainDbClient.QueryFirst<SettleDataRNG_CQ9>("Service_BettransRng_CQ9_Unsettle_Get_V3", new DynamicParameters(new { TransId = transId }));
        }

        public int SetSettleFlagRNG_CQ9(string roundid, int SettleFlag)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = _mainDbClient.Execute("Service_BettransRng_CQ9_Settle_Set", trans, new { roundid = roundid, SettleFlag = SettleFlag });
            _mainDbClient.CommitAndDisposeConnection(conn, trans);
            return result;
        }

        public SettleDataRNG_CQ9 GetUnsettledListFish_CQ9(string transId)
        {
            return _mainDbClient.QueryFirst<SettleDataRNG_CQ9>("Service_BettransFish_CQ9_Unsettle_Get_V3", new DynamicParameters(new { TransId = transId }));
        }

        public int SetSettleFlagFish_CQ9(string roundid, int SettleFlag)
        {
            var result = _mainDbClient.Execute("Service_BettransFish_CQ9_Settle_Set", commandType: CommandType.StoredProcedure, new { roundid = roundid, SettleFlag = SettleFlag });
            return result;
        }

        public SettleDataRNG_CQ9 GetUnsettledListWCC_CQ9(string transId)
        {
            return _mainDbClient.QueryFirst<SettleDataRNG_CQ9>("Service_BettransCF_CQ9_Unsettle_Get", new DynamicParameters(new { TransId = transId }));
        }

        public int SetSettleFlagWCC_CQ9(string roundid, int SettleFlag)
        {
            var result = _mainDbClient.Execute("Service_BettransCF_CQ9_Settle_Set", commandType: CommandType.StoredProcedure, new { roundid = roundid, SettleFlag = SettleFlag });
            return result;
        }

        public IEnumerable<Record_CQ9> GetRecordRNG_CQ9(string mtcode)
        {
            var data = _mainDbClient.Query<Record_CQ9>("Service_BettransRng_CQ9_Get_V2", commandType: CommandType.StoredProcedure, new { mtcode = mtcode });
            if (data.Count() > 0)
            {
                data.ElementAt(0).TableType = (int)GameType.CQ9_RNG;
                return data;
            }

            if (data == null || data.Count() == 0)
            {
                data = _mainDbClient.Query<Record_CQ9>("Service_BettransFish_CQ9_Get_V2", commandType: CommandType.StoredProcedure, new { mtcode = mtcode });
                if (data.Count() > 0)
                {
                    data.ElementAt(0).TableType = (int)GameType.CQ9_Fish;
                    return data;
                }
            }

            if (data == null || data.Count() == 0)
            {
                data = _mainDbClient.Query<Record_CQ9>("Service_BettransCF_CQ9_Get", commandType: CommandType.StoredProcedure, new { mtcode = mtcode });
                if (data.Count() > 0)
                {
                    data.ElementAt(0).TableType = (int)GameType.CQ9_WCC;
                    return data;
                }
            }

            if (data == null || data.Count() == 0)
            {
                data = _mainDbClient.Query<Record_CQ9>("Service_RewardRNG_CQ9_Get", commandType: CommandType.StoredProcedure, new { mtcode = mtcode });
            }
            return data;
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_Iconic_CancelBet_Set", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_Iconic_Payout_Set", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int CancelBetFish_Iconic(TpCustInfo tpCustInfo, BettingDataFish_Iconic bettingDataFish_Iconic, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransFish_Iconic_CancelBet_Set", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransFish_Iconic_Ins", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        #endregion

        #region YL Fishing

        public int SettledFish_YL(TpCustInfo tpCustInfo, BettingDataFish_YL bettingDataFish_YL, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            long transTime = 0;
            DateTime.TryParse(bettingDataFish_YL.transTime, out DateTime dtTransTime);
            transTime = tools.ConvertLocTime(dtTransTime, 8);
            try
            {
                result = _mainDbClient.Execute("Service_BettransFish_YL_Ins", trans, new
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
                }
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result = 0;
            long updateTime = 0;
            DateTime.TryParse(refundDataFish_YL.updateTime, out DateTime dtUpdateTime);
            updateTime = tools.ConvertLocTime(dtUpdateTime, 8);
            BettingDataFish_YL bettingDataFish_YL = _mainDbClient.QueryFirst<BettingDataFish_YL>("Service_BettransFish_YL_Get", new { refundDataFish_YL.txId });

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
                    result = _mainDbClient.Execute("Service_BettransFish_YL_Set", trans, new
                    {
                        refundDataFish_YL.txId,
                        updateTime = updateTime
                    }
                    );

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                _mainDbClient.Execute("Service_BettransFish_YL_CancelBet_Ins", trans, p);
                var refLC = p.Get<int>("out_result");
                if (refLC == 2)
                {
                    // insert cancel record
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            return _mainDbClient.Query<SettleData>("Service_BettransFish_YL_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }
        public int SetSettleFlagFish_YL(string TransId)
        {
            var result = _mainDbClient.Execute("Service_BettransFish_YL_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
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
            return _wagerDbClient.Query<SettleData>("Service_TpWagerRng_PT_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerRng_PT(long TransId)
        {
            return _wagerDbClient.Execute("Service_TpWagerRng_PT_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region MG
        public async Task<MGPayoutResult> PayoutRNG_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
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
                var bettransMg = await _mainDbClient.QueryAsync<BettransRng_MG>("Service_BettransRngByBet_MG_Get_V3", new { bettingDataRNG_MG.betId });
                var bettrans = bettransMg.ToList();
                // 已結束局
                if (bettrans.Any(a => a.Completed))
                {
                    logger.LogError($"{bettingDataRNG_MG.betId} is completed");

                    var refSettleFlag = bettrans.FirstOrDefault(b => b.Completed).SettleFlag;
                    // refSettleFlag -2 為已Settle注單手動調整 將執行此段重新結算 (避免會員重新進入遊戲對方會再重送資料)
                    if (bettingDataRNG_MG.completed && refSettleFlag == -2)
                    {
                        // 記錄重新結算時間
                        _mainDbClient.Execute("Service_BettransRng_MG_Resettle_Ins", trans, p);

                        // 重新結算
                        var r = _tpUnsettleService.InsertTpUnsettle(Tp.MG, GameType.MG_RNG, bettingDataRNG_MG.betId, trans);
                        if (r > 0)
                        {
                            await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                        }
                        else
                        {
                            await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                        }
                    }

                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }
                // 檢查注單是否已經存在
                if (bettrans.Any(a => a.TxnId == bettingDataRNG_MG.txnId))
                {
                    logger.LogError($"{bettingDataRNG_MG.txnId} exist");

                    return new MGPayoutResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount,
                        ExtTxnId = bettrans.FirstOrDefault(b => b.TxnId == bettingDataRNG_MG.txnId).ExtTxnId
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransRng_MG_Ins_V2", trans, p);
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
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    if (bettingDataRNG_MG.completed)
                    {
                        _tpUnsettleService.InsertTpUnsettle(Tp.MG, GameType.MG_RNG, bettingDataRNG_MG.betId, trans);
                    }
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
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

            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {

                var bettransMg = await _mainDbClient.QueryAsync<BettransRng_MG>("Service_BettransRng_MG_txnId_Get", new { txnId = cancelDataRng_MG.txnId });
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
                    logger.LogError($"{cancelDataRng_MG.betId} is completed(txnId)");
                    return new MGCancelResult() { Result = 1 };
                }

                // 檢查該局是否已經結算
                var isCompleted = await _mainDbClient.QueryFirstOrDefaultAsync<bool>("Service_BettransRng_MG_Check", trans, new { cancelDataRng_MG.betId });
                if (isCompleted)
                {
                    logger.LogError($"{cancelDataRng_MG.betId} is completed(betId)");
                    return new MGCancelResult() { Result = 1 };
                }

                var bet = bettrans.FirstOrDefault(a => a.TxnId == cancelDataRng_MG.txnId);
                // 注單不存在
                if (bet == null)
                {
                    logger.LogInformation($"no txnId: {cancelDataRng_MG.txnId}");
                    return new MGCancelResult() { Result = 1 };
                }
                // 玩家或金額不符
                if (bet.PlayerId != cancelDataRng_MG.playerId || Math.Abs(bet.Amount) != cancelDataRng_MG.amount)
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

                var betId = (!string.IsNullOrEmpty(cancelDataRng_MG.betId)) ? cancelDataRng_MG.betId : bet.BetId;


                var result = _mainDbClient.Execute("Service_BettransRng_MG_CancelBet_Set", new { betId, cancelDataRng_MG.txnId }, conn, trans);
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
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
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
            return _mainDbClient.Query<SettleData>(spName, commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_MG(string spName, string transId)
        {
            return _mainDbClient.Execute(spName, commandType: CommandType.StoredProcedure, new { betId = transId });
        }

        public int GetTpGameType_MG(string txnId, long creationTimeMs)
        {
            var tpGameType = 0;
            var p = new DynamicParameters(new
            {
                txnId = txnId
            });
            var slot = _mainDbClient.QueryFirst<BettransRng_MG>("Service_BettransRng_MG_txnId_Get", p);

            if (slot != null)
            {
                tpGameType = (int)GameType.MG_RNG;
                return tpGameType;
            }

            var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(creationTimeMs).DateTime;

            var live = _partitionService.Query<BettingDataRNG_MG>(partitionTime, "Service_BettransLC_MG_txnId_Get", p);
            if (live.Any())
            {
                tpGameType = (int)GameType.MG_LiveCasino;
                return tpGameType;
            }

            return tpGameType;
        }

        #endregion

        #region JILI

        public int PayoutRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, ThirdPartyBLL thirdPartyBLL, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_JILI.wagersTime).DateTime;

                // check exists
                var check = _partitionService.Query<BettransData_JILI>(partitionTime, "Service_BettransRng_JILI_Round_Get", new { round = bettingData_JILI.round });
                if (check.Any())
                {
                    return -1;
                }

                var iTicketStatus = (int)TicketStatus.Win;
                var winlose = bettingData_JILI.winloseAmount - bettingData_JILI.turnover;
                if (winlose < 0)
                {
                    iTicketStatus = (int)TicketStatus.Lost;
                }
                else if (winlose == 0)
                {
                    iTicketStatus = (int)TicketStatus.Draw;
                }

                var p = new DynamicParameters(new
                {
                    reqId = bettingData_JILI.reqId,
                    userId = bettingData_JILI.userId,
                    currency = bettingData_JILI.currency,
                    game = bettingData_JILI.game,
                    round = bettingData_JILI.round,
                    wagersTime = bettingData_JILI.wagersTime,
                    betAmount = Math.Abs(bettingData_JILI.betAmount) * -1,
                    winloseAmount = bettingData_JILI.winloseAmount,
                    turnover = bettingData_JILI.turnover,
                    preserve = bettingData_JILI.preserve,
                    jpContribute = bettingData_JILI.jpContribute,
                    jpWin = bettingData_JILI.jpWin,
                    isFreeRound = bettingData_JILI.isFreeRound,
                    TransactionID = bettingData_JILI.TransactionID,
                    sessionId = bettingData_JILI.sessionId,
                    type = bettingData_JILI.type,
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = iTicketStatus,
                    SettleFlag = SettleFlag.EndGame
                });

                var category = TpCategory.Slots;
                var gameType = GameType.JILI_RNG;

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_JILI_Ins_V2", p, trans);
                if (result > 0)
                {
                    int bonusId = 0;
                    // 退還預扣下注差額
                    if (bettingData_JILI.preserve > 0)
                    {
                        var refundBet = bettingData_JILI.preserve - bettingData_JILI.betAmount;
                        if (refundBet > 0)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                refundBet,
                                (int)Tp.JILI,
                                bettingData_JILI.sessionId.ToString(),
                                (int)gameType,
                                category,
                                bettingData_JILI.sessionId.ToString(),
                                TransType.Refunds,
                                null,
                                out afterAmount,
                                "Payout",
                                out bonusId,
                                conn,
                                trans
                            );
                        }
                    }

                    // 派彩
                    if (bettingData_JILI.winloseAmount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingData_JILI.winloseAmount,
                            (int)Tp.JILI,
                            bettingData_JILI.sessionId.ToString(),
                            (int)gameType,
                            category,
                            bettingData_JILI.sessionId.ToString(),
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
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.JILI, GameType.JILI_RNG, bettingData_JILI.sessionId.ToString(), trans, partitionTime);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int BetAndPayoutRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, ThirdPartyBLL thirdPartyBLL, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_JILI.wagersTime).DateTime;
                // check exists
                var check = _partitionService.Query<BettransData_JILI>(partitionTime, "Service_BettransRng_JILI_Round_Get", new { round = bettingData_JILI.round });
                if (check.Any())
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
                    turnover = 0,
                    preserve = 0,
                    jpContribute = 0,
                    jpWin = 0,
                    sessionId = 0,
                    type = 0,
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

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_JILI_Ins_V2", p, trans);
                if (result > 0)
                {
                    int bonusId = 0;
                    if (betAmount != 0)
                    {
                        result = _bettingService.PlaceBet(
                            tpCustInfo,
                            betAmount,
                            (int)Tp.JILI,
                            bettingData_JILI.round.ToString(),
                            gameType,
                            bettingData_JILI.round.ToString(),
                            out afterAmount,
                            ref bonusId,
                            out _,
                            conn,
                            trans
                        );

                        if (result != 1)
                        {
                            return 0;
                        }
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
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.JILI, GameType.JILI_RNG, bettingData_JILI.round.ToString(), trans, partitionTime);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }

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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            try
            {
                var current = new DateTimeOffset(DateTime.UtcNow);
                var partitionTime = current.DateTime;
                // check exists
                var check = _partitionService.Query<BettransData_JILI>(partitionTime, "Service_BettransRng_JILI_Round_Get", new { round = cancelBetData_JILI.round });
                if (!check.Any())
                {
                    return -2;
                }
                else if (check.Any(x => x.TicketStatus == (int)TicketStatus.Refund))
                {
                    return -1;
                }

                var p = new DynamicParameters(new
                {
                    round = cancelBetData_JILI.round,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = TicketStatus.Refund,
                    SettleFlag = SettleFlag.Settled
                });

                var category = TpCategory.Slots;
                var gameType = (int)GameType.JILI_RNG;

                var refundAmount = Math.Abs(cancelBetData_JILI.betAmount) + (Math.Abs(cancelBetData_JILI.winloseAmount) * -1);
                var transId = cancelBetData_JILI.round.ToString();
                var memo = "CancelBet";
                if (cancelBetData_JILI.sessionId > 0)
                {
                    refundAmount = (cancelBetData_JILI.betAmount != 0) ? Math.Abs(cancelBetData_JILI.betAmount) : Math.Abs(cancelBetData_JILI.preserve);
                    transId = cancelBetData_JILI.sessionId.ToString();
                    memo = $"CancelBet - {transId}";
                }

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_JILI_CancelBet_Set_V2", p, trans);
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
                            memo,
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.JILI, GameType.JILI_RNG, transId, trans, partitionTime);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
                }
                else
                {
                    // cancel insert
                    if (cancelBetData_JILI.sessionId > 0)
                    {
                        /*
                         說明: 牌局型注單的特性是，必須等前一筆下注成功了才會進行下一筆，下注都成功後會進行結算，或是某一筆下注失敗後中止牌局也進行結算。
                             取消下注 (type=1): 成功時只會取消單筆下注，同局的其他下注不受影響。正常情況下，只會取消最後一筆下注，而且必須是還未結算的牌局。
                             接受取消後，這一局就不可再接受任何下注，只會有結算。之後遊戲商對同一局發送的下注無效。
                             有可能因為網路延遲或其他因素，導致營運商在收到下注前就收到取消，對營運商來說是取消一筆不存在的下注，這時營運商應注記該局為失敗，之後同一局的下注均無效。
                             正常情況遊戲商不會在結算後發送 type 1 取消。但有可能因為網路延遲或其他因素，導致營運商在結算後才收到 type 1 的取消。若營運商收到此種 type 1 取消，應取消該筆下注，原因是遊戲商是以該筆下注失敗為前提進行結算。
                             遊戲商不會對結算 (type=2) 發送取消
                             我司發送的 cancelSessionBet 都是對應牌局的其中一筆下注, 請參考 round 欄位
                        */
                        var cancelParam = new DynamicParameters(new
                        {
                            reqId = cancelBetData_JILI.reqId,
                            userId = cancelBetData_JILI.userId,
                            currency = cancelBetData_JILI.currency,
                            game = cancelBetData_JILI.game,
                            round = cancelBetData_JILI.round,
                            wagersTime = current.ToUnixTimeSeconds(),
                            betAmount = cancelBetData_JILI,
                            winloseAmount = cancelBetData_JILI.winloseAmount,
                            turnover = 0,
                            preserve = cancelBetData_JILI.preserve,
                            jpContribute = 0,
                            jpWin = 0,
                            isFreeRound = 0,
                            TransactionID = 0,
                            sessionId = cancelBetData_JILI.sessionId,
                            type = cancelBetData_JILI.type,
                            SiteId = tpCustInfo.SiteId,
                            CustId = tpCustInfo.CustId,
                            CurrencyId = tpCustInfo.CurrencyId,
                            TicketType = (int)BettransType.CancelBet,
                            TicketStatus = TicketStatus.Refund,
                            SettleFlag = SettleFlag.Settled
                        });

                        var cancelInsert = _partitionService.Execute(partitionTime, "Service_BettransRng_JILI_Ins_V2", cancelParam, trans);
                        if (cancelInsert > 0)
                        {
                            // insert cancel record
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                    }
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = _mainDbClient.QueryFirst<BettransData_JILI>("Service_BettransFish_JILI_Get", new { round = bettingData_JILI.round });
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

                result = _mainDbClient.Execute("Service_BettransFish_JILI_Ins", trans, p);

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

                        if (result != 1)
                        {
                            return 0;
                        }
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // check bet
                var check = _mainDbClient.QueryFirst<BettransData_JILI>("Service_BettransFish_JILI_Get", new { round = cancelBetData_JILI.round });
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

                result = _mainDbClient.Execute("Service_BettransFish_JILI_CancelBet_Set", trans, p);

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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int SetSettleFlag_BettransRng_JILI(string spName, string transId, int ticketStatus, long settleTime)
        {
            var result = 0;
            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(settleTime).DateTime;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            result = _partitionService.Execute(partitionTime, spName, new DynamicParameters(new { TransId = transId, TicketStatus = ticketStatus }), trans);
            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;
        }

        public IEnumerable<CheckBettingData_JILI> GetTransactionCheck_JILI(string YYYYMM)
        {
            var query = new { _YYYYMM = YYYYMM };
            string sp = "Service_BettransRng_JILI_TransactionCheck_Get";
            return _mainDbClient.Query<CheckBettingData_JILI>(sp, CommandType.StoredProcedure, new DynamicParameters(query));
        }

        public int SetTransactionCheckSettleFlag_JILI(string YYYYMM, long TransId, int SettleFlag)
        {
            string sp = "Service_BettransRng_JILI_TransactionCheck_Set";
            return _mainDbClient.Execute(sp, parameters: new DynamicParameters(new { _YYYYMM = YYYYMM, _TransId = TransId, _SettleFlag = SettleFlag }));
        }

        public TpBetFailureRecord GetTpBetFailureParam_JILI(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, BettingData_JILI bettingData_JILI, int gameType, TpCheckRecordCode Code, decimal AfterAmount)
        {
            // BLL會經過匯率換算 下列還原為原始資料
            TpBetFailureRecordInfo_JILI InfoData = new TpBetFailureRecordInfo_JILI
            {
                CurrentBalance = custBalance.Balance / tpCurrency.ExchangeRate,
                betAmount = bettingData_JILI.betAmount / tpCurrency.ExchangeRate,
                winloseAmount = bettingData_JILI.winloseAmount / tpCurrency.ExchangeRate,
                InsufficientBalance = AfterAmount,
                ExchangeRate = tpCurrency.ExchangeRate,
            };

            return new TpBetFailureRecord
            {
                TransId = (bettingData_JILI.sessionId == 0) ? bettingData_JILI.round.ToString() : bettingData_JILI.sessionId.ToString(),
                TpId = (int)Tp.JILI,
                TpGameType = gameType,
                SiteId = tpCustInfo.SiteId,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName,
                Code = (int)Code,
                TransTime = bettingData_JILI.wagersTime,
                Info = JsonConvert.SerializeObject(InfoData),
            };
        }
        #endregion

        #region Netent

        public int CancelBetRNG_Netent(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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

            result = _mainDbClient.Execute("Service_BettransRng_Netent_CancelBet_Set", trans, p);

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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            else
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransRng_Netent_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
                if (!check.Any())
                {
                    var cancelInsert = _mainDbClient.Execute("Service_BettransRng_Netent_Ins", p, conn, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -2;
                    }
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransRng_Netent_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
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

                result = _mainDbClient.Execute("Service_BettransRng_Netent_Ins", trans, p);

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
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.Netent, GameType.Netent_RNG, bettingData_EVO.transaction.refId, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public SettleData GetUnsettledList_Netent(string transactionRefId)
        {
            return _mainDbClient.QueryFirst<SettleData>("Service_BettransRng_Netent_Unsettle_Get_V2", parameters: new DynamicParameters(new { TransactionRefId = transactionRefId }));
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
            using var conn = _mainDbClient.GetMySqlConnection();
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

            result = _mainDbClient.Execute("Service_BettransRng_RedTiger_CancelBet_Set", trans, p);

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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            else
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransRng_RedTiger_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
                if (!check.Any())
                {
                    var cancelInsert = _mainDbClient.Execute("Service_BettransRng_RedTiger_Ins", p, conn, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -2;
                    }
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
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransRng_RedTiger_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
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

                result = _mainDbClient.Execute("Service_BettransRng_RedTiger_Ins", trans, p);

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
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.RedTiger, GameType.RedTiger_RNG, bettingData_EVO.transaction.refId, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public SettleData GetUnsettledList_RedTiger(string transactionRefId)
        {
            return _mainDbClient.QueryFirst<SettleData>("Service_BettransRng_RedTiger_Unsettle_Get_V2", parameters: new DynamicParameters(new { TransactionRefId = transactionRefId }));
        }

        public int SetSettleFlag_RedTiger(string TransId)
        {
            return _mainDbClient.Execute("Service_BettransRng_RedTiger_Settle_Set", parameters: new DynamicParameters(new { TransId = TransId }));
        }

        #endregion

        #region NLC

        public int CancelBetRNG_NLC(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            int result = 0;

            var tpId = Tp.NLC;
            var tpGameType = GameType.NLC_RNG;
            var tpCategory = TpCategory.Slots;
            var SetSpName = "Service_BettransRng_NLC_CancelBet_Set";
            var InsSpName = "Service_BettransRng_NLC_Ins";
            var GetSpName = "Service_BettransRng_NLC_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.UtcNow;
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

                result = _partitionService.Execute(partitionTime, SetSpName, p, trans);
                if (result > 0)
                {
                    if (bettingData_EVO.transaction.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_EVO.transaction.amount),
                            (int)tpId,
                            bettingData_EVO.transaction.refId,
                            (int)tpGameType,
                            tpCategory,
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    var check = _partitionService.Query<BettransData_EVO>(partitionTime, GetSpName, new { TransactionRefId = bettingData_EVO.transaction.refId });
                    if (!check.Any())
                    {
                        var cancelInsert = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                        if (cancelInsert > 0)
                        {
                            // insert cancel record
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                            return -2;
                        }
                    }

                    var subChk = check.Where(x => x.TicketType == 2);
                    if (subChk.Any())
                    {
                        // had cancel
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int PayoutRNG_NLC(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;

            var tpId = Tp.NLC;
            var tpGameType = GameType.NLC_RNG;
            var tpCategory = TpCategory.Slots;
            var InsSpName = "Service_BettransRng_NLC_Ins";
            var GetSpName = "Service_BettransRng_NLC_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.UtcNow;
                var check = _partitionService.Query<BettransData_EVO>(partitionTime, GetSpName, new { TransactionRefId = bettingData_EVO.transaction.refId });
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

                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                if (result > 0)
                {
                    int bonusId = 0;

                    if (bettingData_EVO.transaction.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_EVO.transaction.amount),
                            (int)tpId,
                            bettingData_EVO.transaction.refId,
                            (int)tpGameType,
                            tpCategory,
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
                        result = _tpUnsettleService.InsertTpUnsettle(tpId, (GameType)tpGameType, bettingData_EVO.transaction.refId.ToString(), trans, partitionTime);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
        }

        public int PromoPayoutRNG_NLC(TpCustInfo tpCustInfo, Reward_EVO data, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            var tpId = Tp.NLC;
            var tpGameType = GameType.NLC_RNG;
            var tpCategory = TpCategory.Slots;

            using var conn = _mainDbClient.GetMySqlConnection();
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
                            (int)tpId,
                            data.PromoTransactionId,
                            (int)tpGameType,
                            tpCategory,
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                var check = _partitionService.Query<BettransRNG_AP>(partitionTime, "Service_BettransRng_AP_Get_V2", new { bettingDataRNG_AP.gameroundid, bettingDataRNG_AP.TicketType });
                if (check.Any())
                {
                    return -1;//Duplicated
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                var win = bettingDataRNG_AP.jpwin > 0 ? bettingDataRNG_AP.jpwin : bettingDataRNG_AP.totalwin;
                bettingDataRNG_AP.TicketStatus = win > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;

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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                throw;
            }

            return result;
        }

        public int RewardRNG_AP(TpCustInfo tpCustInfo, CPSettleDataRNG_AP rewardDataRNG_AP, out decimal afterAmount)
        {
            var bonusId = 0;
            var result = 0;
            afterAmount = 0;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();

                var p = new DynamicParameters(rewardDataRNG_AP);
                result = _mainDbClient.Execute("Service_RewardRNG_AP_Ins_V2", trans, p);

                if (result > 0 && rewardDataRNG_AP.cpwin > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        rewardDataRNG_AP.cpwin,
                        (int)Tp.AP,
                        rewardDataRNG_AP.cptransid,
                        (int)GameType.AP_RNG,
                        TpCategory.Slots,
                        rewardDataRNG_AP.seq,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"{rewardDataRNG_AP.cpid} Prize",
                        out bonusId,
                        conn,
                        trans
                    );
                    if (result > 0)
                    {
                        result = _tpUnRewardService.InsertTpUnReward(Tp.AP, GameType.AP_RNG, rewardDataRNG_AP.cptransid, rewardDataRNG_AP.playerid, trans);
                    }
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
                    logger.LogError(mySqlEx, mySqlEx.Message);
                    throw;
                }
            }

            return result;
        }
        #endregion

        #region AdvantPlay Mini 

        public int Payout_AdvantPlayMini(TpCustInfo tpCustInfo, PayoutData_AdvantPlayMini payoutData_AdvantPlayMini, out decimal afterAmount)
        {
            var bonusId = 0;
            var result = 0;
            afterAmount = 0;
            try
            {
                // Service_BettransRng_AP_Mini_crash_Ins、Service_BettransRng_AP_Mini_cardracing_Ins
                var spName = $"Service_BettransRng_AP_Mini_{payoutData_AdvantPlayMini.MiniGameCode.ToLower()}_Ins";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(payoutData_AdvantPlayMini.SettlementTime).DateTime;

                decimal WonAmount = payoutData_AdvantPlayMini.WonAmount;
                decimal BuyAmount = 0;
                decimal Odds = 0;
                string Suit = "";
                string TransactionReferenceId = payoutData_AdvantPlayMini.TransactionReferenceId;
                string TransId = payoutData_AdvantPlayMini.TransId;
                if (payoutData_AdvantPlayMini.WinResults.Any())
                {
                    var WinResults = payoutData_AdvantPlayMini.WinResults.FirstOrDefault();
                    WonAmount = WinResults.WonAmount;
                    BuyAmount = WinResults.BuyAmount;
                    Odds = WinResults.Odds;
                    TransactionReferenceId = WinResults.TransactionReferenceId;
                    TransId = WinResults.TransactionReferenceId;
                }

                if (payoutData_AdvantPlayMini.LostResults.Any())
                {
                    var LostResults = payoutData_AdvantPlayMini.LostResults.FirstOrDefault();
                    Suit = LostResults.Suit;
                    TransactionReferenceId = LostResults.TransactionReferenceId;
                    TransId = LostResults.TransactionReferenceId;
                }

                var p = new DynamicParameters(new
                {
                    TransId = TransId,
                    TransactionReferenceId = TransactionReferenceId,
                    GameId = payoutData_AdvantPlayMini.GameId,
                    SessionId = payoutData_AdvantPlayMini.SessionId,
                    UserReferenceId = payoutData_AdvantPlayMini.UserReferenceId,
                    Amount = 0,
                    BetTime = 0,
                    SettlementTime = payoutData_AdvantPlayMini.SettlementTime,
                    StakeAmount = payoutData_AdvantPlayMini.StakeAmount,
                    BuyAmount = BuyAmount,
                    WonAmount = WonAmount,
                    Multiplier = payoutData_AdvantPlayMini.Multiplier,
                    Odds = Odds,
                    Suit = Suit,
                    RefundTime = 0,
                    Data = "[]",
                    WinResults = JsonConvert.SerializeObject(payoutData_AdvantPlayMini.WinResults),
                    LostResults = JsonConvert.SerializeObject(payoutData_AdvantPlayMini.LostResults),
                    GameResult = payoutData_AdvantPlayMini.GameResult,
                    MiniGameCode = payoutData_AdvantPlayMini.MiniGameCode,
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = (payoutData_AdvantPlayMini.WonAmount > 0) ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                    SettleFlag = SettleFlag.Running
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                result = _partitionService.Execute(partitionTime, spName, p, trans);

                if (result > 0 && WonAmount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        WonAmount,
                        (int)Tp.AP,
                        TransId,
                        (int)GameType.AP_RNG,
                        TpCategory.Slots,
                        TransId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans
                   );
                }

                if (result > 0 && payoutData_AdvantPlayMini.MiniGameCode == "cardracing")
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.AP, GameType.AP_RNG, TransId, trans);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    return -1;
                }
            }

            return result;
        }

        public int Result_AdvantPlayMini(TpCustInfo tpCustInfo, ResultData_AdvantPlayMini resultData_AdvantPlayMini)
        {
            var result = 0;
            try
            {
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();

                // check exist
                // Service_BettransRng_AP_Mini_crash_Get
                var spName = $"Service_BettransRng_AP_Mini_{resultData_AdvantPlayMini.MiniGameCode.ToLower()}_Get";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(resultData_AdvantPlayMini.SettlementTime).DateTime;
                var data = "[]";
                if (resultData_AdvantPlayMini.Data.Any(i => i.TransactionReferenceId == resultData_AdvantPlayMini.TransId))
                {
                    data = JsonConvert.SerializeObject(resultData_AdvantPlayMini.Data.Where(i => i.TransactionReferenceId == resultData_AdvantPlayMini.TransId).ToList());
                }
                var p = new DynamicParameters(new
                {
                    TransId = resultData_AdvantPlayMini.TransId,
                    TransactionReferenceId = resultData_AdvantPlayMini.TransId,
                    GameId = resultData_AdvantPlayMini.GameId,
                    SessionId = resultData_AdvantPlayMini.SessionId,
                    UserReferenceId = resultData_AdvantPlayMini.UserReferenceId,
                    Amount = 0,
                    BetTime = 0,
                    SettlementTime = resultData_AdvantPlayMini.SettlementTime,
                    StakeAmount = 0,
                    BuyAmount = 0,
                    WonAmount = 0,
                    Multiplier = resultData_AdvantPlayMini.Multiplier,
                    Odds = 0,
                    //Suit = Suit,
                    RefundTime = 0,
                    Data = data,
                    WinResults = "[]",
                    LostResults = "[]",
                    GameResult = "",
                    MiniGameCode = resultData_AdvantPlayMini.MiniGameCode,
                    TicketType = (int)BettransType.EndRound,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = SettleFlag.EndGame
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

                // Service_BettransRng_AP_Mini_crash_Ins
                spName = $"Service_BettransRng_AP_Mini_{resultData_AdvantPlayMini.MiniGameCode.ToLower()}_Ins";
                result = _partitionService.Execute(partitionTime, spName, p, trans);

                if (result > 0)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.AP, GameType.AP_RNG, resultData_AdvantPlayMini.TransId, trans);

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    return -1;
                }
            }

            return result;
        }

        public int CancelBet_AdvantPlayMini(TpCustInfo tpCustInfo, CancelBetData_AdvantPlayMini cancelBetData_AdvantPlayMini)
        {
            var result = 0;
            var bonusId = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            try
            {
                var amount = cancelBetData_AdvantPlayMini.Data.First().Amount;
                var TransactionReferenceId = cancelBetData_AdvantPlayMini.Data.First().TransactionReferenceId;

                // Service_BettransRng_AP_Mini_crash_CancelBet_Set、Service_BettransRng_AP_Mini_cardracing_CancelBet_Set
                var spName = $"Service_BettransRng_AP_Mini_{cancelBetData_AdvantPlayMini.MiniGameCode.ToLower()}_CancelBet_Set";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(cancelBetData_AdvantPlayMini.refundTime).DateTime;

                var p = new DynamicParameters(new
                {
                    TransactionReferenceId = TransactionReferenceId,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund
                });

                result = _partitionService.Execute(partitionTime, spName, p, trans);

                if (result > 0)
                {
                    if (amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            amount,
                            (int)Tp.AP,
                            TransactionReferenceId,
                            (int)GameType.AP_RNG,
                            TpCategory.Slots,
                            TransactionReferenceId,
                            TransType.Payout,
                            null,
                            out _,
                            "CancelBet",
                            out bonusId,
                            conn,
                            trans
                       );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
                else
                {
                    // insert cancel
                    var pCancel = new DynamicParameters(new
                    {
                        TransId = cancelBetData_AdvantPlayMini.TransId,
                        TransactionReferenceId = TransactionReferenceId,
                        GameId = cancelBetData_AdvantPlayMini.GameId,
                        SessionId = cancelBetData_AdvantPlayMini.SessionId,
                        UserReferenceId = cancelBetData_AdvantPlayMini.UserReferenceId,
                        Amount = amount,
                        BetTime = 0,
                        SettlementTime = 0,
                        StakeAmount = 0,
                        BuyAmount = 0,
                        WonAmount = 0,
                        Multiplier = 0,
                        Odds = 0,
                        Suit = 0,
                        RefundTime = cancelBetData_AdvantPlayMini.refundTime,
                        Data = "[]",
                        WinResults = "[]",
                        LostResults = "[]",
                        GameResult = "",
                        MiniGameCode = cancelBetData_AdvantPlayMini.MiniGameCode,
                        TicketType = (int)TransferType.CancelBet,
                        TicketStatus = (int)TicketStatus.Refund,
                        SettleFlag = SettleFlag.EndGame
                    });
                    pCancel.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

                    // Service_BettransRng_AP_Mini_crash_Ins、Service_BettransRng_AP_Mini_cardracing_Ins
                    spName = $"Service_BettransRng_AP_Mini_{cancelBetData_AdvantPlayMini.MiniGameCode.ToLower()}_Ins";
                    var cancelInsert = _partitionService.Execute(partitionTime, spName, pCancel, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    return -1;
                }
                throw mySqlEx;
            }

            return result;
        }

        public int CancelPayout_AdvantPlayMini(TpCustInfo tpCustInfo, CancelBetData_AdvantPlayMini cancelBetData_AdvantPlayMini)
        {
            var result = 0;
            var bonusId = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            try
            {
                var amount = cancelBetData_AdvantPlayMini.Data.First().Amount;
                var TransactionReferenceId = cancelBetData_AdvantPlayMini.Data.First().TransactionReferenceId;

                // Service_BettransRng_AP_Mini_cardracing_CancelPayout_Set
                var spName = $"Service_BettransRng_AP_Mini_cardracing_CancelPayout_Set";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(cancelBetData_AdvantPlayMini.refundTime).DateTime;

                var p = new DynamicParameters(new
                {
                    TransactionReferenceId = TransactionReferenceId,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund
                });

                result = _partitionService.Execute(partitionTime, spName, p, trans);

                if (result > 0)
                {
                    if (amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            amount,
                            (int)Tp.AP,
                            TransactionReferenceId,
                            (int)GameType.AP_RNG,
                            TpCategory.Slots,
                            TransactionReferenceId,
                            TransType.Payout,
                            null,
                            out _,
                            "Resettle",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                       );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    return -1;
                }
                throw mySqlEx;
            }

            return result;
        }

        public int SetSettleFlag_BettransRng_AP_Mini(string spName, string transId, long settleTime)
        {
            var result = 0;
            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(settleTime).DateTime;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            result = _partitionService.Execute(partitionTime, spName, new DynamicParameters(new { TransId = transId }), trans);
            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public bool BetAndPayout_AMB(DateTime partitionTime, BettingDataRNG_AMB data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_AMB_Ins_V2", data, trans);
            return result != 0;
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

        public bool SetBalance_AMB(TpCustInfo tpCustInfo, BettingDataRNG_AMB data, TransType transType, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            if (bonusId > 0)
            {
                result = PayoutWithBonusId(tpCustInfo, data.amount, (int)Tp.AMB, data.roundId, (int)GameType.AMB_RNG,
                    TpCategory.Slots, data.transactionId, transType, out afterAmount, memo, bonusId, conn, trans);
            }
            else
            {
                result = PayoutV2(tpCustInfo, data.amount, (int)Tp.AMB, data.roundId, (int)GameType.AMB_RNG,
                    TpCategory.Slots, data.transactionId, transType, null,
                    out afterAmount, memo, out bonusId, conn, trans);
            }
            return result != 0;
        }

        public void UpdateTpBonusFlag(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId)
        {
            GetTpBonus(tpId, tpTransId, gameType, tpCategory, out CustId, out BonusId);
        }

        #endregion

        #region YGG

        public SettleData_YGG GetSettle_TpWager_YGG(string transId)
        {
            return _wagerDbClient.QueryFirst<SettleData_YGG>("Service_TpWager_YGG_Settle_Get_V2", new { TransId = transId });
        }

        #endregion

        #region Kiss918

        public IEnumerable<BettingDataRNG_Kiss918> IsExistBet_Kiss918(DateTime partitionTime, BettingDataRNG_Kiss918 data)
        {
            var query = new { playSessionID = data.playSessionID };
            return _partitionService.Query<BettingDataRNG_Kiss918>(partitionTime, "Service_BettransRng_Kiss918_Cancel_Check", query);
        }

        public bool Payout_Kiss918(DateTime partitionTime, BettingDataRNG_Kiss918 data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Kiss918_Ins", data, trans);

            if (result == 0)
                return false;

            return true;
        }

        public bool Refund_Kiss918(DateTime partitionTime, BettingDataRNG_Kiss918 data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Kiss918_Ins", data, trans);

            if (result == 0)
                return false;

            return true;
        }

        public bool SetBalance_Kiss918(TpCustInfo tpCustInfo, BettingDataRNG_Kiss918 data, TransType transType, string memo,
                                       out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {

            var result = PayoutV2(
                tpCustInfo,
                data.amount,
                (int)Tp.Kiss918,
                data.NewRoundId,
                (int)GameType.Kiss918_RNG,
                TpCategory.Slots,
                data.NewRoundId,
                transType,
                null,
                out afterAmount,
                $"{memo} - {data.referenceID} ",
                out bonusId,
                conn,
                trans
            );

            if (result == 0)
                return false;

            return true;
        }

        public int GetTpBonus_Kiss918(TpCustInfo tpCustInfo, BettingDataRNG_Kiss918 data)
        {
            var result = GetTpBonus((int)Tp.Kiss918, data.NewRoundId, (int)GameType.Kiss918_RNG, TpCategory.Slots, out int custId, out int bonusId);
            return bonusId;
        }

        public bool InsertUnsettle_Kiss918(DateTime partitionTime, string playSessionID, MySqlTransaction trans)
        {
            var result = _tpUnsettleService.InsertTpUnsettle(Tp.Kiss918, GameType.Kiss918_RNG, playSessionID, trans, partitionTime);

            if (result == 0)
                return false;

            return true;
        }

        public IEnumerable<SettleData_Kiss918> GetSettle_TpWager_Kiss918(string playSessionID, string referenceID)
        {
            return _wagerDbClient.Query<SettleData_Kiss918>("Service_TpWagerKiss918_Get", commandType: CommandType.StoredProcedure, new { _playSessionID = playSessionID, _referenceID = referenceID });
        }

        #endregion

        #region SimplePlay

        public int PayoutRNG_SimplePlay(TpCustInfo tpCustInfo, PayoutData_SimplePlay payoutData_SimplePlay, out decimal afterAmount)
        {
            int gameType = (int)GameType.SimplePlay_RNG;
            int timezone = 8;

            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                long timestamp = tools.ConvertLocTime(payoutData_SimplePlay.timestamp, timezone);
                long payouttime = tools.ConvertLocTime(payoutData_SimplePlay.Payouttime, timezone);
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                int ticketStatus = payoutData_SimplePlay.PayoutType;

                var p = new DynamicParameters(new
                {
                    username = payoutData_SimplePlay.username,
                    currency = payoutData_SimplePlay.currency,
                    amount = payoutData_SimplePlay.amount,
                    txnid = payoutData_SimplePlay.txnid,
                    timestamp = timestamp,
                    gametype = payoutData_SimplePlay.gametype,
                    gamecode = payoutData_SimplePlay.gamecode,
                    gameid = payoutData_SimplePlay.gameid,
                    Payouttime = payouttime,
                    JackpotWin = payoutData_SimplePlay.JackpotWin,
                    TransId = payoutData_SimplePlay.TransId,
                    JackpotContribution = 0,
                    txn_reverse_id = "",
                    ip = "",
                    platform = -1,
                    TicketType = (int)TransferType.Payout,
                    TicketStatus = ticketStatus,
                });

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_SimplePlay_Ins", p, trans);

                if (result > 0)
                {
                    if (payoutData_SimplePlay.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            payoutData_SimplePlay.amount,
                            (int)Tp.SimplePlay,
                            payoutData_SimplePlay.TransId,
                            gameType,
                            TpCategory.Slots,
                            payoutData_SimplePlay.TransId,
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

        public int CancelBetRNG_SimplePlay(TpCustInfo tpCustInfo, CancelBetData_SimplePlay cancelBetData_SimplePlay, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            int result = 0;

            int gameType = (int)GameType.SimplePlay_RNG;
            int timezone = 8;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                long timestamp = tools.ConvertLocTime(cancelBetData_SimplePlay.timestamp, timezone);
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
                var refBettrans = _partitionService.Execute(partitionTime, "Service_BettransRng_SimplePlay_CancelBet_Set", new { txnid = cancelBetData_SimplePlay.txn_reverse_id }, trans);
                if (refBettrans > 0)
                {
                    // cancel update
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(cancelBetData_SimplePlay.amount),
                        (int)Tp.SimplePlay,
                        cancelBetData_SimplePlay.txn_reverse_id,
                        gameType,
                        TpCategory.Slots,
                        cancelBetData_SimplePlay.TransId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "CancelBet",
                        out bonusId,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
                else
                {
                    // insert cancel
                    var p = new DynamicParameters(new
                    {
                        username = cancelBetData_SimplePlay.username,
                        currency = cancelBetData_SimplePlay.currency,
                        amount = cancelBetData_SimplePlay.amount,
                        txnid = cancelBetData_SimplePlay.txn_reverse_id,// 轉存對應placebet的txnid
                        timestamp = timestamp,
                        gametype = cancelBetData_SimplePlay.gametype,
                        gamecode = cancelBetData_SimplePlay.gamecode,
                        gameid = cancelBetData_SimplePlay.gameid,
                        txn_reverse_id = cancelBetData_SimplePlay.txnid,// 轉存cancelbet的txnid
                        TransId = cancelBetData_SimplePlay.TransId,
                        Payouttime = 0,
                        JackpotContribution = 0,
                        JackpotWin = false,
                        ip = "",
                        platform = -1,
                        TicketType = (int)TransferType.CancelBet,
                        TicketStatus = (int)TicketStatus.Refund,
                    });

                    var cancelInsert = _partitionService.Execute(partitionTime, "Service_BettransRng_SimplePlay_CancelBet_Ins", p, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                    result = -1;
                }
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }
            return result;
        }

        #endregion

        #region Skywind
        public int Settled_Skywind(TpCustInfo tpCustInfo, BettingData_Skywind bettingData_Skywind, int gameType, out decimal afterAmount)
        {
            var bonusId = 0;
            var result = 0;
            afterAmount = 0;
            var tpCategory = TpCategory.Slots;
            var TransId = $"{bettingData_Skywind.round_id}-{bettingData_Skywind.game_id}";
            try
            {
                switch (gameType)
                {
                    case (int)GameType.Skywind_LiveCasino:
                        tpCategory = TpCategory.LiveCasino;
                        break;
                    case (int)GameType.Skywind_Fish:
                        tpCategory = TpCategory.Fish;
                        break;
                }
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_Skywind.timestamp).DateTime;
                var query = new
                {
                    bettingData_Skywind.trx_id
                };
                var check = _partitionService.Query<BettransRng_Skywind>(partitionTime, "Service_Bettrans_Skywind_Check", query);
                if (check.Any() && check.Any(x => x.TicketStatus != (int)TicketStatus.UnSettled))
                {
                    return -1;
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                bettingData_Skywind.TicketStatus = bettingData_Skywind.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;

                switch (bettingData_Skywind.game_status)
                {
                    case "freegame":
                        bettingData_Skywind.TicketType = (int)BettransType.Bonus;
                        break;
                    case "bonusgame":
                        bettingData_Skywind.TicketType = (int)BettransType.Bonus;
                        break;
                    default:
                        bettingData_Skywind.TicketType = (int)BettransType.EndRound;
                        break;
                }

                result = _partitionService.Execute(partitionTime, "Service_Bettrans_Skywind_Ins", bettingData_Skywind, trans);

                if (result > 0 && bettingData_Skywind.amount > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingData_Skywind.amount,
                        (int)Tp.Skywind,
                        TransId,
                        gameType,
                        tpCategory,
                        TransId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans);
                }

                if (result > 0 && bettingData_Skywind.TicketType == (int)BettransType.EndRound)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.Skywind, (GameType)gameType, TransId, trans, partitionTime);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }

                logger.LogError(mySqlEx, mySqlEx.Message);
                throw;
            }

            return result;
        }

        public int CancelBet_Skywind(TpCustInfo tpCustInfo, CancelBetData_Skywind cancelBetData_Skywind, int gameType, out decimal afterAmount)
        {
            var result = 0;
            int bonusId = 0;
            afterAmount = 0;
            var tpCategory = gameType == (int)GameType.Skywind_RNG ? TpCategory.Slots : TpCategory.LiveCasino;
            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(cancelBetData_Skywind.timestamp).DateTime;
            var TransId = $"{cancelBetData_Skywind.roundid}-{cancelBetData_Skywind.gameid}";
            var query = new
            {
                trx_id = cancelBetData_Skywind.trxid
            };
            var bettransSkywind = _partitionService.Query<BettransRng_Skywind>(partitionTime, "Service_Bettrans_Skywind_Check", query)?.FirstOrDefault(x => x.TicketStatus == (int)TicketStatus.UnSettled);
            if (bettransSkywind == null)
            {
                return -2;
            }
            var para = new DynamicParameters(new
            {
                SQ = bettransSkywind.SQ
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            result = _partitionService.Execute(partitionTime, "Service_Bettrans_Skywind_CancelBet_Set", para, trans);


            if (result > 0)
            {
                if (bettransSkywind.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(bettransSkywind.amount),
                        (int)Tp.Skywind,
                        TransId,
                        gameType,
                        tpCategory,
                        TransId,
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
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            return result;
        }

        public SettleData_Skywind GetSettle_TpWager_Skywind(string transId)
        {
            return _wagerDbClient.QueryFirst<SettleData_Skywind>("Service_TpWager_Skywind_Settle_Get_V2", new { TransId = transId });
        }

        #endregion

        #region PlayTech

        public int Payout_PlayTech(TpCustInfo tpCustInfo, BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpCategory = (gameType == GameType.PlayTech_RNG) ? TpCategory.Slots : TpCategory.LiveCasino;
            var tableName = (gameType == GameType.PlayTech_RNG) ? "Rng" : "LC";
            // Service_BettransRng_PlayTech_roundId_Get、Service_BettransLC_PlayTech_roundId_Get
            var GetSpName = $"Service_Bettrans{tableName}_PlayTech_roundId_Get";
            // Service_BettransRng_PlayTech_Ins、Service_BettransLC_PlayTech_Ins
            var InsSpName = $"Service_Bettrans{tableName}_PlayTech_Ins";

            var cntTrans = bettingData_PlayTech.trans.Count;
            var cntExet = 0;
            int cntContinue = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (var data in bettingData_PlayTech.trans)
            {
                try
                {
                    var partitionTime = data.transTime;

                    // 檢查是否有下注
                    var bettrans = _partitionService.Query<BettransData_PlayTech>(partitionTime, GetSpName, new { roundId = data.roundId, playerId = bettingData_PlayTech.playerId }, partitionCheckDay);
                    if (!bettrans.Any())
                    {
                        // 沒有下注單
                        return -4;
                    }
                    else if (!bettrans.Any(i => i.TicketType == BettransType.Bet))
                    {
                        if (bettrans.Any(i => i.TicketType == BettransType.CancelBet))
                        {
                            // 沒有下注單，全部cancel: 與對方確認需要處理，在此情況下中獎金額將為0
                            if (data.amount != 0)
                            {
                                // 阻擋amount != 0
                                return -5;
                            }
                            // 處理 amount = 0
                        }
                        else
                        {
                            // 沒有下注單
                            return -4;
                        }
                    }
                    else if (bettrans.Any(i => i.TicketType == BettransType.Payout && i.transId == data.transId))
                    {
                        // 檢查是否已經收過該transId payout
                        cntExet++;
                        cntContinue++;
                        continue;
                    }

                    var p = new DynamicParameters(new
                    {
                        requestId = bettingData_PlayTech.requestId,
                        brandId = bettingData_PlayTech.brandId,
                        playerId = bettingData_PlayTech.playerId,
                        playerSessionId = bettingData_PlayTech.playerSessionId,
                        gameCode = bettingData_PlayTech.gameCode,
                        endSession = bettingData_PlayTech.endSession,
                        detailUrl = bettingData_PlayTech.detailUrl,
                        trans = JsonConvert.SerializeObject(bettingData_PlayTech.trans),
                        seq = data.seq,
                        transId = data.transId,
                        referenceId = data.referenceId,
                        amount = data.amount,
                        transType = data.transType,
                        transTime = data.transTime,
                        roundId = data.roundId,
                        roundType = data.roundType,
                        endRound = data.endRound,
                        desc = data.desc,
                        jpc = data.jpc,
                        jpw = data.jpw,
                        jpDetails = data.jpDetails != null && data.jpDetails.Any() ? JsonConvert.SerializeObject(data.jpDetails) : "[]",
                        validBet = data.validBet,
                        validWin = data.validWin,
                        additionalData = data.additionalData != null ? JsonConvert.SerializeObject(data.additionalData) : "[]",
                        transTimeUnix = data.transTimeUnix,
                        TicketType = (int)TransferType.Payout,
                        TicketStatus = (int)TicketStatus.UnSettled,
                        SettleFlag = (data.endRound == 1) ? (int)SettleFlag.EndGame : (int)SettleFlag.Running
                    });

                    result = 0;
                    result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                    if (result > 0)
                    {
                        if (data.amount != 0)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                data.amount,
                                (int)Tp.PlayTech,
                                data.roundId,
                                (int)gameType,
                                tpCategory,
                                data.roundId,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"Payout - {data.transId} ",
                                out bonusId,
                                conn,
                                trans
                            );
                        }

                        if (result > 0 && data.endRound == 1)
                        {
                            result = _tpUnsettleService.InsertTpUnsettle(Tp.PlayTech, gameType, data.roundId.ToString(), trans, partitionTime);
                        }
                    }

                    if (result > 0)
                    {
                        cntExet++;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    result = 0;
                    logger.LogError(mySqlEx.Message);
                    if (mySqlEx.Number == 1062)
                    {
                        /*Duplicate for key 'PRIMARY'*/
                        result = -1;
                        cntExet++;
                        cntContinue++;
                        continue;
                    }
                    throw;
                }
            }

            if (cntContinue == cntExet)
            {
                // 全為無需處理的Request
                return -1;
            }
            else if (cntExet == cntTrans)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                return result;
            }
            else if (cntExet != cntTrans && result > 0)
            {
                result = 0;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int CancelBet_PlayTech(TpCustInfo tpCustInfo, BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpCategory = (gameType == GameType.PlayTech_RNG) ? TpCategory.Slots : TpCategory.LiveCasino;
            var tableName = (gameType == GameType.PlayTech_RNG) ? "Rng" : "LC";
            // Service_BettransRng_PlayTech_roundId_Get、Service_BettransLC_PlayTech_roundId_Get
            var GetSpName = $"Service_Bettrans{tableName}_PlayTech_roundId_Get";
            // Service_BettransRng_PlayTech_CancelBet_Set、Service_BettransLC_PlayTech_CancelBet_Set
            var SetSpName = $"Service_Bettrans{tableName}_PlayTech_CancelBet_Set";
            // Service_BettransRng_PlayTech_Ins、Service_BettransLC_PlayTech_Ins
            var InsSpName = $"Service_Bettrans{tableName}_PlayTech_Ins";

            var cntTrans = bettingData_PlayTech.trans.Count;
            var cntExet = 0;
            int cntContinue = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (var data in bettingData_PlayTech.trans)
            {
                try
                {
                    var partitionTime = data.transTime;

                    // 檢查是否有下注
                    var bettrans = _partitionService.Query<BettransData_PlayTech>(partitionTime, GetSpName, new { roundId = data.roundId, playerId = bettingData_PlayTech.playerId }, partitionCheckDay);
                    if (bettrans.Any(i => i.TicketType == BettransType.Bet && i.transId == data.referenceId))
                    {
                        // 取得下注單的時間 避免跨月抓不到
                        var betTicket = bettrans.First(i => i.TicketType == BettransType.Bet && i.transId == data.referenceId);
                        partitionTime = betTicket.transTime;
                        // update cancel
                        var p = new DynamicParameters(new
                        {
                            referenceId = data.referenceId,
                            playerId = bettingData_PlayTech.playerId,
                        });
                        result = 0;
                        result = _partitionService.Execute(partitionTime, SetSpName, p, trans);

                        if (result > 0)
                        {
                            result = PayoutV2(
                                tpCustInfo,
                                Math.Abs(data.amount),
                                (int)Tp.PlayTech,
                                data.roundId,
                                (int)gameType,
                                tpCategory,
                                data.roundId,
                                TransType.Refunds,
                                null,
                                out afterAmount,
                                $"CancelBet - {data.referenceId} ",
                                out bonusId,
                                conn,
                                trans
                            );

                            if (result > 0 && data.endRound == 1)
                            {
                                result = _tpUnsettleService.InsertTpUnsettle(Tp.PlayTech, gameType, data.roundId.ToString(), trans, partitionTime);
                            }
                        }
                    }
                    else if (!bettrans.Any(i => i.TicketType == BettransType.Bet) || !bettrans.Any(i => i.TicketType == BettransType.Bet && i.transId == data.referenceId))
                    {
                        // insert cancel
                        var p = new DynamicParameters(new
                        {
                            requestId = bettingData_PlayTech.requestId,
                            brandId = bettingData_PlayTech.brandId,
                            playerId = bettingData_PlayTech.playerId,
                            playerSessionId = bettingData_PlayTech.playerSessionId,
                            gameCode = bettingData_PlayTech.gameCode,
                            endSession = bettingData_PlayTech.endSession,
                            detailUrl = bettingData_PlayTech.detailUrl,
                            trans = JsonConvert.SerializeObject(bettingData_PlayTech.trans),
                            seq = data.seq,
                            transId = data.transId,
                            referenceId = data.referenceId,
                            amount = data.amount,
                            transType = data.transType,
                            transTime = data.transTime,
                            roundId = data.roundId,
                            roundType = data.roundType,
                            endRound = data.endRound,
                            desc = data.desc,
                            jpc = data.jpc,
                            jpw = data.jpw,
                            jpDetails = data.jpDetails != null && data.jpDetails.Any() ? JsonConvert.SerializeObject(data.jpDetails) : "[]",
                            validBet = data.validBet,
                            validWin = data.validWin,
                            additionalData = data.additionalData != null ? JsonConvert.SerializeObject(data.additionalData) : "[]",
                            transTimeUnix = data.transTimeUnix,
                            TicketType = (int)TransferType.CancelBet,
                            TicketStatus = (int)TicketStatus.Refund,
                            SettleFlag = (int)SettleFlag.Settled
                        });

                        result = 0;
                        result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                        if (result > 0)
                        {
                            result = -2;
                        }
                    }

                    if (result > 0 || result == -2)
                    {
                        cntExet++;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    result = 0;
                    logger.LogError(mySqlEx.Message);
                    if (mySqlEx.Number == 1062)
                    {
                        /*Duplicate for key 'PRIMARY'*/
                        result = -1;
                        cntExet++;
                        cntContinue++;
                        continue;
                    }
                    throw;
                }
            }

            if (cntContinue == cntExet)
            {
                // 全為無需處理的Request
                return -1;
            }
            else if (cntExet == cntTrans)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                return result;
            }
            else if (cntExet != cntTrans && result > 0)
            {
                result = 0;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int BetAndPayout_PlayTech(TpCustInfo tpCustInfo, BettingData_PlayTech bettingData_PlayTech, GameType gameType, int partitionCheckDay, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            var tpCategory = (gameType == GameType.PlayTech_RNG) ? TpCategory.Slots : TpCategory.LiveCasino;
            var tableName = (gameType == GameType.PlayTech_RNG) ? "Rng" : "LC";
            // Service_BettransRng_PlayTech_Ins、Service_BettransLC_PlayTech_Ins
            var InsSpName = $"Service_Bettrans{tableName}_PlayTech_Ins";
            // Service_BettransRng_PlayTech_roundId_Get、Service_BettransLC_PlayTech_roundId_Get
            var GetSpName = $"Service_Bettrans{tableName}_PlayTech_roundId_Get";

            var cntTrans = bettingData_PlayTech.trans.Count;
            var cntExet = 0;
            int cntContinue = 0;
            Dictionary<string, int> verifyBonus = new();
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            var trans = conn.BeginTransaction();

            // 同一rouond
            foreach (var data in bettingData_PlayTech.trans)
            {
                try
                {
                    result = 0;
                    int bonusId = 0;

                    var partitionTime = data.transTime;
                    var p = new DynamicParameters(new
                    {
                        requestId = bettingData_PlayTech.requestId,
                        brandId = bettingData_PlayTech.brandId,
                        playerId = bettingData_PlayTech.playerId,
                        playerSessionId = bettingData_PlayTech.playerSessionId,
                        gameCode = bettingData_PlayTech.gameCode,
                        endSession = bettingData_PlayTech.endSession,
                        detailUrl = bettingData_PlayTech.detailUrl,
                        trans = JsonConvert.SerializeObject(bettingData_PlayTech.trans),
                        seq = data.seq,
                        transId = data.transId,
                        referenceId = data.referenceId,
                        amount = data.amount,
                        transType = data.transType,
                        transTime = data.transTime,
                        roundId = data.roundId,
                        roundType = data.roundType,
                        endRound = data.endRound,
                        desc = data.desc,
                        jpc = data.jpc,
                        jpw = data.jpw,
                        jpDetails = data.jpDetails != null && data.jpDetails.Any() ? JsonConvert.SerializeObject(data.jpDetails) : "[]",
                        validBet = data.validBet,
                        validWin = data.validWin,
                        additionalData = data.additionalData != null ? JsonConvert.SerializeObject(data.additionalData) : "[]",
                        transTimeUnix = data.transTimeUnix,
                        TicketStatus = (int)TicketStatus.UnSettled
                    });

                    if (string.Equals(data.transType, "bet", StringComparison.OrdinalIgnoreCase))
                    {
                        var bettrans = _partitionService.Query<BettransData_PlayTech>(partitionTime, GetSpName, new { roundId = data.roundId, playerId = bettingData_PlayTech.playerId }, partitionCheckDay);
                        if (bettrans.Any(i => i.TicketType == BettransType.Bet && i.transId == data.transId))
                        {
                            // 檢查是否已經收過該transId bet
                            cntExet++;
                            cntContinue++;
                            continue;
                        }

                        p.Add("TicketType", (int)TransferType.PlaceBet);
                        p.Add("SettleFlag", (int)SettleFlag.Running);
                        result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                        if (result > 0)
                        {
                            result = _bettingService.PlaceBet(
                                tpCustInfo,
                                Math.Abs(data.amount) * -1,
                                (int)Tp.PlayTech,
                                data.roundId,
                                (int)gameType,
                                data.roundId,
                                out afterAmount,
                                ref bonusId,
                                out decimal useBonus,
                                conn,
                                trans,
                                memo: $" - {data.transId} "
                            );

                            if (bonusId > 0)
                            {
                                verifyBonus.Add(data.roundId, bonusId);
                                _bettingService.AddTpBonus((int)Tp.PlayTech, data.transId, (int)gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, useBonus, bonusId, trans);
                            }
                        }
                    }
                    else if (string.Equals(data.transType, "win", StringComparison.OrdinalIgnoreCase))
                    {
                        var bettrans = _partitionService.Query<BettransData_PlayTech>(partitionTime, GetSpName, new { roundId = data.roundId, playerId = bettingData_PlayTech.playerId }, partitionCheckDay);
                        if (bettrans.Any(i => i.TicketType == BettransType.Payout && i.transId == data.transId))
                        {
                            // 檢查是否已經收過該transId payout
                            cntExet++;
                            cntContinue++;
                            continue;
                        }

                        p.Add("TicketType", (int)TransferType.Payout);
                        p.Add("SettleFlag", data.endRound == 1 ? (int)SettleFlag.EndGame : (int)SettleFlag.Running);
                        result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                        if (result > 0)
                        {
                            if (data.amount != 0)
                            {
                                if (verifyBonus.ContainsKey(data.roundId))
                                {
                                    result = PayoutWithBonusId(
                                        tpCustInfo,
                                        data.amount,
                                        (int)Tp.PlayTech,
                                        data.roundId,
                                        (int)gameType,
                                        tpCategory,
                                        data.roundId,
                                        TransType.Payout,
                                        out afterAmount,
                                        $"Payout - {data.transId} ",
                                        verifyBonus[data.roundId],
                                        conn,
                                        trans
                                    );
                                }
                                else
                                {
                                    result = PayoutV2(
                                        tpCustInfo,
                                        data.amount,
                                        (int)Tp.PlayTech,
                                        data.roundId,
                                        (int)gameType,
                                        tpCategory,
                                        data.roundId,
                                        TransType.Payout,
                                        null,
                                        out afterAmount,
                                        $"Payout - {data.transId} ",
                                        out bonusId,
                                        conn,
                                        trans
                                    );
                                }
                            }
                        }

                        if (result > 0 && data.endRound == 1)
                        {
                            result = _tpUnsettleService.InsertTpUnsettle(Tp.PlayTech, gameType, data.roundId.ToString(), trans, partitionTime);
                        }
                    }

                    if (result > 0)
                    {
                        cntExet++;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    result = 0;
                    logger.LogError(mySqlEx.Message);
                    if (mySqlEx.Number == 1062)
                    {
                        /*Duplicate for key 'PRIMARY'*/
                        result = -1;
                        cntExet++;
                        cntContinue++;
                        continue;
                    }
                    throw;
                }
            }

            try
            {
                if (cntContinue == cntExet)
                {
                    // 全為無需處理的Request
                    return -1;
                }
                else if (cntExet == cntTrans)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    if (verifyBonus.Any())
                    {
                        //call Service_TpSeamlessBonus_Get to set flag
                        foreach (var bonusTransId in verifyBonus)
                        {
                            GetTpBonus((int)Tp.PlayTech, bonusTransId.Key, (int)gameType, tpCategory, out _, out _);
                        }
                    }
                    return result;
                }
                else if (cntExet != cntTrans && result > 0)
                {
                    result = 0;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int Reward_PlayTech(TpCustInfo tpCustInfo, RewardData_PlayTech rewardData_PlayTech, out decimal afterAmount)
        {
            afterAmount = 0;
            var InsSpName = $"Service_Reward_PlayTech_Ins";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result;
            try
            {
                var p = new DynamicParameters(new
                {
                    requestId = rewardData_PlayTech.requestId,
                    brandId = rewardData_PlayTech.brandId,
                    playerId = rewardData_PlayTech.playerId,
                    transId = rewardData_PlayTech.transId,
                    type = rewardData_PlayTech.type,
                    referenceId = rewardData_PlayTech.referenceId,
                    referenceName = rewardData_PlayTech.referenceName,
                    currency = rewardData_PlayTech.currency,
                    amount = rewardData_PlayTech.amount,
                    transTime = rewardData_PlayTech.transTime,
                    desc = rewardData_PlayTech.desc,
                    additionalData = rewardData_PlayTech.additionalData != "" ? JsonConvert.SerializeObject(rewardData_PlayTech.additionalData) : "[]",
                    transTimeUnix = rewardData_PlayTech.transTimeUnix,
                    TicketType = (int)TransferType.Payout,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                result = _mainDbClient.Execute(InsSpName, p, conn, trans);
                if (result > 0)
                {
                    if (rewardData_PlayTech.amount != 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            rewardData_PlayTech.amount,
                            (int)Tp.PlayTech,
                            TransType.Prize,
                            $"Prize: {Math.Abs(rewardData_PlayTech.amount):#,##0.##}",
                            rewardData_PlayTech.transId,
                            out afterAmount,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        result = _tpUnRewardService.InsertTpUnReward(Tp.PlayTech, GameType.SG_RNG, rewardData_PlayTech.transId, rewardData_PlayTech.playerId, trans);
                    }
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    result = -1;
                    return result;
                }
                throw;
            }
            return result;
        }

        public TpBetFailureRecord GetTpBetFailureParam_PlayTech(TpCustInfo tpCustInfo, TpCustBalance custBalance, TpCurrency tpCurrency, BettingData_PlayTech bettingData_PlayTech, int gameType, TpCheckRecordCode Code, decimal AfterAmount)
        {
            return _bettingService.GetTpBetFailureParam_PlayTech(tpCustInfo, custBalance, tpCurrency, bettingData_PlayTech, gameType, Code, AfterAmount);
        }

        #endregion

        #region Nextspin

        public async Task<NextspinResult> PayoutRNG_Nextspin(TpCustInfo tpCustInfo, BettingDataRNG_Nextspin bettingData)
        {
            var afterAmount = 0m;
            using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            try
            {
                var partitionTime = DateTime.UtcNow;
                bettingData.TicketType = (int)BettransType.Payout;
                bettingData.TicketStatus = (int)bettingData.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
                var p = new DynamicParameters(bettingData);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Nextspin_Ins", p, trans);
                var sq = p.Get<long>("SQ");
                if (sq == 0)
                    throw new NextspinException(NextspinErrorCode.SystemError);

                // update ticketId
                result = _partitionService.Execute(partitionTime, "Service_BettransRng_Nextspin_Set", new { ticketId = bettingData.ticketId, referenceId = bettingData.referenceId }, trans);
                if (result < 1)
                    throw new NextspinException(NextspinErrorCode.RecordIdNotFound);

                if (bettingData.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingData.amount,
                        (int)Tp.Nextspin,
                        bettingData.referenceId,
                        (int)GameType.Nextspin_RNG,
                        TpCategory.Slots,
                        bettingData.transferId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        "Payout",
                        out var bonusId,
                        conn,
                        trans
                    );
                    if (result != 1)
                        throw new NextspinException(NextspinErrorCode.InsufficientBalance);

                }
                result = _tpUnsettleService.InsertTpUnsettle(Tp.Nextspin, GameType.Nextspin_RNG, bettingData.ticketId, trans, partitionTime);
                if (result < 1)
                    throw new NextspinException(NextspinErrorCode.SystemError);

                await trans.CommitAsync();
                return new NextspinResult()
                {
                    Result = 1,
                    SQ = sq.ToString(),
                    AfterAmount = afterAmount
                };
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    throw new NextspinException(NextspinErrorCode.DuplicatedReferenceId);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await trans.RollbackAsync();
                throw new NextspinException(NextspinErrorCode.SystemError);
            }
        }
        public async Task<NextspinResult> CancelBetRNG_Nextspin(TpCustInfo tpCustInfo, BettingDataRNG_Nextspin bettingData)
        {
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            try
            {
                var partitionTime = DateTime.UtcNow;
                bettingData.TicketType = (int)BettransType.CancelBet;
                bettingData.TicketStatus = (int)TicketStatus.Refund;
                var p = new DynamicParameters(bettingData);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Nextspin_Ins", p, trans);
                var sq = p.Get<long>("SQ");
                if (sq == 0)
                    throw new NextspinException(NextspinErrorCode.SystemError);

                result = PayoutV2(
                    tpCustInfo,
                    bettingData.amount,
                    (int)Tp.Nextspin,
                    bettingData.referenceId,
                    (int)GameType.Nextspin_RNG,
                    TpCategory.Slots,
                    bettingData.transferId,
                    TransType.Refunds,
                    null,
                    out var afterAmount,
                    "Cancel",
                    out var bonusId,
                    conn,
                    trans);
                if (result < 1)
                    throw new NextspinException(NextspinErrorCode.InsufficientBalance);

                await trans.CommitAsync();
                return new NextspinResult()
                {
                    Result = 1,
                    SQ = sq.ToString(),
                    AfterAmount = afterAmount
                };
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    throw new NextspinException(NextspinErrorCode.DuplicatedReferenceId);
                }
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                await trans.RollbackAsync();
                throw new NextspinException(NextspinErrorCode.SystemError);
            }
        }

        #endregion

        #region Endorphina

        public int CancelBetRNG_Endorphina(TpCustInfo tpCustInfo, CancelBetData_Endorphina cancelBetData_Endorphina, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            int result = 0;

            var tpId = Tp.Endorphina;
            var tpCategory = TpCategory.Slots;
            var SetSpName = "Service_BettransRng_Endorphina_CancelBet_Set";
            var InsSpName = "Service_BettransRng_Endorphina_Ins";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(cancelBetData_Endorphina.date).DateTime;
                var refBettrans = _partitionService.Execute(partitionTime, SetSpName, new { id = cancelBetData_Endorphina.id, player = cancelBetData_Endorphina.player }, trans);
                if (refBettrans > 0)
                {
                    // cancel update
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(cancelBetData_Endorphina.amount),
                        (int)tpId,
                        cancelBetData_Endorphina.id.ToString(),
                        gameType,
                        tpCategory,
                        cancelBetData_Endorphina.id.ToString(),
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        $"CancelBet",
                        out bonusId,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    // insert cancel
                    var p = new DynamicParameters(new
                    {
                        TransId = cancelBetData_Endorphina.id,
                        amount = cancelBetData_Endorphina.amount,
                        currency = cancelBetData_Endorphina.currency,
                        date = cancelBetData_Endorphina.date,
                        game = cancelBetData_Endorphina.game,
                        gameId = cancelBetData_Endorphina.gameId,
                        id = cancelBetData_Endorphina.id,
                        player = cancelBetData_Endorphina.player,
                        token = cancelBetData_Endorphina.token,
                        betSessionId = "",
                        betTransactionId = 0,
                        progressive = false,
                        progressiveDesc = "",
                        TicketType = (int)BettransType.CancelBet,
                        TicketStatus = (int)TicketStatus.Refund,
                        SettleFlag = (int)SettleFlag.Settled
                    });

                    var cancelInsert = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                    if (cancelInsert > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int PayoutRNG_Endorphina(TpCustInfo tpCustInfo, PayoutData_Endorphina payoutData_Endorphina, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.Endorphina;
            var tpCategory = TpCategory.Slots;
            var GetSpName = "Service_BettransRng_Endorphina_id_Get";
            var InsSpName = "Service_BettransRng_Endorphina_Ins";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(payoutData_Endorphina.date).DateTime;

                var bettrans = _partitionService.Query<BettransData_Endorphina>(partitionTime, GetSpName, new { id = payoutData_Endorphina.id, player = payoutData_Endorphina.player });
                if (bettrans.Any(i => (i.TicketType == BettransType.Payout || i.TicketType == BettransType.Jackpot) && i.id == payoutData_Endorphina.id /*&& i.gameId == payoutData_Endorphina.gameId*/))
                {
                    // 檢查是否已經收過該transId payout
                    return -1;
                }

                // Yes, betTransactionId is present for all wins.
                var TransId = Convert.ToInt64(payoutData_Endorphina.betTransactionId);
                if (TransId == 0)
                {
                    return -6;
                }
                // id取代
                if (payoutData_Endorphina.id == 0)
                {
                    payoutData_Endorphina.id = TransId * -1;
                }

                var progressive = payoutData_Endorphina.progressive == null ? false : (bool)payoutData_Endorphina.progressive;
                var betTransactionId = payoutData_Endorphina.betTransactionId == null ? "0" : payoutData_Endorphina.betTransactionId;

                var p = new DynamicParameters(new
                {
                    TransId = TransId,
                    amount = payoutData_Endorphina.amount,
                    currency = payoutData_Endorphina.currency,
                    date = payoutData_Endorphina.date,
                    game = payoutData_Endorphina.game,
                    gameId = payoutData_Endorphina.gameId,
                    id = payoutData_Endorphina.id,
                    player = payoutData_Endorphina.player,
                    token = payoutData_Endorphina.token,
                    betSessionId = payoutData_Endorphina.betSessionId,
                    betTransactionId = betTransactionId,
                    progressive = progressive,
                    progressiveDesc = payoutData_Endorphina.progressiveDesc,
                    TicketType = progressive ? (int)BettransType.Jackpot : (int)BettransType.Payout,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                if (result > 0 && payoutData_Endorphina.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        payoutData_Endorphina.amount,
                        (int)tpId,
                        TransId.ToString(),
                        gameType,
                        tpCategory,
                        TransId.ToString(),
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout - {payoutData_Endorphina.id} ",
                        out bonusId,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    result = _tpUnsettleService.InsertTpUnsettle(tpId, (GameType)gameType, TransId.ToString(), trans, partitionTime);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int EndSessionRNG_Endorphina(TpCustInfo tpCustInfo, EndSessionData_Endorphina endSessionData_Endorphina, int gameType, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var InsSpName = "Service_BettransRng_EndSession_Endorphina_Ins";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTime.Now;

                var p = new DynamicParameters(new
                {
                    bet = endSessionData_Endorphina.bet,
                    bonusBet = endSessionData_Endorphina.bonusBet,
                    bonusGames = endSessionData_Endorphina.bonusGames,
                    bonusWin = endSessionData_Endorphina.bonusWin,
                    currency = endSessionData_Endorphina.currency,
                    game = endSessionData_Endorphina.game,
                    games = endSessionData_Endorphina.games,
                    jackpotWin = endSessionData_Endorphina.jackpotWin,
                    jackpots = endSessionData_Endorphina.jackpots,
                    player = endSessionData_Endorphina.player,
                    token = endSessionData_Endorphina.token,
                    win = endSessionData_Endorphina.win,
                    TicketType = (int)BettransType.EndRound,
                    TicketStatus = (int)TicketStatus.Related,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return result;
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        #endregion

        #region PNG

        public async Task<PNGResult> CancelBetRNG_PNG(TpCustInfo tpCustInfo, BettingDataRNG_PNG bettingData)
        {
            decimal afterAmount = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                // update
                var result = _mainDbClient.Execute("Service_BettransRng_PNG_CancelBet_Set", new { _TransactionId = bettingData.TransactionId }, conn, trans);
                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingData.Amount,
                        (int)Tp.PNG,
                        bettingData.RoundId.ToString(),
                        (int)GameType.PNG_RNG,
                        TpCategory.Slots,
                        bettingData.RoundId.ToString(),
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "Cancel",
                        out var bonusId,
                        conn,
                        trans);

                    if (result < 1)
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "CancelReserve Set Balance Fail");
                }
                else
                {
                    // Insert
                    var p = new DynamicParameters(bettingData);
                    result = _mainDbClient.Execute("Service_BettransRng_PNG_Ins", p, conn, trans);

                    if (result < 1)
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "CancelReserve Insert Fail");
                }

                await trans.CommitAsync();
                return new PNGResult
                {
                    ExternalTransactionId = bettingData.TransactionId,
                    AfterAmount = afterAmount //沒有退款的話 afterAmount 會是 0，有需要可以在外層判斷 = 0 重取餘額一次
                };
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    throw new PNGException(PNG_StatsuCodeEnum.Ok, "Duplicate CancelReserve");
                }
                throw;
            }
            catch (Exception ex)
            {
                await trans.RollbackAsync();
                logger.LogError(ex, ex.Message);
                throw new PNGException(PNG_StatsuCodeEnum.Internal, ex.Message);
            }
        }

        public async Task<PNGResult> PayoutRNG_PNG(TpCustInfo tpCustInfo, BettingDataRNG_PNG bettingData, TransType transType)
        {
            var afterAmount = 0m;
            long endRoundId = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();

            try
            {
                var p = new DynamicParameters(bettingData);
                var result = bettingData.RoundId != endRoundId
                    ? _mainDbClient.Execute("Service_BettransRng_PNG_Ins", p, conn, trans) // 正常投注、取消、派彩
                    : _mainDbClient.Execute("Service_BettransRng_PNG_END_Ins", p, conn, trans); // EndRound 紀錄

                if (result == 0)
                    throw new PNGException(PNG_StatsuCodeEnum.Internal, "Release Fail");

                if (bettingData.Amount > 0 && bettingData.RoundId != endRoundId)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingData.Amount,
                        (int)Tp.PNG,
                        bettingData.RoundId.ToString(),
                        (int)GameType.PNG_RNG,
                        TpCategory.Slots,
                        bettingData.RoundId.ToString(),
                        transType,
                        null,
                        out afterAmount,
                        "Payout",
                        out var bonusId,
                        conn,
                        trans);

                    if (result < 1)
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "Release Set Balance Fail");
                }

                if (bettingData.RoundId != endRoundId) //非endRound 才塞 TpUnsettle
                {
                    result = _tpUnsettleService.InsertTpUnsettle(Tp.PNG, GameType.PNG_RNG, bettingData.RoundId.ToString(), trans);

                    if (result < 1)
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "PNG TpUnsettle Fail");
                }

                await trans.CommitAsync();

                return new PNGResult
                {
                    ExternalTransactionId = bettingData.TransactionId,
                    AfterAmount = afterAmount
                };
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    throw new PNGException(PNG_StatsuCodeEnum.Ok, $"TransactionId:{bettingData.TransactionId},RoundId:{bettingData.RoundId} Duplicate Release");
                }
                throw;
            }
            catch (PNGException ex)
            {
                logger.LogError(ex, ex.Message);
                await trans.RollbackAsync();
                throw new PNGException(PNG_StatsuCodeEnum.Internal, ex.Message);
            }
        }

        #endregion

        #region Amigo

        public int CancelBetRNG_Amigo(TpCustInfo tpCustInfo, BettingData_Amigo bettingData_Amigo)
        {
            int bonusId = 0;
            int result = 0;

            var tpId = Tp.Amigo;
            var tpGameType = GameType.Amigo_RNG;
            var tpCategory = TpCategory.Slots;
            var SetSpName = "Service_BettransRng_Amigo_CancelBet_Set";
            var InsSpName = "Service_BettransRng_Amigo_Ins";
            var GetSpName = "Service_BettransRng_Amigo_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingData_Amigo.timestamp).DateTime;

                var p = new DynamicParameters(bettingData_Amigo);
                p.Add("TicketStatus", (int)TicketStatus.Refund);
                p.Add("SettleFlag", (int)SettleFlag.Settled);
                p.Add("jackpotId", 0);

                result = _partitionService.Execute(partitionTime, SetSpName, p, trans);
                if (result > 0)
                {
                    if (bettingData_Amigo.amount != 0 && string.IsNullOrEmpty(bettingData_Amigo.bonusCode))
                    {
                        // bonuscode bet/refund 無須異動額度
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(bettingData_Amigo.amount),
                            (int)tpId,
                            bettingData_Amigo.roundId.ToString(),
                            (int)tpGameType,
                            tpCategory,
                            bettingData_Amigo.roundId.ToString(),
                            TransType.Refunds,
                            null,
                            out _,
                            "CancelBet",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    var check = _partitionService.Query<BettingData_Amigo>(partitionTime, GetSpName, new { roundId = bettingData_Amigo.roundId });
                    if (!check.Any() || !check.Any(x => x.reference == bettingData_Amigo.reference && x.TicketType == (int)BettransType.CancelBet))
                    {
                        var cancelInsert = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                        if (cancelInsert > 0)
                        {
                            // insert cancel record
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                            return 1;
                        }
                    }

                    var subChk = check.Where(x => x.TicketType == 2);
                    if (subChk.Any())
                    {
                        // had cancel
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return -1;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int PayoutRNG_Amigo(TpCustInfo tpCustInfo, PayoutData_Amigo payoutData_Amigo, out decimal afterAmount)
        {
            afterAmount = 0;
            var tpId = Tp.Amigo;
            var tpGameType = GameType.Amigo_RNG;
            var tpCategory = TpCategory.Slots;
            var InsSpName = "Service_BettransRng_Amigo_Ins";
            var GetSpName = "Service_BettransRng_Amigo_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result;
            try
            {
                var bonusCode = (!string.IsNullOrEmpty(payoutData_Amigo.bonusCode)) ? $" ({payoutData_Amigo.bonusCode})" : "";
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(payoutData_Amigo.timestamp).DateTime;
                var p = new DynamicParameters(payoutData_Amigo);
                p.Add("SettleFlag", (int)SettleFlag.EndGame);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);

                var check = _partitionService.Query<BettingData_Amigo>(partitionTime, GetSpName, new { roundId = payoutData_Amigo.roundId });
                if (!check.Any())
                {
                    // no bet or cancel data
                    return -2;
                }

                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                if (result > 0)
                {
                    int bonusId = 0;

                    if (payoutData_Amigo.amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(payoutData_Amigo.amount),
                            (int)tpId,
                            payoutData_Amigo.roundId.ToString(),
                            (int)tpGameType,
                            tpCategory,
                            payoutData_Amigo.roundId.ToString(),
                            TransType.Payout,
                            null,
                            out afterAmount,
                            $"Payout{bonusCode}",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(tpId, tpGameType, payoutData_Amigo.roundId.ToString(), trans, partitionTime);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
        }

        public int BonusWinRNG_Amigo(TpCustInfo tpCustInfo, PayoutData_Amigo payoutData_Amigo, out decimal afterAmount)
        {
            afterAmount = 0;
            var InsSpName = "Service_BettransRng_Amigo_Ins";
            var GetSpName = "Service_BettransRng_Amigo_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            int result;
            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(payoutData_Amigo.timestamp).DateTime;
                var p = new DynamicParameters(payoutData_Amigo);
                p.Add("SettleFlag", (int)SettleFlag.Settled);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);

                var check = _partitionService.Query<BettingData_Amigo>(partitionTime, GetSpName, new { roundId = payoutData_Amigo.roundId });
                if (!check.Any())
                {
                    // no bet or cancel data
                    return -2;
                }

                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
        }

        #endregion

        #region DragonGaming

        public int CancelBetRNG_DragonGaming(TpCustInfo tpCustInfo, BettingData_DragonGaming refundData, string originalTransactionId, out decimal afterAmount)
        {
            int bonusId = 0;
            int result = 0;
            afterAmount = 0;

            var tpId = Tp.DragonGaming;
            var tpGameType = GameType.DragonGaming_RNG;
            var tpCategory = TpCategory.Slots;
            var SetSpName = "Service_BettransRng_DragonGaming_CancelBet_Set";
            var InsSpName = "Service_BettransRng_DragonGaming_Ins";
            var GetSpName = "Service_BettransRng_DragonGaming_Cancel_Exist_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(refundData);
                p.Add("SettleFlag", (int)SettleFlag.Settled);

                var check = _mainDbClient.QueryFirst<BettingData_DragonGaming>(GetSpName, trans, new { refundData.round_id, TicketType = (int)BettransType.Bet, transaction_id = originalTransactionId });

                if (check is not null)
                {
                    result = _mainDbClient.Execute(SetSpName, new { refundData.round_id, refundData.note }, conn, trans);

                    if (result > 0 && refundData.amount != 0)
                    {
                        // bonuscode bet/refund 無須異動額度
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(refundData.amount),
                            (int)tpId,
                            refundData.round_id,
                            (int)tpGameType,
                            tpCategory,
                            refundData.round_id,
                            TransType.Refunds,
                            null,
                            out _,
                            "CancelBet",
                            out bonusId,
                            conn,
                            trans
                        );
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return result;
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return result;
                    }
                }
                else
                {
                    var cancelcheck = _mainDbClient.QueryFirst<BettingData_DragonGaming>(GetSpName, trans, new { refundData.round_id, TicketType = (int)BettransType.CancelBet, transaction_id = originalTransactionId });

                    if (cancelcheck is not null)
                    {
                        // had cancel
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        return 1;
                    }

                    var cancelInsertResult = _mainDbClient.Execute(InsSpName, p, conn, trans);
                    if (cancelInsertResult > 0)
                    {
                        // insert cancel record
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        return cancelInsertResult;
                    }
                }

            }
            catch (MySqlException mySqlEx)
            {
                result = 0;
                logger.LogError(mySqlEx.Message);
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            return result;
        }

        public int PayoutRNG_DragonGaming(TpCustInfo tpCustInfo, BettingData_DragonGaming payoutData, out decimal afterAmount)
        {
            int bonusId = 0;
            int result = 0;
            afterAmount = 0;

            var tpId = Tp.DragonGaming;
            var tpGameType = GameType.DragonGaming_RNG;
            var tpCategory = TpCategory.Slots;
            var InsSpName = "Service_BettransRng_DragonGaming_Ins";
            var GetSpName = "Service_BettransRng_DragonGaming_Exist_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var p = new DynamicParameters(payoutData);
                p.Add("SettleFlag", (int)SettleFlag.EndGame);

                var check = _mainDbClient.QueryFirst<BettingData_DragonGaming>(GetSpName, trans, new { payoutData.round_id });

                if (check is null)
                {
                    // no bet or cancel data
                    return -2;
                }

                result = _mainDbClient.Execute(InsSpName, p, conn, trans);

                if (result > 0)
                {
                    if (payoutData.amount != 0)
                    {
                        result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(payoutData.amount),
                        (int)tpId,
                        payoutData.round_id,
                        (int)tpGameType,
                        tpCategory,
                        payoutData.round_id,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans
                        );
                    }

                    if (result > 0 && payoutData.round_end_state)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(tpId, tpGameType, payoutData.round_id, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return -1;
                }
                throw;
            }

            return result;
        }

        public int SetSettleFlag_DragonGaming(string transId)
        {
            return _mainDbClient.Execute("Service_BettransRng_DragonGaming_Settle_Set", commandType: CommandType.StoredProcedure, new { TransId = transId });
        }
		#endregion
        
        #region PG2
        
        public bool InsertBettrans_PG2(BettingDataRNG_PG2 data, MySqlTransaction trans)
        {
            var result = _mainDbClient.Execute("Service_BettransRng_PG2_Ins", trans, data);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool SetBalance_PG2(TpCustInfo tpCustInfo, BettingDataRNG_PG2 data, TransType transType, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = 0;
            data.amount = data.amount + data.betAmount;//用winloss+bet反推payout
            if (bonusId > 0)
            {
                result = PayoutWithBonusId(
                    tpCustInfo, 
                    data.amount, 
                    (int)Tp.PG2, 
                    data.roundId, 
                    (int)GameType.PG2_RNG,
                    TpCategory.Slots, 
                    data.refId, 
                    transType, 
                    out afterAmount, 
                    memo, 
                    bonusId, 
                    conn, 
                    trans
                );
            }
            else
            {
                result = PayoutV2(
                    tpCustInfo, 
                    data.amount, 
                    (int)Tp.PG2, 
                    data.roundId, 
                    (int)GameType.PG2_RNG,
                    TpCategory.Slots, 
                    data.refId, 
                    transType, 
                    null,
                    out afterAmount, 
                    memo, 
                    out bonusId, 
                    conn, 
                    trans
                );
            }
            return result != 0;
        }

        public bool CancelBettrans_PG2(int sq, MySqlTransaction trans)
        {
            var query = new { SQ = sq, TicketType = BettransType.CancelBet, TicketStatus = TicketStatus.Refund };
            var result = _mainDbClient.Execute("Service_BettransRng_PG2_CancelBet_Set",trans,query);

            if (result == 0)
            {
                return false;
            }
            return true;
        }

        public bool InsertBettrans_BetAndPayout_PG2(BettingDataRNG_PG2 data, MySqlTransaction trans)
        {
            var result = _mainDbClient.Execute("Service_BettransRng_PG2_BetAndPayout_Ins", trans, data);
            return result != 0;
        }

        public bool InsertUnsettle_PG2(DateTime partitionTime, string transactionId, MySqlTransaction trans)
        {
            var result = _tpUnsettleService.InsertTpUnsettle(Tp.PG2, GameType.PG2_RNG, transactionId, trans, partitionTime);

            if (result == 0)
            {
                return false;
            }
            return true;
        }
        #endregion
    }
}
