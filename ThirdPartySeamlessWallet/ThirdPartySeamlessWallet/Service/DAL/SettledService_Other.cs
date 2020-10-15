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
        #region CF

        public int Payout_CF(TpCustInfo tpCustInfo, PayoutData_CF payoutData_CF, out decimal afterAmount)
        {
            afterAmount = 0;
            int betRes = 0;
            var transType = TransType.Payout;
            betRes = payoutData_CF.status switch
            {
                "WIN" => (int)TicketStatus.Win,
                "LOST" => (int)TicketStatus.Lost,
                _ => (int)TicketStatus.Refund,
            };
            using var connWager = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            connWager.Open();
            using var wagerTrans = connWager.BeginTransaction();

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = connWager.Execute("Service_TpWagerCF_Payout_Set", new { ticket_id = payoutData_CF.ticket_id }, transaction: wagerTrans, commandType: CommandType.StoredProcedure);
            if (result > 0)
            {
                var betData = conn.QueryFirstOrDefault<BetRecordData_CF>("Service_Bettrans_CF_Get", new { ticket_id = payoutData_CF.ticket_id }, transaction: trans, commandType: CommandType.StoredProcedure);
                conn.Execute("Service_Bettrans_CF_Payout_Set", new { ticket_id = payoutData_CF.ticket_id, Payout = payoutData_CF.payout, TicketStatus = betRes }, transaction: trans, commandType: CommandType.StoredProcedure);
                var payoutAmount = payoutData_CF.payout;
                var headMemo = "Payout";
                if (betRes == (int)TicketStatus.Refund)
                {
                    headMemo = "Refund";
                }

                if (betData.SettleFlag != 0)
                {
                    payoutAmount = 0;

                    if (betData.Payout == 0 && betRes == (int)TicketStatus.Refund)
                    {
                        payoutAmount = payoutData_CF.payout;
                        transType = TransType.Refunds;
                        headMemo = "Refund";
                    }
                    else if (betData.Payout == 0 && betRes == (int)TicketStatus.Win)
                    {
                        payoutAmount = payoutData_CF.payout;
                        headMemo = "Resettle";
                    }
                    else if (betData.Payout > 0 && betRes == (int)TicketStatus.Lost)
                    {
                        payoutAmount = -1 * betData.Payout;
                        headMemo = "Resettle";
                    }
                    else if (betData.Payout > 0 && betData.Payout != payoutData_CF.payout && betRes == (int)TicketStatus.Refund)
                    {
                        payoutAmount = payoutData_CF.payout - betData.Payout;
                        transType = TransType.Refunds;
                        headMemo = "Refund";
                    }
                }


                if (payoutAmount != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        payoutAmount,
                        (int)Tp.CF,
                        transType,
                        $"{headMemo}: {Math.Abs(payoutAmount):#,##0.##}",
                        payoutData_CF.ticket_id.ToString(),
                        out afterAmount,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    wagerTrans.Commit();
                    trans.Commit();
                }
            }

            //using var conn_sec = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            //conn_sec.Execute("Service_Bettrans_CF_Payout_Set", new { ticket_id = payoutData_CF.ticket_id, Payout = payoutData_CF.payout, TicketStatus = betRes }, commandType: CommandType.StoredProcedure);

            return result;
        }

        public int Settled_CF(TpCustInfo tpCustInfo, SettleData_CF settleData_CF, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

            int betRes = 0;
            betRes = settleData_CF.status switch
            {
                "WIN" => (int)TicketStatus.Win,
                "LOSE" => (int)TicketStatus.Lost,
                _ => (int)TicketStatus.Refund,
            };

            var ticketStatus = settleData_CF.status switch
            {
                "WIN" => TicketStatus.Win,
                "LOSE" => TicketStatus.Lost,
                _ => TicketStatus.Refund,
            };

            decimal ValidBet = (betRes == (int)TicketStatus.Refund && settleData_CF.winloss == 0) ? 0 : settleData_CF.stake_money;
            BetRecordInfo betRecordInfo = new BetRecordInfo
            {
                TpId = (int)Tp.CF,
                Stake = settleData_CF.stake,
                ValidBet = ValidBet,
                WinlostAmount = settleData_CF.winloss,
                TransId = settleData_CF.ticket_id.ToString(),
                GameType = GameType.CF_Rooster,
                TpUserName = settleData_CF.login_id,
                SettledTime = 0,
                SettlementType = SettlementType.SettledForValidNotRollover,
                TicketStatus = ticketStatus
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
            conn_wag.Execute("Service_TpWagerCF_Settle_Set", new { ticket_id = settleData_CF.ticket_id }, commandType: CommandType.StoredProcedure);

            using var conn_sec = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn_sec.Execute("Service_Bettrans_CF_Settle_Set", new { ticket_id = settleData_CF.ticket_id, TicketStatus = betRes }, commandType: CommandType.StoredProcedure);

            return result;
        }

        public IEnumerable<PayoutData_CF> GetUnPayoutList_CF()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<PayoutData_CF>("Service_TpWagerCF_PayoutList_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<SettleData_CF> GetUnSettleList_CF()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleData_CF>("Service_TpWagerCF_SettleList_Get", commandType: CommandType.StoredProcedure);
        }

        public BetRecordData_CF GetBetRecord_CF(PayoutData_CF payoutData_CF)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<BetRecordData_CF>("Service_Bettrans_CF_Get", new { ticket_id = payoutData_CF.ticket_id }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region AWC

        public int PayoutCF_AWC(TpCustInfo tpCustInfo, SettleDataCF_AWC settleDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;

            int ticketStatus = (int)TicketStatus.Win;
            if (settleDataCF_AWC.winAmount < 0)
            {
                ticketStatus = (int)TicketStatus.Lost;
            }
            else if (settleDataCF_AWC.winAmount == 0)
            {
                ticketStatus = (int)TicketStatus.Draw;
            }


            var p = new DynamicParameters(new
            {
                platformTxId = settleDataCF_AWC.platformTxId,
                txId = settleDataCF_AWC.txId,
                userId = settleDataCF_AWC.userId,
                platform = settleDataCF_AWC.platform,
                gameType = settleDataCF_AWC.gameType,
                gameCode = settleDataCF_AWC.gameCode,
                gameName = settleDataCF_AWC.gameName,
                betType = settleDataCF_AWC.betType,
                betAmount = settleDataCF_AWC.betAmount,
                winAmount = settleDataCF_AWC.winAmount,
                turnover = settleDataCF_AWC.turnover,
                roundId = settleDataCF_AWC.roundId,
                gameInfo = JsonConvert.SerializeObject(settleDataCF_AWC.gameInfo),
                betUnix = new DateTimeOffset(settleDataCF_AWC.betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                txUnix = new DateTimeOffset(settleDataCF_AWC.txTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                updateUnix = new DateTimeOffset(settleDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketStatus = ticketStatus,
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });
            p.Add("out_action", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransCF_AWC_Payout_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                decimal payout = settleDataCF_AWC.winAmount;
                if (result > 0 && payout != 0)
                {
                    int doAction = p.Get<int>("out_action");
                    if (doAction > 0)
                    {
                        var headMemo = (doAction == 1) ? "Payout" : "Resettle";
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            payout,
                            (int)Tp.AWC,
                            TransType.Payout,
                            $"{headMemo}: {Math.Abs(payout):#,##0.##}",
                            settleDataCF_AWC.platformTxId,
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
                    else
                    {
                        return doAction;
                    }
                }
                else
                {
                    return -1;
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

        public int UnPayoutCF_AWC(TpCustInfo tpCustInfo, UnsettleDataCF_AWC unsettleDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;

            var p = new DynamicParameters(new
            {
                platformTxId = unsettleDataCF_AWC.platformTxId,
                txId = unsettleDataCF_AWC.txId,
                voidType = 0,
                updateUnix = new DateTimeOffset(unsettleDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketStatus = (int)TicketStatus.Refund,
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });
            p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransCF_AWC_UnPayout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    decimal payout = p.Get<decimal>("out_payout");
                    if (payout != 0)
                    {
                        var headMemo = "Resettle";
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            payout * -1,
                            (int)Tp.AWC,
                            TransType.Undo,
                            $"{headMemo}: {Math.Abs(payout):#,##0.##}",
                            unsettleDataCF_AWC.platformTxId,
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

        public int VoidSettleCF_AWC(TpCustInfo tpCustInfo, VoidSettleDataCF_AWC voidSettleDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;

            var p = new DynamicParameters(new
            {
                platformTxId = voidSettleDataCF_AWC.platformTxId,
                txId = voidSettleDataCF_AWC.txId,
                voidType = voidSettleDataCF_AWC.voidType,
                updateUnix = new DateTimeOffset(voidSettleDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketStatus = (int)TicketStatus.Refund,
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });
            p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransCF_AWC_UnPayout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    decimal payout = p.Get<decimal>("out_payout");
                    if (payout != 0)
                    {
                        var headMemo = "Resettle";
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            payout * -1,
                            (int)Tp.AWC,
                            TransType.Undo,
                            $"{headMemo}: {Math.Abs(payout):#,##0.##}",
                            voidSettleDataCF_AWC.platformTxId,
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

        public int UnvoidSettleCF_AWC(TpCustInfo tpCustInfo, UnvoidSettleDataCF_AWC unvoidSettleDataCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;

            var p = new DynamicParameters(new
            {
                platformTxId = unvoidSettleDataCF_AWC.platformTxId,
                txId = unvoidSettleDataCF_AWC.txId,
                voidType = unvoidSettleDataCF_AWC.voidType,
                updateUnix = new DateTimeOffset(unvoidSettleDataCF_AWC.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Payout,
                SettleFlag = 2
            });
            p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransCF_AWC_UnvoidPayout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    decimal payout = p.Get<decimal>("out_payout");
                    if (payout != 0)
                    {
                        var headMemo = "Resettle";
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            payout,
                            (int)Tp.AWC,
                            TransType.Undo,
                            $"{headMemo}: {Math.Abs(payout):#,##0.##}",
                            unvoidSettleDataCF_AWC.platformTxId,
                            out _,
                            conn,
                            trans
                        );

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
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int RefundCF_AWC(TpCustInfo tpCustInfo, RefundCF_AWC refundCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var before = refundCF_AWC.txns.ElementAt(0);
                var after = refundCF_AWC.txns.ElementAt(1);
                var payout = before.winAmount + after.winAmount;

                int ticketStatus = (int)TicketStatus.Win;
                if (after.winAmount < 0)
                {
                    ticketStatus = (int)TicketStatus.Lost;
                }
                else if (after.winAmount == 0)
                {
                    ticketStatus = (int)TicketStatus.Draw;
                }

                var p = new DynamicParameters(new
                {
                    platformTxId = after.platformTxId,
                    txId = after.txId,
                    betAmount = after.betAmount,
                    winAmount = after.winAmount,
                    turnover = after.turnover,
                    updateUnix = new DateTimeOffset(after.updateTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                    TicketStatus = ticketStatus,
                    TicketType = (int)BettransType.Payout,
                    SettleFlag = 2
                });

                result = conn.Execute("Service_BettransCF_AWC_Refund_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (payout != 0)
                    {
                        var headMemo = "Resettle";
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            payout,
                            (int)Tp.AWC,
                            TransType.Refunds,
                            $"{headMemo}: {Math.Abs(payout):#,##0.##}",
                            after.platformTxId,
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
    }
}
