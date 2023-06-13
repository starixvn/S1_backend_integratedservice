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
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {

        #region GPI

        public int PlaceBetRNG_GPI(TpCustInfo tpCustInfo, BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string Transid)
        {
            int bonusId = 0;
            int gameType = (int)GameType.GPI_RNG;

            afterAmount = 0;
            Transid = bettingDataRNG_GPI.trx_id;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_GPI.amount *= -1;
            var p = new DynamicParameters(bettingDataRNG_GPI);
            p.Add("jackpot_win", 0);
            p.Add("type", (int)BettransType.Bet);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_BettransRNG_GPI_Ins", p, conn, trans);

            if (result > 0 && bettingDataRNG_GPI.amount != 0)
            {
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_GPI.amount, (int)Tp.GPI, bettingDataRNG_GPI.round_id, gameType, bettingDataRNG_GPI.trx_id, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
            }

            return result;
        }

        #endregion

        public int PlaceBetRNG_PP(TpCustInfo tpCustInfo, BettingDataRNG_PP bettingDataRNG_PP, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var bonusCode = (!string.IsNullOrEmpty(bettingDataRNG_PP.bonusCode)) ? $" ({bettingDataRNG_PP.bonusCode})" : "";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_PP.amount *= -1;
            bettingDataRNG_PP.TicketStatus = (int)TicketStatus.UnSettled;
            bettingDataRNG_PP.type = BettransType.Bet;
            var p = new DynamicParameters(new { reference = bettingDataRNG_PP.reference, roundId = bettingDataRNG_PP.roundId, gameId = bettingDataRNG_PP.gameId, amount = bettingDataRNG_PP.amount, userId = bettingDataRNG_PP.userId, roundDetails = bettingDataRNG_PP.roundDetails, timestamp = bettingDataRNG_PP.timestamp, bonusCode = bettingDataRNG_PP.bonusCode, jackpotId = bettingDataRNG_PP.jackpotId, jackpotContribution = bettingDataRNG_PP.jackpotContribution, type = (int)bettingDataRNG_PP.type, TicketStatus = (int)bettingDataRNG_PP.TicketStatus, promoWinAmount = (decimal?)null, promoWinReference = (string)null, promoCampaignID = (int?)null, promoCampaignType = (string)null });

            var category = TpCategory.Slots;
            if (gameType == (int)GameType.PP_RNG)
            {
                result = _mainDbClient.Execute("Service_BettransRng_PP_Ins", p, conn, trans);
            }
            else
            {
                category = TpCategory.LiveCasino;
                result = _mainDbClient.Execute("Service_BettransLC_PP_Ins", p, conn, trans);
            }

            if (result > 0 && bettingDataRNG_PP.amount != 0)
            {
                result = PlaceBetV2(
                    tpCustInfo,
                    bettingDataRNG_PP.amount,
                    (int)Tp.PP,
                    bettingDataRNG_PP.roundId.ToString(),
                    gameType,
                    bettingDataRNG_PP.reference,
                    null,
                    category,
                    out afterAmount,
                    ref bonusId,
                    conn,
                    trans,
                    memo: $"{bonusCode}"

                );
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                
                var p = new DynamicParameters(insertData);
                p.Add("tickets", (int?)null);
                p.Add("bonusprize", (decimal?)null);
                p.Add("type", (int)BettransType.Bet);
                
                result = _mainDbClient.Execute("Service_BettransRngV2_YGG_Ins", p, conn, trans);

                if (result > 0 && insertData.amount < 0)
                {
                    if (result > 0 && insertData.amount < 0)
                    {
                        result = PlaceBetV2(tpCustInfo, insertData.amount, (int)Tp.YGG, insertData.reference, gameType, insertData.subreference, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
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

        public int PlaceBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_SGV2_Ins", p, conn, trans);
            }
            else
            {
                cash = true;
                category = TpCategory.Fish;
                result = _mainDbClient.Execute("Service_BettransFish_SG_Ins", p, conn, trans);
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
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
            }

            return result;
        }

        public int PlaceBetRNG_PG(TpCustInfo tpCustInfo, BettingDataRNG_PG bettingDataRNG_PG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.PG_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_PG_Ins_V3", p, conn, trans);

                if (result > 0 && bettingDataRNG_PG.TransferAmount != 0)
                {
                    result = PlaceBetV2(tpCustInfo, bettingDataRNG_PG.TransferAmount, (int)Tp.PG, bettingDataRNG_PG.ParentBetId, gameType, bettingDataRNG_PG.TransactionId, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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

        #region Joker
        public int PlaceBetRNG_Joker(TpCustInfo tpCustInfo, BettingDataRNG_Joker bettingDataRNG_Joker, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Joker_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_Joker.amount *= -1;
            bettingDataRNG_Joker.type = "bet";
            bettingDataRNG_Joker.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(bettingDataRNG_Joker);
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_BettransRng_Joker_Ins", p, conn, trans);

            if (result > 0 && bettingDataRNG_Joker.amount != 0)
            {
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_Joker.amount, (int)Tp.Joker, bettingDataRNG_Joker.roundid, gameType, bettingDataRNG_Joker.id, null, TpCategory.Slots, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
                using var trans = conn.BeginTransaction();



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
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                result = 0;
            }

            return result;
        }

        public IEnumerable<RewardRNG_Joker> GetRewardRNG_Joker(bool isCancel, RewardRNG_Joker data)
        {
            if (isCancel)
            {
                var query = new { username = data.username, tournamentid = data.tournamentid, FromAPIType = data.FromAPIType };
                return _mainDbClient.Query<RewardRNG_Joker>("Service_RewardRNG_Joker_Cancel_Check", CommandType.StoredProcedure, new DynamicParameters(query));
            }
            else
            {
                var query = new { username = data.username, id = data.id, FromAPIType = data.FromAPIType };
                return _mainDbClient.Query<RewardRNG_Joker>("Service_RewardRNG_Joker_Check", CommandType.StoredProcedure, new DynamicParameters(query));
            }
        }

        public bool InsertReward_Joker(RewardRNG_Joker data, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = _mainDbClient.QueryFirstAsync<int>("Service_RewardRNG_Joker_Ins", trans ,new DynamicParameters(data)).Result;

            return (result == 0) ? false : true;
        }

        public bool PlaceBetReward_Joker(TpCustInfo tpCustInfo, RewardRNG_Joker data,
               out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PlaceBetV2(tpCustInfo,
                                    data.amount,
                                    (int)Tp.Joker,
                                    data.id,  //TpTransId
                                    (int)GameType.Joker_RNG,
                                    data.tournamentid,  //RefId
                                    null,
                                    TpCategory.Slots,
                                    out afterAmount,
                                    ref bonusId,
                                    conn,
                                    trans);

            return (result > 0) ? true : false;
        }

        #endregion

        #region MG
        public async Task<MGPlaceBetResult> PlaceBetRNG_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            var result = 0;
            await using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            bettingDataRNG_MG.amount *= -1;
            bettingDataRNG_MG.TransType = "D";
            bettingDataRNG_MG.TicketType = (int)BettransType.Bet;
            bettingDataRNG_MG.TicketStatus = (int)TicketStatus.UnSettled;
            bettingDataRNG_MG.SettleFlag = (int)SettleFlag.Running;

            try
            {
                // 檢查該局是否已經結算 (避免會員重新進入遊戲對方會再重送資料)
                var isCompleted = await _mainDbClient.QueryFirstOrDefaultAsync<bool>("Service_BettransRng_MG_Check", trans ,new { bettingDataRNG_MG.betId });
                if (isCompleted)
                {
                    logger.LogError($"{bettingDataRNG_MG.betId} is completed");
                    return new MGPlaceBetResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }

                var p = new DynamicParameters(bettingDataRNG_MG);
                p.Add("extTxnId", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransRng_MG_Ins_V2", p, conn , trans);
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
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return new MGPlaceBetResult()
                {
                    Result = result,
                    AfterAmount = afterAmount,
                    ExtTxnId = extTxnId
                };
            }
            catch (MySqlException mySqlEx)
            {
                // 重複注單下注
                if (mySqlEx.Number == 1062)
                {
                    return new MGPlaceBetResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }
                else
                {
                    logger.LogError(mySqlEx, $"PlaceBetRNG_MG failed: {JsonConvert.SerializeObject(bettingDataRNG_MG)}");
                    throw;
                }
            }
        }
        #endregion

        #region CQ9

        public int PlaceBetRNG_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.CQ9_RNG;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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

                        result = _mainDbClient.Execute("Service_BettransRng_CQ9_Ins", p, conn , trans);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn,trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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

                        result = _mainDbClient.Execute("Service_BettransFish_CQ9_Ins", p, conn, trans);

                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn,trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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

                        result = _mainDbClient.Execute("Service_BettransFish_CQ9_Ins", p, conn, trans);
                        if (result > 0)
                        {
                            _mainDbClient.CommitAndDisposeConnection(conn,trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                        }
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_Iconic_Ins", p, conn, trans);

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
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransFish_Iconic_Ins", p, conn, trans);

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
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_Netent_Ins", p, conn, trans);

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
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_BettransRng_RedTiger_Ins", p, conn, trans);

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
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
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
            using var trans = conn.BeginTransaction();

            var bettransAP = _partitionService.Query<BettransRNG_AP>(partitionTime, "Service_BettransRng_AP_Get_V2", new { bettingDataRNG_AP.gameroundid, bettingDataRNG_AP.TicketType });
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
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
            }

            return result;
        }
        #endregion

        #region AdvantPlay Mini
        public int PlaceBet_AdvantPlayMini(TpCustInfo tpCustInfo, BettingData_AdvantPlayMini bettingData_AdvantPlayMini, out decimal afterAmount)
        {
            var result = 0;
            int bonusId = 0;
            int gameType = (int)GameType.AP_RNG;
            afterAmount = 0;
            try
            {
                // Service_BettransRng_AP_Mini_crash_Ins、Service_BettransRng_AP_Mini_cardracing_Ins
                var spName = $"Service_BettransRng_AP_Mini_{bettingData_AdvantPlayMini.MiniGameCode.ToLower()}_Ins";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_AdvantPlayMini.BetTime).DateTime;

                var p = new DynamicParameters(new
                {
                    TransId = bettingData_AdvantPlayMini.TransId,
                    TransactionReferenceId = "",
                    GameId = bettingData_AdvantPlayMini.GameId,
                    SessionId = bettingData_AdvantPlayMini.SessionId,
                    UserReferenceId = bettingData_AdvantPlayMini.UserReferenceId,
                    Amount = bettingData_AdvantPlayMini.Amount,
                    BetTime = bettingData_AdvantPlayMini.BetTime,
                    SettlementTime = 0,
                    BuyAmount = 0,
                    StakeAmount = 0,
                    WonAmount = 0,
                    Multiplier = 0,
                    Odds = 0,
                    Data = "[]",
                    WinResults = "[]",
                    LostResults = "[]",
                    GameResult = "",
                    RefundTime = 0,
                    MiniGameCode = bettingData_AdvantPlayMini.MiniGameCode,
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running,
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                result = _partitionService.Execute(partitionTime, spName, p, trans);

                if (result > 0 && bettingData_AdvantPlayMini.Amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingData_AdvantPlayMini.Amount) * -1,
                        (int)Tp.AP,
                        bettingData_AdvantPlayMini.TransId,
                        gameType,
                        bettingData_AdvantPlayMini.TransId,
                        null,
                        TpCategory.Slots,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                // Duplicate
                if (mySqlEx.Number == 1062)
                {
                    result = -1;
                }
            }
            return result;
        }

        public IEnumerable<BettransData_AdvantPlayMini_Bet> GetAdvantPlayMiniBet(string TransId, long SettlementTime, string MiniGameCode, out string TransactionReferenceId)
        {
            try
            {
                // Service_BettransRng_AP_Mini_crash_Get、Service_BettransRng_AP_Mini_cardracing_Get
                var spName = $"Service_BettransRng_AP_Mini_{MiniGameCode.ToLower()}_Get";

                TransactionReferenceId = "";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(SettlementTime).DateTime;

                var bettrans = _partitionService.Query<BettransData_AdvantPlayMini_Bet>(partitionTime, spName, new { _TransId = TransId });
                if (!bettrans.Any())
                {
                    return null;
                }

                TransactionReferenceId = bettrans.First().TransId;
                return bettrans;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
        }

        public IEnumerable<BettransData_AdvantPlayMini_Bet> GetAdvantPlayMiniBySession(string GameId, long SessionId, long SettlementTime, string MiniGameCode, out string TransactionReferenceId)
        {
            try
            {
                // Service_BettransRng_AP_Mini_crash_BySession_Get、Service_BettransRng_AP_Mini_cardracing_BySession_Get
                var spName = $"Service_BettransRng_AP_Mini_{MiniGameCode.ToLower()}_BySession_Get";

                TransactionReferenceId = "";
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(SettlementTime).DateTime;

                var bettrans = _partitionService.Query<BettransData_AdvantPlayMini_Bet>(partitionTime, spName, new { _GameId = GameId, _SessionId = SessionId });
                if (!bettrans.Any())
                {
                    return null;
                }

                return bettrans;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
                throw;
            }
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

        public IEnumerable<BettingDataRNG_AMB> IsExist_AMB(bool checkEndRound, DateTime partitionTime, BettingDataRNG_AMB data)
        {
            var query = new { CheckEndRound = checkEndRound, transactionId = data.transactionId, roundId = data.roundId, FromAPIType = data.FromAPIType };
            return _partitionService.Query<BettingDataRNG_AMB>(partitionTime, "Service_BettransRng_AMB_Check", query);
        }

        public bool PlaceBet_AMB(DateTime partitionTime, BettingDataRNG_AMB data, MySqlTransaction trans)
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
                                data.bet == 0 ? data.amount : data.bet,
                                (int)Tp.AMB,
                                data.roundId,  //TpTransId
                                (int)GameType.AMB_RNG,
                                data.transactionId,  //RefId
                                null,
                                TpCategory.Slots,
                                out afterAmount,
                                ref bonusId,
                                conn,
                                trans);

            if (result <= 0)
            {
                return false;
            }

            return true;
        }

        #endregion

        #region Kiss918

        public IEnumerable<BettingDataRNG_Kiss918> IsExist_Kiss918(DateTime partitionTime, BettingDataRNG_Kiss918 data)
        {
            var query = new { referenceID = data.referenceID };
            return _partitionService.Query<BettingDataRNG_Kiss918>(partitionTime, "Service_BettransRng_Kiss918_Check", query);
        }

        public bool PlaceBet_Kiss918(DateTime partitionTime, BettingDataRNG_Kiss918 data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Kiss918_Ins", data, trans);

            if (result == 0)
                return false;

            return true;
        }

        public bool SetBalance_Kiss918(TpCustInfo tpCustInfo, BettingDataRNG_Kiss918 data,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PlaceBetV2(tpCustInfo,
                                data.amount,
                                (int)Tp.Kiss918,
                                data.NewRoundId,  //TpTransId
                                (int)GameType.Kiss918_RNG,
                                data.NewRoundId,  //RefId
                                null,
                                TpCategory.Slots,
                                out afterAmount,
                                ref bonusId,
                                conn,
                                trans,
                                memo: $" - {data.referenceID} ");

            if (result <= 0)
                return false;

            return true;
        }

        public bool AddTpBonus_Kiss918(TpCustInfo tpCustInfo, BettingDataRNG_Kiss918 data, int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = AddTpBonus((int)Tp.Kiss918, data.referenceID, (int)GameType.Kiss918_RNG,
                             tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, data.amount,
                             bonusId, TpCategory.Slots, conn, trans);

            if (result == 0)
                return false;

            return true;
        }

        #endregion

        #region JILI

        public int PlaceBetRNG_JILI(TpCustInfo tpCustInfo, BettingData_JILI bettingData_JILI, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var debit = (bettingData_JILI.betAmount != 0) ? Math.Abs(bettingData_JILI.betAmount) * -1 : Math.Abs(bettingData_JILI.preserve) * -1;
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
                TicketType = (bettingData_JILI.betAmount != 0) ? (int)BettransType.Bet : (int)BettransType.Reserve,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = SettleFlag.Running
            });

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_JILI.wagersTime).DateTime;

                var category = TpCategory.Slots;
                var gameType = (int)GameType.JILI_RNG;

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_JILI_Ins_V2", p, trans);
                if (result > 0 && debit != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        debit,
                        (int)Tp.JILI,
                        bettingData_JILI.sessionId.ToString(),
                        gameType,
                        bettingData_JILI.sessionId.ToString(),
                        null,
                        category,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

                    if (bonusId > 0)
                    {
                        AddTpBonus((int)Tp.JILI, bettingData_JILI.round.ToString(), gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, debit, bonusId, category, conn, trans);
                    }
                }

                if (result > 0)
                {
                    _mainDbClient.CommitAndDisposeConnection(conn,trans);
                }
                else
                {
                    _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                }
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'、'UNIQUE'*/
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

        #region SimplePlay

        public int PlaceBetRNG_SimplePlay(TpCustInfo tpCustInfo, BettingData_SimplePlay bettingData_SimplePlay, out decimal afterAmount)
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
                long timestamp = tools.ConvertLocTime(bettingData_SimplePlay.timestamp, timezone);
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;

                var p = new DynamicParameters(new
                {
                    username = bettingData_SimplePlay.username,
                    currency = bettingData_SimplePlay.currency,
                    amount = bettingData_SimplePlay.amount,
                    txnid = bettingData_SimplePlay.txnid,
                    timestamp = timestamp,
                    ip = bettingData_SimplePlay.ip,
                    gametype = bettingData_SimplePlay.gametype,
                    platform = bettingData_SimplePlay.platform,
                    gamecode = bettingData_SimplePlay.gamecode,
                    gameid = bettingData_SimplePlay.gameid,
                    JackpotContribution = bettingData_SimplePlay.JackpotContribution,
                    TransId = bettingData_SimplePlay.TransId,
                    Payouttime = 0,
                    JackpotWin = false,
                    txn_reverse_id = "",
                    TicketType = (int)TransferType.PlaceBet,
                    TicketStatus = (int)TicketStatus.UnSettled
                });

                result = _partitionService.Execute(partitionTime, "Service_BettransRng_SimplePlay_Ins", p, trans);

                if (result > 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingData_SimplePlay.amount) * -1,
                        (int)Tp.SimplePlay,
                        bettingData_SimplePlay.TransId,
                        gameType,
                        bettingData_SimplePlay.TransId,
                        null,
                        TpCategory.Slots,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

                    if (bonusId > 0)
                    {
                        AddTpBonus((int)Tp.SimplePlay, bettingData_SimplePlay.txnid, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, bettingData_SimplePlay.amount, bonusId, TpCategory.Slots, conn, trans);
                    }

                    if (result > 0)
                    {
                        _mainDbClient.CommitAndDisposeConnection(conn,trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn,trans);
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                logger.LogError(mySqlEx.Message);
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    result = -1;
                }
            }
            return result;
        }

        #endregion

        #region SkyWind
        public int PlaceBet_Skywind(TpCustInfo tpCustInfo, BettingData_Skywind bettingData_Skywind, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            var tpCategory = gameType == (int)GameType.Skywind_RNG ? TpCategory.Slots : TpCategory.LiveCasino;
            bettingData_Skywind.TicketStatus = (int)TicketStatus.UnSettled;
            bettingData_Skywind.TicketType = (int)BettransType.Bet;
            afterAmount = 0;
            var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingData_Skywind.timestamp).DateTime;
            var query = new
            {
                bettingData_Skywind.trx_id
            };
            var check = _partitionService.Query<BettransRng_Skywind>(partitionTime, "Service_Bettrans_Skywind_Check", query);
            if (check.Any())
            {
                return -1;
            }

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = _partitionService.Execute(partitionTime, "Service_Bettrans_Skywind_Ins", bettingData_Skywind, trans);

            if (result > 0 && bettingData_Skywind.amount != 0)
            {
                var TransId = $"{bettingData_Skywind.round_id}-{bettingData_Skywind.game_id}";
                result = PlaceBetV2(tpCustInfo, Math.Abs(bettingData_Skywind.amount) * -1, (int)Tp.Skywind, TransId, gameType, TransId, null, tpCategory, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn,trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn,trans);
            }

            return result;
        }
        #endregion

        #region Nextspin
        public IEnumerable<BettingDataRNG_Nextspin> GetBettingDataRNG_Nextspin(string referenceId)
        {
            var now = DateTime.UtcNow;
            return _partitionService.Query<BettingDataRNG_Nextspin>(now, "Service_BettransRng_Nextspin_Get", new { referenceId });
        }

        public async Task<NextspinResult> PlaceBetRNG_Nextspin(TpCustInfo tpCustInfo, BettingDataRNG_Nextspin bettingData) 
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            await conn.OpenAsync();
            await using var trans = await conn.BeginTransactionAsync();
            try
            {
                var partitionTime = DateTime.UtcNow;
                bettingData.TicketType = (int)BettransType.Bet;
                bettingData.TicketStatus = (int)TicketStatus.UnSettled;
                var p = new DynamicParameters(bettingData);
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                var result = _partitionService.Execute(partitionTime, "Service_BettransRng_Nextspin_Ins", p, trans);
                var sq = p.Get<long>("SQ");
                if (sq == 0)
                    throw new NextspinException(NextspinErrorCode.SystemError);
                
                var bonusId = 0;
                result = PlaceBetV2(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(bettingData.amount) * -1,
                    tpId: (int)Tp.Nextspin,
                    tpTransId: bettingData.referenceId,
                    gameType: (int)GameType.Nextspin_RNG,
                    refId: bettingData.transferId,
                    betData: null,
                    tpCategory: TpCategory.Slots,
                    afterAmount: out var afterAmount,
                    bonusId: ref bonusId,
                    conn: conn,
                    trans: trans);
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
                await trans.RollbackAsync();
                logger.LogError(ex, ex.Message);
                throw new NextspinException(NextspinErrorCode.SystemError);
            }
        }
        #endregion
    }
}
