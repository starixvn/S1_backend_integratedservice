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
        public int Settled_Without_Bonus(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
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
                trans.Commit();
            }

            return result;

        }

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
                        //txId = item.txId,
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
                                trans,
                                AllowNegative.Allow
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
                    //txId = item.txId,
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
                    //txId = item.txId,
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
            var payout = (before.betAmount * -1) + before.winAmount + (after.betAmount * -1) + after.winAmount;

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
                //RefTxId = after.txId,
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

        public int RewardCF_AWC(RewardCF_AWC rewardCF_AWC, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in rewardCF_AWC.txns)
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    var p = new DynamicParameters(new
                    {
                        promotionTxId = item.promotionTxId,
                        promotionId = item.promotionId,
                        promotionTypeId = item.promotionTypeId,
                        platform = item.platform,
                        userId = item.userId,
                        amount = item.amount,
                        txUnix = new DateTimeOffset(item.txTime).ToUnixTimeSeconds()
                    });
                    result = conn.Execute("Service_RewardCF_AWC_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                    decimal amount = item.amount;
                    if (result > 0)
                    {
                        if (amount != 0)
                        {
                            result = walletService.SetBalance(
                                item.tpCustInfo.CustId,
                                item.tpCustInfo.SiteId,
                                item.tpCustInfo.UserName,
                                amount,
                                (int)Tp.AWC,
                                TransType.Prize,
                                $"Prize: {Math.Abs(amount):#,##0.##}",
                                item.promotionTxId,
                                out afterAmount,
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
                }
                catch (MySqlException mySqlEx)
                {
                    if (mySqlEx.Number == 1062)
                    {
                        result = -1;
                        continue;
                    }
                    else
                    {
                        isError = true;
                        log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_give");
                    }
                }
            }

            return isError ? 0 : result;
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

        #region One789

        public int PayoutLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                TicketType = (int)BettransType.Payout,
                TicketStatus = (bettingDataLT_One789.Amount > 0) ? (int)TicketStatus.Win : (int)TicketStatus.Lost,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
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
                    if (bettingDataLT_One789.Amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataLT_One789.Amount,
                            (int)Tp.One789,
                            mixUniqueId,
                            gameType,
                            category,
                            bettingDataLT_One789.Tx,
                            TransType.Payout,
                            null,
                            out afterAmount,
                            "Payout",
                            out bonusId,
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

        public int RefundLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            var gameParseTmp = bettingDataLT_One789.RefSource.Split("_");
            var p = new DynamicParameters(new
            {
                RefTx = bettingDataLT_One789.Tx,
                Tx = bettingDataLT_One789.Source,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = 1
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            string mixUniqueId = "";
            var category = TpCategory.Lottery;
            if (gameType == (int)GameType.One789_Lottery)
            {
                mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                result = conn.Execute("Service_BettransLT_One789_Refund_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else if (gameType == (int)GameType.One789_Lottery_789)
            {
                mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                result = conn.Execute("Service_BettransLT_One789_789_Refund_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }
            else
            {
                mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                category = TpCategory.LiveCasino;
                result = conn.Execute("Service_BettransLC_One789_Refund_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
            }

            if (result > 0)
            {
                if (bettingDataLT_One789.Amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataLT_One789.Amount,
                        (int)Tp.One789,
                        mixUniqueId,
                        gameType,
                        category,
                        bettingDataLT_One789.Tx,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "Refund",
                        out bonusId,
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

        public int ResettleLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var result = 0;
            var gameParseTmp = bettingDataLT_One789.RefSource.Split("_");
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
                TicketType = (int)BettransType.Payout,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = 2
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
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
                    if (bettingDataLT_One789.Amount != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            bettingDataLT_One789.Amount,
                            (int)Tp.One789,
                            mixUniqueId,
                            gameType,
                            category,
                            bettingDataLT_One789.Tx,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "Resettle",
                            out bonusId,
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

        public BettingDataLT_One789 GetRecordLT_One789(string source)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var data = conn.QueryFirstOrDefault<BettingDataLT_One789>("Service_BettransLT_One789_Get", new { Source = source }, commandType: CommandType.StoredProcedure);
            if (data == null)
            {
                data = conn.QueryFirstOrDefault<BettingDataLT_One789>("Service_BettransLT_One789_789_Get", new { Source = source }, commandType: CommandType.StoredProcedure);
            }
            if (data == null)
            {
                data = conn.QueryFirstOrDefault<BettingDataLT_One789>("Service_BettransLC_One789_Get", new { Source = source }, commandType: CommandType.StoredProcedure);
            }
            return data;
        }

        public IEnumerable<SettleLT_One789> GetUnsettledList_One789(int gameType)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_BettransLT_One789_Unsettle_Get";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_BettransLT_One789_789_Unsettle_Get";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_BettransLC_One789_Unsettle_Get";
                    break;
                default:
                    break;
            }
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Query<SettleLT_One789>($"{sp}", commandType: CommandType.StoredProcedure);
        }

        public SettleLT_One789 GetSettle_TpWagerOne789(int gameType, string TransId)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_TpWagerOne789_LT_Settle_Get";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_TpWagerOne789_LT_789_Settle_Get";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_TpWagerOne789_LC_Settle_Get";
                    break;
                default:
                    break;
            }
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.QueryFirstOrDefault<SettleLT_One789>($"{sp}", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerOne789(int gameType, string TransId)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_TpWagerOne789_LT_Settle_Set";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_TpWagerOne789_LT_789_Settle_Set";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_TpWagerOne789_LC_Settle_Set";
                    break;
                default:
                    break;
            }
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute($"{sp}", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_Bettrans_One789(int gameType, string TransId)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_BettransLT_One789_Settle_Set";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_BettransLT_One789_789_Settle_Set";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_BettransLC_One789_Settle_Set";
                    break;
                default:
                    break;
            }
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute($"{sp}", new { TransId = TransId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region OneGaming

        public int MoneyInPK_OneGaming(TpCustInfo tpCustInfo, BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var ticketType = (int)BettransType.Payout;

            var p = new DynamicParameters(new
            {
                amount = bettingDataPK_OneGaming.amount,
                transaction_id = bettingDataPK_OneGaming.transaction_id,
                game_key = bettingDataPK_OneGaming.game_key,
                game_name = bettingDataPK_OneGaming.game_name,
                bcode = bettingDataPK_OneGaming.bcode,
                player_id = bettingDataPK_OneGaming.player_id,
                ref_tx_id = bettingDataPK_OneGaming.ref_tx_id,
                dateUnix = bettingDataPK_OneGaming.dateUnix,
                TicketType = ticketType
            });

            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransPK_OneGaming_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    if (bettingDataPK_OneGaming.amount != 0 && p.Get<int>("result") == 1)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(bettingDataPK_OneGaming.amount),
                            (int)Tp.OneGaming,
                            TransType.Payout,
                            $"Transfer In: {Math.Abs(bettingDataPK_OneGaming.amount):#,##0.##}",
                            bettingDataPK_OneGaming.ref_tx_id ?? bettingDataPK_OneGaming.transaction_id,
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

        public int RollbackPK_OneGaming(TpCustInfo tpCustInfo, RollbackDataPK_OneGaming rollbackDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 1;
            var transactionId = rollbackDataPK_OneGaming.transaction_id.Replace("TimeOut_", "");

            var p = new DynamicParameters(new
            {
                type = rollbackDataPK_OneGaming.type,
                amount = rollbackDataPK_OneGaming.amount,
                transaction_id = transactionId,
                game_key = rollbackDataPK_OneGaming.game_key,
                game_name = rollbackDataPK_OneGaming.game_name,
                bcode = rollbackDataPK_OneGaming.bcode,
                player_id = rollbackDataPK_OneGaming.player_id,
                ref_tx_id = (string)null,
                dateUnix = rollbackDataPK_OneGaming.dateUnix
            });

            p.Add("result", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var amount = Math.Abs(rollbackDataPK_OneGaming.amount);
            if (string.Equals(rollbackDataPK_OneGaming.type, "MoneyIn", StringComparison.OrdinalIgnoreCase))
            {
                amount *= -1;
            }

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            conn.Execute("Service_BettransPK_OneGaming_Rollback_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (amount != 0 && p.Get<int>("result") == 1)
            {
                walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    amount,
                    (int)Tp.OneGaming,
                    TransType.Undo,
                    $"Rollback: {Math.Abs(rollbackDataPK_OneGaming.amount):#,##0.##}",
                    transactionId,
                    out afterAmount,
                    conn,
                    trans, AllowNegative.Allow
                );
            }

            trans.Commit();
            return result;
        }

        public IEnumerable<SettleDataPK_OneGaming> GetUnsettledListPK_OneGaming()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleDataPK_OneGaming>("Service_TpWagerPK_OneGaming_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_OneGaming(string ticket)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_OneGaming_Settle_Set", new { ticket = ticket }, commandType: CommandType.StoredProcedure);
        }

        public int Settled_OneGaming(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
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
                trans.Commit();
            }

            return result;
        }

        #endregion

        #region V8

        public IEnumerable<SettleDataPK_V8> GetUnsettledListPK_V8()
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Query<SettleDataPK_V8>("Service_TpWagerPK_V8_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int SetSettleFlag_TpWagerPK_V8(string gameId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            return conn.Execute("Service_TpWagerPK_V8_Settle_Set", new { GameID = gameId }, commandType: CommandType.StoredProcedure);
        }

        #endregion

        #region BTI

        public async Task<BTIResult> CancelReserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });
                p.Add("returnAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_CancelReserve_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    RetrunAmount = p.Get<decimal>("returnAmount"),
                };

                if (btiResult.SPResult < 1 || btiResult.RetrunAmount == 0)
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: btiResult.RetrunAmount,
                        tpId: (int)Tp.BTI,
                        tpTransId: request.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: request.reserve_id,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Cancel",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                if (btiResult.SetBalanceResult == 1)
                {
                    btiResult.AffterAmount = afterAmount;
                    trans.Commit();                    
                }

                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return default;
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        public async Task<BTIResult> CommitReserve_BTIAsync(TpCustInfo tpCustInfo, BTIRequestObj request)
        {
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new
                {
                    request.cust_id,
                    request.reserve_id,
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("returnAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_CommitReserve_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    RetrunAmount = p.Get<decimal>("returnAmount"),
                    Trx_id = p.Get<long>("trx_id"),
                };

                if (btiResult.SPResult < 1 )
                {
                    return btiResult;
                }

                if (btiResult.SPResult == 1 || btiResult.RetrunAmount <= 0)
                {
                    trans.Commit();
                    return btiResult;
                }

                //Commit金額跟Reserve金額不同需退回部分下注金額
                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: btiResult.RetrunAmount,
                        tpId: (int)Tp.BTI,
                        tpTransId: request.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: request.reserve_id,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Refund",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                btiResult.AffterAmount = afterAmount;
                if (btiResult.SetBalanceResult == 1)
                {                  
                    trans.Commit();
                }
                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return default;
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }

        public async Task<BTIResult> Credit_BTIAsync(TpCustInfo tpCustInfo, BTiCreditRequest credit)
        {
            try
            {
                using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(credit);
                p.Add("trx_id", dbType: DbType.Int64, direction: ParameterDirection.Output);
                p.Add("SPResult", dbType: DbType.Int16, direction: ParameterDirection.Output);

                _ = await conn.ExecuteAsync("Service_BettransSports_BTI_Credit_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                var btiResult = new BTIResult()
                {
                    SPResult = p.Get<short>("SPResult"),
                    Trx_id = p.Get<long>("trx_id"),
                };

                if (btiResult.SPResult < 1 )
                {
                    return btiResult;
                }

                btiResult.SetBalanceResult = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: credit.amount,
                        tpId: (int)Tp.BTI,
                        tpTransId: credit.reserve_id,
                        gameType: (int)GameType.BTI_Sports,
                        tpCategory: TpCategory.Sports,
                        refId: credit.reserve_id,
                        TransType.Payout,
                        betData: null,
                        out decimal afterAmount,
                        "Payout",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);

                btiResult.AffterAmount = afterAmount;
                if (btiResult.SetBalanceResult == 1)
                {
                    trans.Commit();
                }
                return btiResult;
            }
            catch (MySqlException mySqlEx)
            {
                /*Duplicate for key 'PRIMARY'*/
                if (mySqlEx.Number == 1062)
                {
                    return default;
                }
                else
                {
                    throw mySqlEx;
                }
            }
        }
        #endregion

    }
}
