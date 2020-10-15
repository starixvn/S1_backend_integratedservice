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

        public int PlaceBetCF_AWC(TpCustInfo tpCustInfo, BettingDataCF_AWC bettingDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new
            {
                platformTxId = bettingDataCF_AWC.platformTxId,
                userId = bettingDataCF_AWC.userId,
                currency = bettingDataCF_AWC.currency,
                platform = bettingDataCF_AWC.platform,
                gameType = bettingDataCF_AWC.gameType,
                gameCode = bettingDataCF_AWC.gameCode,
                gameName = bettingDataCF_AWC.gameName,
                betType = bettingDataCF_AWC.betType,
                betAmount = bettingDataCF_AWC.betAmount,
                betUnix = new DateTimeOffset(bettingDataCF_AWC.betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                roundId = bettingDataCF_AWC.roundId,
                gameInfo = JsonConvert.SerializeObject(bettingDataCF_AWC.gameInfo)
            });
            try
            {
                result = conn.Execute("Service_BettransCF_AWC_Bet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataCF_AWC.betAmount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        bettingDataCF_AWC.betAmount * -1,
                        (int)Tp.AWC,
                        TransType.Wagers,
                        $"Stake: {bettingDataCF_AWC.betAmount:#,##0.##}",
                        bettingDataCF_AWC.platformTxId,
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

        public int CancelBetCF_AWC(TpCustInfo tpCustInfo, CancelBetDataCF_AWC cancelBetDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new { platformTxId = cancelBetDataCF_AWC.platformTxId });
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
                    cancelBetDataCF_AWC.platformTxId,
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
                return -1;
            }


            return result;
        }

        public int AdjustBetCF_AWC(TpCustInfo tpCustInfo, AdjustBetDataCF_AWC adjustBetDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new
            {
                platformTxId = adjustBetDataCF_AWC.platformTxId,
                betAmount = adjustBetDataCF_AWC.betAmount,
                adjustAmount = adjustBetDataCF_AWC.adjustAmount,
                updateUnix = new DateTimeOffset(adjustBetDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
            });
            result = conn.Execute("Service_BettransCF_AWC_AdjustBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal adjust = adjustBetDataCF_AWC.adjustAmount;
                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    adjust,
                    (int)Tp.AWC,
                    TransType.Refunds,
                    $"Resettle: {Math.Abs(adjust):#,##0.##}",
                    adjustBetDataCF_AWC.platformTxId,
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
                return -1;
            }

            return result;
        }

        public int VoidBetCF_AWC(TpCustInfo tpCustInfo, VoidBetDataCF_AWC voidBetDataCF_AWC)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new
            {
                platformTxId = voidBetDataCF_AWC.platformTxId,
                txId = voidBetDataCF_AWC.txId,
                updateUnix = new DateTimeOffset(voidBetDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
            });
            result = conn.Execute("Service_BettransCF_AWC_VoidBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal stake = Math.Abs(voidBetDataCF_AWC.betAmount);
                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    stake,
                    (int)Tp.AWC,
                    TransType.Refunds,
                    $"Refund: {Math.Abs(stake):#,##0.##}",
                    voidBetDataCF_AWC.platformTxId,
                    out _,
                    conn,
                    trans
                );

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            
            return result;
        }

        public int UnvoidBetCF_AWC(TpCustInfo tpCustInfo, UnvoidBetDataCF_AWC unvoidBetDataCF_AWC)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new
            {
                platformTxId = unvoidBetDataCF_AWC.platformTxId,
                txId = unvoidBetDataCF_AWC.txId,
                updateUnix = new DateTimeOffset(unvoidBetDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds()
            });
            p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            result = conn.Execute("Service_BettransCF_AWC_UnvoidBet_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal stake = p.Get<decimal>("out_stake");
                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    Math.Abs(stake) * -1,
                    (int)Tp.AWC,
                    TransType.Wagers,
                    $"Resettle: {Math.Abs(stake):#,##0.##}",
                    unvoidBetDataCF_AWC.platformTxId,
                    out _,
                    conn,
                    trans,
                    AllowNegative.Allow
                );

                if (result > 0)
                {
                    trans.Commit();
                }
            }
            
            return result;
        }

        #endregion
    }
}
