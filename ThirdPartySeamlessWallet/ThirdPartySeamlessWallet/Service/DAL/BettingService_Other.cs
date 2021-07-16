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
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region CF

        public int PlaceBet_CF(TpCustInfo tpCustInfo, BettingData_CF bettingData_CF, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                ticket_id = bettingData_CF.ticket_id,
                login_id = bettingData_CF.login_id,
                arena_code = bettingData_CF.arena_code,
                match_no = bettingData_CF.match_no,
                matchUnix = new DateTimeOffset(bettingData_CF.match_date, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                fight_no = bettingData_CF.fight_no,
                fightUnix = new DateTimeOffset(bettingData_CF.fight_datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                bet_on = bettingData_CF.bet_on,
                odds_given = bettingData_CF.odds_given,
                stake = bettingData_CF.stake,
                stake_money = bettingData_CF.stake_money,
                createUnix = new DateTimeOffset(bettingData_CF.created_datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds()
            });
            try
            {
                result = conn.Execute("Service_Bettrans_CF_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingData_CF.stake != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingData_CF.stake_money * -1,
                        (int)Tp.CF,
                        TransType.Wagers,
                        $"Stake: {bettingData_CF.stake_money:#,##0.##}",
                        bettingData_CF.ticket_id.ToString(),
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

        public int CancelBet_CF(TpCustInfo tpCustInfo, CancelBetData_CF cancelBetData_CF)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new { ticket_id = cancelBetData_CF.ticket_id });
            p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("out_stake_money", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            result = conn.Execute("Service_Bettrans_CF_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal payout = p.Get<decimal>("out_stake_money");
                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    payout,
                    (int)Tp.CF,
                    TransType.Refunds,
                    $"Cancel: {Math.Abs(cancelBetData_CF.stake_money):#,##0.##}",
                    cancelBetData_CF.ticket_id.ToString(),
                    out _,
                    conn,
                    trans
                );
            }

            if (result > 0)
            {
                trans.Commit();
            }
            return result;

        }

        public BetRecordData_CF GetBetRecord_CF(CancelBetData_CF cancelBetData_CF)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BetRecordData_CF>("Service_Bettrans_CF_Get", new { ticket_id = cancelBetData_CF.ticket_id }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region AWC

        public int PlaceBetCF_AWC(TpCustInfo tpCustInfo, BetCF_AWC betCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            foreach (var item in betCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    userId = item.userId,
                    currency = item.currency,
                    platform = item.platform,
                    gameType = item.gameType,
                    gameCode = item.gameCode,
                    gameName = item.gameName,
                    betType = item.betType,
                    betAmount = item.betAmount,
                    realBetAmount = item.gameInfo.realBetAmount,
                    odds = item.gameInfo.odds,
                    betUnix = new DateTimeOffset(item.betTime).ToUnixTimeSeconds(),
                    roundId = item.roundId,
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo)
                });

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            item.betAmount * -1,
                            (int)Tp.AWC,
                            TransType.Wagers,
                            $"Stake: {item.betAmount:#,##0.##}",
                            item.platformTxId,
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
                    /*Duplicate for key 'PRIMARY'*/
                    if (mySqlEx.Number == 1062)
                    {
                        result = -1;
                        continue;
                    }
                    else
                    {
                        throw mySqlEx;
                    }
                }
            }
            return result;

        }

        public int CancelBetCF_AWC(TpCustInfo tpCustInfo, CancelCF_AWC cancelCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            foreach (var item in cancelCF_AWC.txns)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new { platformTxId = item.platformTxId });
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                result = conn.Execute("Service_BettransCF_AWC_CancelBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    decimal stake = p.Get<decimal>("out_stake");
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        stake,
                        (int)Tp.AWC,
                        TransType.Refunds,
                        $"Cancel: {Math.Abs(stake):#,##0.##}",
                        item.platformTxId,
                        out afterAmount,
                        conn,
                        trans
                    );

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                else
                {
                    result = -1;
                    continue;
                }
            }

            return result;
        }

        public int AdjustBetCF_AWC(TpCustInfo tpCustInfo, AdjustBetCF_AWC adjustBetCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            foreach (var item in adjustBetCF_AWC.txns)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    betAmount = item.betAmount,
                    adjustAmount = item.adjustAmount,
                    realBetAmount = item.gameInfo.realBetAmount,
                    odds = item.gameInfo.odds,
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                });
                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_AdjustBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        decimal adjust = item.adjustAmount;
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            adjust,
                            (int)Tp.AWC,
                            TransType.Refunds,
                            $"AdjustBet: {Math.Abs(adjust):#,##0.##}",
                            item.platformTxId,
                            out afterAmount,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                    else
                    {
                        result = -1;
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int VoidBetCF_AWC(VoidBetCF_AWC voidBetCF_AWC)
        {
            var result = 0;
            foreach (var item in voidBetCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    //txId = item.txId,
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),

                });

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_VoidBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    if (result > 0)
                    {
                        decimal stake = Math.Abs(item.betAmount);
                        result = walletService.SetBalance(
                            item.tpCustInfo.CustId,
                            item.tpCustInfo.SiteId,
                            item.tpCustInfo.UserName,
                            stake,
                            (int)Tp.AWC,
                            TransType.Refunds,
                            $"VoidBet: {Math.Abs(stake):#,##0.##}",
                            item.platformTxId,
                            out _,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }
                    else
                    {
                        result = -1;
                        continue;
                    }
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    throw mySqlEx;
                }
            }
            return result;
        }

        public int UnvoidBetCF_AWC(UnVoidBetCF_AWC unVoidBetCF_AWC)
        {
            var result = 0;
            foreach (var item in unVoidBetCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    //txId = item.txId,
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds()
                });
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_UnvoidBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    if (result > 0)
                    {
                        decimal stake = p.Get<decimal>("out_stake");
                        result = walletService.SetBalance(
                            item.tpCustInfo.CustId,
                            item.tpCustInfo.SiteId,
                            item.tpCustInfo.UserName,
                            Math.Abs(stake) * -1,
                            (int)Tp.AWC,
                            TransType.Wagers,
                            $"UnvoidBet: {Math.Abs(stake):#,##0.##}",
                            item.platformTxId,
                            out _,
                            conn,
                            trans,
                            AllowNegative.Allow
                        );
                    }
                    else
                    {
                        result = -1;
                        continue;
                    }
                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    throw mySqlEx;
                }
            }
            return result;
        }

        #endregion

        #region One789
        public int PlaceBetLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var gameParseTmp = bettingDataLT_One789.Source.Split("_");
            var gameParse = gameParseTmp[0];
            var p = new DynamicParameters(new
            {
                Username = bettingDataLT_One789.Username,
                Tx = bettingDataLT_One789.Tx,
                Amount = bettingDataLT_One789.Amount,
                Description = bettingDataLT_One789.Description,
                CreatedAt = bettingDataLT_One789.CreatedAt,
                Source = bettingDataLT_One789.Source,
                Type = bettingDataLT_One789.Type,
                GameType = gameParse,
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 2
            });

            try
            {
                string mixUniqueId = "";
                var category = TpCategory.Lottery;
                if (gameType == (int)GameType.One789_Lottery)
                {
                    mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    result = conn.Execute("Service_BettransLT_One789_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }
                else if (gameType == (int)GameType.One789_Lottery_789)
                {
                    mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    result = conn.Execute("Service_BettransLT_One789_789_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }
                else
                {
                    mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    category = TpCategory.LiveCasino;
                    result = conn.Execute("Service_BettransLC_One789_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                }

                if (result > 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingDataLT_One789.Amount) * -1,
                        (int)Tp.One789,
                        mixUniqueId,
                        gameType,
                        bettingDataLT_One789.Tx,
                        null,
                        category,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

                    //if (bonusId > 0 && result > 0)
                    //{
                    //    AddTpBonus(
                    //        (int)Tp.One789,
                    //        bettingDataLT_One789.Tx,
                    //        gameType,
                    //        tpCustInfo.CustId,
                    //        tpCustInfo.UserName,
                    //        tpCustInfo.CurrencyId,
                    //        tpCustInfo.SiteId,
                    //        Math.Abs(bettingDataLT_One789.Amount),
                    //        bonusId,
                    //        category,
                    //        conn,
                    //        trans
                    //    );
                    //}

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

        #region TCG
        public async Task<TCGBettransLottery> BettransLottery_Get_TCG(string username, string ref_no)
        {
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                var p = new DynamicParameters(new
                {
                    username,
                    ref_no,
                });

                return await conn.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p, commandType: CommandType.StoredProcedure);         
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"BettransLottery_Get_TCG Exception: username={username}, ref_no={ref_no}");
                return default;
            }
        }


        public async Task<TCGResult> Debit_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGTransactionData data)
        {
            try
            {
                var bonusId = 0;
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                var p = new DynamicParameters(new
                {
                    request.method,
                    request.product_type,
                    request.request_time,
                    data.ref_no,
                    data.username,
                    data.amount,
                    data.tx_type,
                    data.order_no,
                    data.remarks,
                    additionalInformation = JsonConvert.SerializeObject(data.additionalInformation),
                    TransType = TCGTransType.Debit.ToString(),
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                var bettransTCG = await conn.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p, transaction: trans, commandType: CommandType.StoredProcedure);
                if (bettransTCG != null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionAlreadyExists,
                    };
                }
                
                var result = conn.Execute("Service_BettransLottery_TCG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                
                if (result > 0 && data.amount != 0)
                {
                    result = PlaceBetV2(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(data.amount) * -1,
                    tpId: (int)Tp.TCG,
                    tpTransId: data.ref_no,
                    gameType: (int)gameType,
                    refId: data.ref_no,
                    betData: null,
                    tpCategory: TpCategory.Lottery,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    conn: conn,
                    trans: trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }

                return new TCGResult()
                { 
                    Result = result > 0 ? TCGReturnCode.Success : TCGReturnCode.UnknownSystemError,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Debit_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        public async Task<TCGResult> Reserve_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGTransactionData data)
        {
            try
            {
                var bonusId = 0;
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    request.method,
                    request.product_type,
                    request.request_time,
                    data.ref_no,
                    data.username,
                    data.amount,
                    data.tx_type,
                    data.order_no,
                    data.remarks,
                    additionalInformation = JsonConvert.SerializeObject(data.additionalInformation),
                    TransType = TCGTransType.Reserve.ToString(),
                    TicketType = (int)BettransType.Reserve,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                var bettransTCG = await conn.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p, transaction: trans, commandType: CommandType.StoredProcedure);
                if (bettransTCG != null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionAlreadyExists,
                    };
                }

                var result = conn.Execute("Service_BettransLottery_TCG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && data.amount != 0)
                {
                    result = PlaceBetV2(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(data.amount) * -1,
                    tpId: (int)Tp.TCG,
                    tpTransId: data.ref_no,
                    gameType: (int)gameType,
                    refId: data.ref_no,
                    betData: null,
                    tpCategory: TpCategory.Lottery,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    conn: conn,
                    trans: trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }

                return new TCGResult()
                {
                    Result = result > 0 ? TCGReturnCode.Success : TCGReturnCode.UnknownSystemError,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Reserve_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        #endregion
    }
}