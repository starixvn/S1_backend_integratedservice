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

        public int PayoutCF_AWC(PayoutCF_AWC payoutCF_AWC, out decimal afterAmount)
        {
            /*
            result ->1:win , -1:lose , 0:draw
            settled ->0 : non-settled ; 1 : settled
            status :LOSE / WIN / DRAW / VOID / CANCEL
            */
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in payoutCF_AWC.txns)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                int ticketStatus;
                ticketStatus = item.gameInfo.result switch
                {
                    1 => (int)TicketStatus.Win,
                    -1 => (int)TicketStatus.Lost,
                    0 => (int)TicketStatus.Draw,
                    _ => throw new NotImplementedException()
                };

                try
                {
                    var p = new DynamicParameters(new
                    {
                        platformTxId = item.platformTxId,
                        txId = item.txId,
                        winAmount = item.winAmount,
                        turnover = item.turnover,
                        roundId = item.roundId,
                        betType = item.betType,
                        gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                        betUnix = new DateTimeOffset(item.betTime).ToUnixTimeSeconds(),
                        txUnix = new DateTimeOffset(item.txTime).ToUnixTimeSeconds(),
                        updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                        TicketStatus = ticketStatus
                    });
                    result = conn.Execute("Service_BettransCF_AWC_Payout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    decimal payout = item.winAmount;
                    if (result > 0)
                    {
                        if (payout != 0)
                        {
                            result = walletService.SetBalance(
                                item.tpCustInfo.CustId,
                                item.tpCustInfo.SiteId,
                                item.tpCustInfo.UserName,
                                payout,
                                (int)Tp.AWC,
                                TransType.Payout,
                                $"Payout: {Math.Abs(payout):#,##0.##}",
                                item.platformTxId,
                                out _,
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
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_settle");
                }
            }

            return isError ? 0 : result;
        }

        public int UnPayoutCF_AWC(UnPayoutCF_AWC unPayoutCF_AWC, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in unPayoutCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    txId = item.txId,
                    gameInfo = JsonConvert.SerializeObject(item.gameInfo),
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds()
                });
                p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                            result = walletService.SetBalance(
                                item.tpCustInfo.CustId,
                                item.tpCustInfo.SiteId,
                                item.tpCustInfo.UserName,
                                payout * -1,
                                (int)Tp.AWC,
                                TransType.Undo,
                                $"UnSettle: {Math.Abs(payout):#,##0.##}",
                                item.platformTxId,
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_unsettle");
                }
            }

            return isError ? 0 : result;
        }

        public int VoidSettleCF_AWC(VoidSettleCF_AWC voidSettleCF_AWC, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in voidSettleCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    txId = item.txId,
                    voidType = item.voidType,
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds(),
                });
                p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));

                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_VoidPayout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        decimal payout = p.Get<decimal>("out_payout");
                        payout = (payout > 0) ? (payout * -1) + item.betAmount : item.betAmount;

                        if (payout != 0)
                        {
                            result = walletService.SetBalance(
                                item.tpCustInfo.CustId,
                                item.tpCustInfo.SiteId,
                                item.tpCustInfo.UserName,
                                payout,
                                (int)Tp.AWC,
                                TransType.Refunds,
                                $"VoidSettle: {Math.Abs(payout):#,##0.##}",
                                item.platformTxId,
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_voidSettle");
                }
            }

            return isError ? 0 : result;
        }

        public int UnvoidSettleCF_AWC(UnVoidSettleCF_AWC unVoidSettleCF_AWC, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in unVoidSettleCF_AWC.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = item.platformTxId,
                    voidType = item.voidType,
                    updateUnix = new DateTimeOffset(item.updateTime).ToUnixTimeSeconds()
                });
                p.Add("out_payout", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = conn.Execute("Service_BettransCF_AWC_UnvoidPayout_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                    if (result > 0)
                    {
                        decimal payout = p.Get<decimal>("out_payout");
                        decimal stake = p.Get<decimal>("out_stake");
                        payout = (payout > 0) ? (stake * -1) + payout : stake * -1;

                        if (payout != 0)
                        {
                            result = walletService.SetBalance(
                                item.tpCustInfo.CustId,
                                item.tpCustInfo.SiteId,
                                item.tpCustInfo.UserName,
                                payout,
                                (int)Tp.AWC,
                                TransType.Undo,
                                $"UnVoidSettle: {Math.Abs(payout):#,##0.##}",
                                item.platformTxId,
                                out _,
                                conn,
                                trans,
                                AllowNegative.Allow
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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_unvoidSettle");
                }
            }

            return isError ? 0 : result;
        }

        public int RefundCF_AWC(TpCustInfo tpCustInfo, RefundCF_AWC refundCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            conn.Open();
            using var trans = conn.BeginTransaction();
            var before = refundCF_AWC.txns.ElementAt(0);
            var after = refundCF_AWC.txns.ElementAt(1);
            var payout = after.winAmount;

            int ticketStatus;
            ticketStatus = after.gameInfo.result switch
            {
                1 => (int)TicketStatus.Win,
                -1 => (int)TicketStatus.Lost,
                0 => (int)TicketStatus.Draw,
                _ => throw new NotImplementedException()
            };

            var p = new DynamicParameters(new
            {
                platformTxId = after.refundPlatformTxId,
                betAmount = after.betAmount,
                winAmount = after.winAmount,
                gameInfo = JsonConvert.SerializeObject(after.gameInfo),
                betUnix = new DateTimeOffset(after.betTime).ToUnixTimeSeconds(),
                updateUnix = new DateTimeOffset(after.updateTime).ToUnixTimeSeconds(),
                RefPlatformTxId = after.platformTxId,
                RefTxId = after.txId,
                TicketStatus = ticketStatus
            });

            result = conn.Execute("Service_BettransCF_AWC_Refund_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                if (payout != 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        payout,
                        (int)Tp.AWC,
                        TransType.Refunds,
                        $"Resettle: {Math.Abs(payout):#,##0.##}",
                        after.refundPlatformTxId,
                        out _,
                        conn,
                        trans,
                        AllowNegative.Allow
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

            return result;
        }

        public IEnumerable<SettleCF_AWC> GetUnsettledListCF_AWC()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleCF_AWC>("Service_BettransCF_AWC_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int Settled_AWC(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                result = conn.Execute("Service_BettransCF_AWC_Settle_Set", new { platformTxId = betRecordInfo.TransId }, transaction: trans, commandType: CommandType.StoredProcedure);
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
