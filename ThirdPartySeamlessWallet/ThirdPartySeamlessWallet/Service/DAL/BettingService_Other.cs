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

                try
                {
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
                            $"Resettle: {Math.Abs(adjust):#,##0.##}",
                            item.platformTxId,
                            out afterAmount,
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

        public int VoidBetCF_AWC(VoidBetCF_AWC voidBetCF_AWC)
        {
            var result = 0;
            foreach (var item in voidBetCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    txId = item.txId,
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
                            $"Refund: {Math.Abs(stake):#,##0.##}",
                            item.platformTxId,
                            out _,
                            conn,
                            trans
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
                    txId = item.txId,
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
                            $"Resettle: {Math.Abs(stake):#,##0.##}",
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
    }
}
