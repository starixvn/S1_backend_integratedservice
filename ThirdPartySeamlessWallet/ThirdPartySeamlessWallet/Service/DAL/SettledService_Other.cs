using System.Data;
using System.Globalization;
using Dapper;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class SettledService
    {
        public int Settled_Without_Bonus(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = SettleTicket(tpCustInfo, betRecordInfo, trans);

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
            using var connWager = _wagerDbClient.GetMySqlConnection();
            connWager.Open();
            using var wagerTrans = connWager.BeginTransaction();

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = connWager.Execute("Service_TpWagerCF_Payout_Set", new { ticket_id = payoutData_CF.ticket_id }, transaction: wagerTrans, commandType: CommandType.StoredProcedure);
            if (result > 0)
            {
                var betData = _mainDbClient.QueryFirst<BetRecordData_CF>("Service_Bettrans_CF_Get", trans, new { ticket_id = payoutData_CF.ticket_id });
                _mainDbClient.Execute("Service_Bettrans_CF_Payout_Set", trans, new { ticket_id = payoutData_CF.ticket_id, Payout = payoutData_CF.payout, TicketStatus = betRes });
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
                    _wagerDbClient.CommitAndDisposeConnection(connWager, wagerTrans);
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                }
                else
                {
                    _wagerDbClient.RollbackAndDisposeConnection(connWager, wagerTrans);
                    _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                }
            }

            //using var conn_sec = _mainDbClient.GetMySqlConnection();
            //conn_sec.Execute("Service_Bettrans_CF_Payout_Set", new { ticket_id = payoutData_CF.ticket_id, Payout = payoutData_CF.payout, TicketStatus = betRes }, commandType: CommandType.StoredProcedure);

            return result;
        }

        public int Settled_CF(TpCustInfo tpCustInfo, SettleData_CF settleData_CF, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();

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
            result = SettleTicket(tpCustInfo, betRecordInfo, trans);

            if (result > 0)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }

            _wagerDbClient.Execute("Service_TpWagerCF_Settle_Set", commandType: CommandType.StoredProcedure, new { ticket_id = settleData_CF.ticket_id });

            _mainDbClient.Execute("Service_Bettrans_CF_Settle_Set", commandType: CommandType.StoredProcedure, new { ticket_id = settleData_CF.ticket_id, TicketStatus = betRes });

            return result;
        }

        public IEnumerable<PayoutData_CF> GetUnPayoutList_CF()
        {
            return _wagerDbClient.Query<PayoutData_CF>("Service_TpWagerCF_PayoutList_Get", commandType: CommandType.StoredProcedure);
        }

        public IEnumerable<SettleData_CF> GetUnSettleList_CF()
        {
            return _wagerDbClient.Query<SettleData_CF>("Service_TpWagerCF_SettleList_Get", commandType: CommandType.StoredProcedure);
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
                using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_Payout_Set", trans, p);
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
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_settle", mySqlEx);
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_UnPayout_Set", trans, p);
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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_unsettle", mySqlEx);
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_VoidPayout_Set", trans, p);

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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_voidSettle", mySqlEx);
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_UnvoidPayout_Set", trans, p);

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
                        continue;
                    }
                }
                catch (MySqlException mySqlEx)
                {
                    isError = true;
                    log2File.Error($"{JsonConvert.SerializeObject(item)}\r\n{mySqlEx}", $"AWC_unvoidSettle", mySqlEx);
                }
            }

            return isError ? 0 : result;
        }

        public int RefundCF_AWC(TpCustInfo tpCustInfo, RefundCF_AWC refundCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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

            result = _mainDbClient.Execute("Service_BettransCF_AWC_Refund_Set", trans, p);

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

            return result;
        }

        public int RewardCF_AWC(RewardCF_AWC rewardCF_AWC, out decimal afterAmount)
        {
            bool isError = false;
            afterAmount = 0;
            var result = 0;
            foreach (var item in rewardCF_AWC.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_RewardCF_AWC_Ins", trans, p);
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
            return _mainDbClient.Query<SettleCF_AWC>("Service_BettransCF_AWC_Unsettle_Get", commandType: CommandType.StoredProcedure);
        }

        public int Settled_AWC(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            result = SettleTicket(tpCustInfo, betRecordInfo, trans);

            if (result > 0)
            {
                result = _mainDbClient.Execute("Service_BettransCF_AWC_Settle_Set", trans, new { platformTxId = betRecordInfo.TransId });
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

        #endregion

        #region One789

        public int PayoutLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransLT_One789_Ins", trans, p);
                }
                else if (gameType == (int)GameType.One789_Lottery_789)
                {
                    mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    result = _mainDbClient.Execute("Service_BettransLT_One789_789_Ins", trans, p);
                }
                else
                {
                    mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    category = TpCategory.LiveCasino;
                    result = _mainDbClient.Execute("Service_BettransLC_One789_Ins", trans, p);
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
                            $"Payout {mixUniqueId}",
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
                if (mySqlEx.Number == 1062)/*Duplicate for key 'PRIMARY'*/
                {
                    result = -1;
                }
                else
                {
                    logger.LogError(mySqlEx, mySqlEx.Message);
                    result = 0;
                }
            }

            return result;
        }

        public int RefundLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            var result = 0;
            var gameParseTmp = bettingDataLT_One789.RefSource.Split("_");
            var p = new DynamicParameters(new
            {
                RefTx = bettingDataLT_One789.Tx,
                Tx = bettingDataLT_One789.Source,
                TicketStatus = (int)TicketStatus.Refund,
                SettleFlag = (gameType == (int)GameType.One789_LiveCasino) ? SettleFlag.Settled : SettleFlag.EndGame
            });

            conn.Open();
            using var trans = conn.BeginTransaction();
            string mixUniqueId = "";
            var category = TpCategory.Lottery;
            if (gameType == (int)GameType.One789_Lottery)
            {
                mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                result = _mainDbClient.Execute("Service_BettransLT_One789_Refund_Set", trans, p);
            }
            else if (gameType == (int)GameType.One789_Lottery_789)
            {
                mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                result = _mainDbClient.Execute("Service_BettransLT_One789_789_Refund_Set", trans, p);
            }
            else
            {
                mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                category = TpCategory.LiveCasino;
                result = _mainDbClient.Execute("Service_BettransLC_One789_Refund_Set", trans, p);
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
                        $"Refund {mixUniqueId}",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                     );
                }

                if (result > 0)
                {
                    _ = _tpUnsettleService.InsertTpUnsettle(Tp.One789, (GameType)gameType, mixUniqueId, trans);

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

            return result;
        }

        public int ResettleLT_One789(TpCustInfo tpCustInfo, BettingDataLT_One789 bettingDataLT_One789, int gameType, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransLT_One789_Ins", trans, p);
                }
                else if (gameType == (int)GameType.One789_Lottery_789)
                {
                    mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    result = _mainDbClient.Execute("Service_BettransLT_One789_789_Ins", trans, p);
                }
                else
                {
                    mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    category = TpCategory.LiveCasino;
                    result = _mainDbClient.Execute("Service_BettransLC_One789_Ins", trans, p);
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
                            $"Resettle {mixUniqueId}",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow
                         );
                    }

                    if (result > 0)
                    {
                        _ = _tpUnsettleService.InsertTpUnsettle(Tp.One789, (GameType)gameType, mixUniqueId, trans);

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
                    logger.LogError(mySqlEx, mySqlEx.Message);
                    result = 0;
                }
            }

            return result;
        }

        public BettingDataLT_One789 GetRecordLT_One789(string source)
        {
            var data = _mainDbClient.QueryFirst<BettingDataLT_One789>("Service_BettransLT_One789_Get", new { Source = source });
            if (data == null)
            {
                data = _mainDbClient.QueryFirst<BettingDataLT_One789>("Service_BettransLT_One789_789_Get", new { Source = source });
            }
            if (data == null)
            {
                data = _mainDbClient.QueryFirst<BettingDataLT_One789>("Service_BettransLC_One789_Get", new { Source = source });
            }
            return data;
        }

        public SettleData GetUnsettledList_One789(int gameType, string TransId)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_BettransLT_One789_Unsettle_Get_V4";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_BettransLT_One789_789_Unsettle_Get_V4";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_BettransLC_One789_Unsettle_Get_V4";
                    break;
                default:
                    break;
            }

            return _mainDbClient.QueryFirst<SettleData>($"{sp}", new { TransId = TransId });
        }

        public SettleData_One789 GetSettle_TpWagerOne789(int gameType, string TransId)
        {
            string sp = "";
            switch (gameType)
            {
                case (int)GameType.One789_Lottery:
                    sp = "Service_TpWagerOne789_LT_Settle_Get_V3";
                    break;
                case (int)GameType.One789_Lottery_789:
                    sp = "Service_TpWagerOne789_LT_789_Settle_Get_V3";
                    break;
                case (int)GameType.One789_LiveCasino:
                    sp = "Service_TpWagerOne789_LC_Settle_Get_V3";
                    break;
                default:
                    break;
            }
            return _wagerDbClient.QueryFirst<SettleData_One789>($"{sp}", new { TransId = TransId });
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
            return _wagerDbClient.Execute($"{sp}", commandType: CommandType.StoredProcedure, new { TransId = TransId });
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
            return _mainDbClient.Execute($"{sp}", commandType: CommandType.StoredProcedure, new { TransId = TransId });
        }

        #endregion

        #region TCG

        public async Task<TCGResult> Credit_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGTransactionData data)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

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
                    TransType = TCGTransType.Credit.ToString(),
                    TicketType = (int)BettransType.Payout,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                var bettransTCG = await _mainDbClient.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", trans, p);
                if (bettransTCG != null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionAlreadyExists,
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransLottery_TCG_Ins", trans, p);

                if (result > 0 && data.amount != 0)
                {
                    // 先取回下注的ref_no
                    var bet_ref_no = _mainDbClient.QueryFirst<string>("Service_BettransLottery_TCG_ReferenceNo_Get", new { TpUserName = data.username, OrderNum = data.order_no });
                    result = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: data.amount,
                        tpId: (int)Tp.TCG,
                        tpTransId: bet_ref_no,
                        gameType: (int)gameType,
                        tpCategory: TpCategory.Lottery,
                        refId: data.order_no,
                        TransType.Payout,
                        betData: null,
                        out decimal afterAmount,
                        "Payout",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
                }

                if (result > 0)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return new TCGResult()
                {
                    Result = result > 0 ? TCGReturnCode.Success : TCGReturnCode.UnknownSystemError,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Credit_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        public async Task<TCGResult> CancelReserve_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGReservationDetail data)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    TransType = TCGTransType.Reserve.ToString(),
                    data.username,
                    data.ref_no,
                    data.amount,
                    TicketType = (int)BettransType.CancelBet,
                    TicketStatus = (int)TicketStatus.Refund,
                    SettleFlag = (int)SettleFlag.Settled
                });

                var bettransTCG = await _mainDbClient.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", trans, p);
                if (bettransTCG == null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionNotFound,
                    };
                }
                if (bettransTCG.TicketType == (int)BettransType.CancelBet)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.Success,
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransLottery_TCG_CancelReserve_Set", trans, p);
                
                if (result > 0 && data.amount != 0)
                {
                    // 先取回下注的ref_no
                    var bet_ref_no = _mainDbClient.QueryFirst<string>("Service_BettransLottery_TCG_ReferenceNo_Get", new { TpUserName = data.username, OrderNum = data.order_no });

                    result = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: data.amount,
                        tpId: (int)Tp.TCG,
                        tpTransId: bet_ref_no,
                        gameType: (int)gameType,
                        tpCategory: TpCategory.Lottery,
                        refId: data.order_no,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Cancel",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
                }
                
                if (result > 0)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return new TCGResult()
                {
                    Result = result > 0 ? TCGReturnCode.Success : TCGReturnCode.UnknownSystemError,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"CancelReserve_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        public async Task<TCGResult> ConfirmReserve_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGReservationDetail data)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    TransType = TCGTransType.Reserve.ToString(),
                    data.username,
                    data.ref_no,
                    data.amount,
                    TicketType = (int)BettransType.ConfirmReserve,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.EndGame
                });

                var bettransTCG = await _mainDbClient.QueryFirstOrDefaultAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", trans, p);
                if (bettransTCG == null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionNotFound,
                    };
                }
                if (bettransTCG.TicketType == (int)BettransType.Bet)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.Success,
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransLottery_TCG_ConfirmReserve_Set", trans, p);

                //Confirm金額跟Reserve金額不同需退回部分下注金額
                var returnAmount = bettransTCG.amount - data.amount;
                if (result > 0 && returnAmount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo: tpCustInfo,
                        amount: returnAmount,
                        tpId: (int)Tp.TCG,
                        tpTransId: data.ref_no,
                        gameType: (int)gameType,
                        tpCategory: TpCategory.Lottery,
                        refId: data.order_no,
                        TransType.Refunds,
                        betData: null,
                        out decimal afterAmount,
                        "Refund",
                        out int bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
                }

                if (result > 0)
                {
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return new TCGResult()
                {
                    Result = result > 0 ? TCGReturnCode.Success : TCGReturnCode.UnknownSystemError,
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"ConfirmReserve_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }
        #endregion

        public async Task<SBOResult> CancelBetLiveCoin_SBO(TpCustInfo tpCustInfo, CancelSportDto_SBO dto)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                var p = new DynamicParameters(new
                {
                    dto.TransferCode
                });

                var result = await _mainDbClient.ExecuteAsync("Service_BettransLiveCoin_SBO_Cancel_Set", trans, p);

                var sboResult = new SBOResult();
                if (result < 1)
                {
                    sboResult.Result = -1;
                    return sboResult;
                }

                result = walletService.SetBalance(
                    tpCustInfo.CustId,
                    tpCustInfo.SiteId,
                    tpCustInfo.UserName,
                    Math.Abs(dto.Amount),
                    (int)Tp.SBO,
                    TransType.Refunds,
                    $"Cancel {dto.Amount:#,##0.##}",
                    dto.TransferCode,
                    out var afterAmount,
                    conn,
                    trans);

                if (result == 1)
                {
                    sboResult.AfterAmount = afterAmount;
                    sboResult.Result = 1;
                    await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                }
                else
                {
                    await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                }

                return sboResult;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }

        #region GPI

        public int PayoutLT_GPI(TpCustInfo tpCustInfo, SettledDataRNG_GPI settledDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            int bonusId = 0;
            var category = TpCategory.Lottery;

            afterAmount = 0;
            TransId = settledDataRNG_GPI.trx_id;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(settledDataRNG_GPI);
            p.Add("TicketStatus", (int)TicketStatus.Win);
            p.Add("TicketType", (int)BettransType.Payout);

            try
            {
                result = _mainDbClient.Execute("Service_BettransLT_GPI_Ins", trans, p);

                if (result > 0)
                {
                    if (settledDataRNG_GPI.amount > 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            settledDataRNG_GPI.amount,
                            (int)Tp.GPI,
                            settledDataRNG_GPI.round_id,
                            gameType,
                            category,
                            settledDataRNG_GPI.round_id,
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
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }

        public int VoidBetLT_GPI(TpCustInfo tpCustInfo, BettingDataRNG_GPI bettingDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            int bonusId = 0;
            var category = TpCategory.Lottery;

            afterAmount = 0;
            TransId = bettingDataRNG_GPI.trx_id;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(bettingDataRNG_GPI);
            p.Add("TicketStatus", (int)TicketStatus.Refund);
            p.Add("TicketType", (int)BettransType.Payout);

            try
            {
                var check = _mainDbClient.Query<BettransData_GPI>("Service_BettransLT_GPI_Get", commandType: CommandType.StoredProcedure, new { round_id = bettingDataRNG_GPI.round_id });
                if (!check.Any())
                {
                    // no bet data
                    return -2;
                }
                var subChk = check.Where(x => x.TicketType == (int)BettransType.Payout && x.TicketStatus == (int)TicketStatus.Refund);
                if (subChk.Any())
                {
                    // had void
                    return -1;
                }

                result = _mainDbClient.Execute("Service_BettransLT_GPI_Ins", trans, p);
                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_GPI.amount,
                        (int)Tp.GPI,
                        bettingDataRNG_GPI.round_id,
                        gameType,
                        category,
                        bettingDataRNG_GPI.round_id,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "VoidBet",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );

                    if (result > 0)
                    {
                        _ = _tpUnsettleService.InsertTpUnsettle(Tp.GPI, (GameType)gameType, bettingDataRNG_GPI.round_id, trans);
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
                throw mySqlEx;
            }

            return result;
        }

        public int EditBetLT_GPI(TpCustInfo tpCustInfo, BettingDataRNG_GPI bettingDataRNG_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            int bonusId = 0;
            var category = TpCategory.Lottery;

            afterAmount = 0;
            TransId = bettingDataRNG_GPI.trx_id;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(bettingDataRNG_GPI);
            p.Add("TicketStatus", (int)TicketStatus.UnSettled);
            p.Add("TicketType", (int)BettransType.Payout);

            try
            {
                var check = _mainDbClient.Query<BettransData_GPI>("Service_BettransLT_GPI_Get", commandType: CommandType.StoredProcedure, new { round_id = bettingDataRNG_GPI.round_id });
                if (!check.Any())
                {
                    // no bet data
                    return -2;
                }
                var subChk = check.Where(x => x.Edit == 1);
                if (subChk.Any())
                {
                    // had edit
                    return -1;
                }

                result = _mainDbClient.Execute("Service_BettransLT_GPI_EditBet_Set", trans, p);

                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        bettingDataRNG_GPI.amount,
                        (int)Tp.GPI,
                        bettingDataRNG_GPI.round_id,
                        gameType,
                        category,
                        bettingDataRNG_GPI.round_id,
                        TransType.Undo,
                        null,
                        out afterAmount,
                        "EditBet",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
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
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }

            return result;
        }

        public int CancelBetLT_GPI(TpCustInfo tpCustInfo, CancelBet_GPI cancelBet_GPI, int gameType, out decimal afterAmount, out string TransId)
        {
            int bonusId = 0;
            var category = TpCategory.Lottery;

            afterAmount = 0;
            TransId = cancelBet_GPI.trx_id;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var p = new DynamicParameters(cancelBet_GPI);
            p.Add("TicketStatus", (int)TicketStatus.Refund);
            p.Add("TicketType", (int)BettransType.CancelBet);

            try
            {
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _mainDbClient.Execute("Service_BettransLT_GPI_CancelBet_Set", trans, p);

                if (result > 0)
                {
                    decimal stake = p.Get<decimal>("out_stake");
                    if (stake != 0)
                    {
                        result = PayoutV2(
                            tpCustInfo,
                            Math.Abs(stake),
                            (int)Tp.GPI,
                            cancelBet_GPI.round_id,
                            gameType,
                            category,
                            cancelBet_GPI.round_id,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "CancelBet",
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
                else
                {
                    var check = _mainDbClient.Query<BettransData_GPI>("Service_BettransLT_GPI_ByTrxId_Get", commandType: CommandType.StoredProcedure, new { trx_id = cancelBet_GPI.trx_id });
                    var subChk = check.Where(x => x.TicketType == 2);
                    if (subChk.Any())
                    {
                        // had cancel
                        return -1;
                    }
                    if (!check.Any())
                    {
                        // no bet data insert cancel
                        var cancelParam = new DynamicParameters(new
                        {
                            trx_id = cancelBet_GPI.trx_id,
                            cust_id = cancelBet_GPI.cust_id,
                            currency = !string.IsNullOrEmpty(cancelBet_GPI.currency) ? cancelBet_GPI.currency : tpCustInfo.TpCurrencyId,
                            amount = 0,
                            Operator = cancelBet_GPI.Operator,
                            game_id = cancelBet_GPI.game_id,
                            round_id = cancelBet_GPI.round_id,
                            platform = "",
                            TicketType = (int)TransferType.CancelBet,
                            TicketStatus = (int)TicketStatus.Refund,
                        });

                        var cancelInsert = _mainDbClient.Execute("Service_BettransLT_GPI_Ins", trans, cancelParam);
                        if (cancelInsert > 0)
                        {
                            // insert cancel record
                            _mainDbClient.CommitAndDisposeConnection(conn, trans);
                        }
                        else
                        {
                            _mainDbClient.RollbackAndDisposeConnection(conn, trans);
                        }
                        return -2;
                    }
                }
            }
            catch (MySqlException mySqlEx)
            {
                throw mySqlEx;
            }

            return result;
        }

        #endregion
        
        #region 93Connect
        
        public int PayoutGames_93Connect(TpCustInfo tpCustInfo, NTCSettleBetRequest settleData_NTConnect, out decimal afterAmount)
        {
            var bonusId = 0;
            var tpId = Tp.NTConnect;
            var tpGameType = GameType.NTConnect_Games;
            var category = TpCategory.Games;
            var InsSpName = "Service_Bettrans_93Connect_Payout_Set";
            var GetSpName = "Service_Bettrans_93Connect_Get";
            afterAmount = 0;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.Now;
                var bettrans = _partitionService.Query<Bettrans93Connect>(partitionTime, GetSpName, 
                    new { bet_id = settleData_NTConnect.bet_id , username = tpCustInfo.TpUserName });
                if (!bettrans.Any(i => i.bet_id == settleData_NTConnect.bet_id))
                {
                    return -2;
                }
                
                if (bettrans.Any(i => i.TicketType != (int)BettransType.Bet))
                {
                    return -3;
                }

                var settleData = new NTCSettleBetRequest
                {
                    bet_id = settleData_NTConnect.bet_id,
                    bet_amt = settleData_NTConnect.bet_amt,
                    win_amt = settleData_NTConnect.win_amt,
                    username = tpCustInfo.TpUserName,
                    status = settleData_NTConnect.NtcStatus,
                    settleBetTime = settleData_NTConnect.settleBetTime,
                    LocalSettleTime = settleData_NTConnect.LocalSettleTime,
                    TicketStatus = settleData_NTConnect.TicketStatus,
                    TicketType = settleData_NTConnect.TicketType,
                };
                
                var p = new DynamicParameters(settleData);
                p.Add("SettleFlag",(int)SettleFlag.EndGame);
                result = _partitionService.Execute(partitionTime,InsSpName, p, trans);

                if (result > 0)
                {
                    switch (settleData_NTConnect.status)
                    {
                        case "cancel" or "refund":
                            var textInfo = new CultureInfo("en-US", false).TextInfo;
                            result = PayoutV2(
                                tpCustInfo,
                                Math.Abs(settleData_NTConnect.bet_amt),
                                (int)tpId,
                                settleData_NTConnect.bet_id,
                                (int)tpGameType,
                                category,
                                settleData_NTConnect.bet_id,
                                TransType.Refunds,
                                null,
                                out afterAmount,
                                textInfo.ToTitleCase(settleData_NTConnect.status),
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
                        case "win" or "loss":
                            result = PayoutV2(
                                tpCustInfo,
                                settleData_NTConnect.win_amt,
                                (int)tpId,
                                settleData_NTConnect.bet_id,
                                (int)tpGameType,
                                category,
                                settleData_NTConnect.bet_id,
                                TransType.Payout,
                                null,
                                out afterAmount,
                                $"Payout",
                                out bonusId,
                                conn,
                                trans,
                                AllowNegative.Allow
                            );
                            break;
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
                    result = -1;
                }
                else
                {
                    throw mySqlEx;
                }
            }

            return result;
        }


        public int CancelBetGames_93Connect(TpCustInfo tpCustInfo, NTCCancelBetRequest cancelData_NTConnect,
            out decimal afterAmount, out string TransId)
        {
            var bonusId = 0;
            var tpId = Tp.NTConnect;
            var tpGameType = GameType.NTConnect_Games;
            var category = TpCategory.Games;
            var CancelSpName = "Service_Bettrans_93Connect_CancelBet_Set";
            var GetSpName = "Service_Bettrans_93Connect_Get";
            afterAmount = 0;
            
            TransId = cancelData_NTConnect.bet_id;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var pc = new DynamicParameters();
                pc.Add("username",tpCustInfo.TpUserName);
                pc.Add("bet_id",cancelData_NTConnect.bet_id);
            
                
                var partitionTime = DateTime.Now;
                var bettrans = _partitionService.Query<Bettrans93Connect>(partitionTime, GetSpName, 
                    new { bet_id = cancelData_NTConnect.bet_id , username = tpCustInfo.TpUserName });
                if (!bettrans.Any(i => i.bet_id == cancelData_NTConnect.bet_id))
                {
                    // no bet data
                    return -2;
                }
                
                if (bettrans.Any(i => i.TicketType == (int)BettransType.CancelBet))
                {
                    return -3;
                }
                
                if(bettrans.Any(i => i.TicketType == (int)BettransType.Bet))
                {
                    return -5;
                }
                
                var cancelData = new Bettrans93ConnectCancelBet()
                {
                    bet_id = cancelData_NTConnect.bet_id,
                    username = tpCustInfo.TpUserName,
                    status = cancelData_NTConnect.status,
                    TicketType = cancelData_NTConnect.TicketType,
                    TicketStatus = cancelData_NTConnect.TicketStatus,
                    SettleFlag = (int)SettleFlag.EndGame
                };
                
                var p = new DynamicParameters(cancelData);
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime,CancelSpName, p, trans);
    
                if (result > 0)
                {
                    var stake = p.Get<decimal>("out_stake");

                    result = bettrans.FirstOrDefault().status switch
                    {
                        "W" => PayoutV2(
                            tpCustInfo,
                            (Math.Abs(bettrans.FirstOrDefault().win_amt) - Math.Abs(bettrans.FirstOrDefault().stake))* -1,
                            (int)tpId,
                            cancelData_NTConnect.bet_id,
                            (int)tpGameType,
                            category, 
                            cancelData_NTConnect.bet_id,
                            TransType.Undo,
                            null,
                            out afterAmount,
                            "CancelBet",
                            out bonusId,
                            conn,
                            trans, 
                            AllowNegative.Allow),
                        "L" => PayoutV2(
                            tpCustInfo,
                            Math.Abs(stake),
                            (int)tpId,
                            cancelData_NTConnect.bet_id,
                            (int)tpGameType,
                            category,
                            cancelData_NTConnect.bet_id,
                            TransType.Refunds,
                            null,
                            out afterAmount,
                            "CancelBet",
                            out bonusId,
                            conn,
                            trans,
                            AllowNegative.Allow),
                        _ => 0
                    };

                    if (result > 0)
                    {
                        //CancelBet是SettleBet之後才會打，要重新結算注單
                        _tpUnsettleService.InsertTpUnsettle(Tp.NTConnect, GameType.NTConnect_Games, TransId, trans);
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
        
        #region CQ9 WCC
        public int RollinWCC_CQ9(TpCustInfo tpCustInfo, RollinDataFish_CQ9 rollinDataWCC_CQ9, int gameType, ref decimal afterAmount)
        {
            var tpId = Tp.CQ9;
            //var tpGameType = GameType.CQ9_WCC;
            var tpCategory = TpCategory.Cockfighting;
            var result = 0;
            int bonusId = 0;
            afterAmount = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                if (rollinDataWCC_CQ9.amount > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        rollinDataWCC_CQ9.amount,
                        (int)tpId,
                        rollinDataWCC_CQ9.roundid,
                        gameType,
                        tpCategory,
                        rollinDataWCC_CQ9.roundid,
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

                if (result > 0 || rollinDataWCC_CQ9.amount == 0)
                {
                    decimal winLose = rollinDataWCC_CQ9.win;
                    var ticketStatus = winLose switch
                    {
                        < 0 => (int)TicketStatus.Lost,
                        0 => (int)TicketStatus.Draw,
                        _ => (int)TicketStatus.Win
                    };

                    var p = new DynamicParameters(new
                    {
                        account = rollinDataWCC_CQ9.account,
                        gamehall = rollinDataWCC_CQ9.gamehall,
                        gamecode = rollinDataWCC_CQ9.gamecode,
                        roundid = rollinDataWCC_CQ9.roundid,
                        validbet = rollinDataWCC_CQ9.validbet,
                        bet = Math.Abs(rollinDataWCC_CQ9.bet) * -1,
                        win = rollinDataWCC_CQ9.win,
                        roomfee = rollinDataWCC_CQ9.roomfee,
                        amount = rollinDataWCC_CQ9.amount,
                        mtcode = rollinDataWCC_CQ9.mtcode,
                        rake = rollinDataWCC_CQ9.rake,
                        gametype = rollinDataWCC_CQ9.gametype,
                        eventUnix = rollinDataWCC_CQ9.eventUnix,
                        createUnix = rollinDataWCC_CQ9.createUnix,
                        before = afterAmount - (Math.Abs(rollinDataWCC_CQ9.amount)),
                        balance = afterAmount,
                        Actions = "rollin",
                        TicketStatus = ticketStatus,
                        TicketType = (int)BettransType.Payout,
                        SettleFlag = (int)SettleFlag.EndGame
                    });

                    result = _mainDbClient.Execute("Service_BettransCF_CQ9_Payout_Ins", trans, p);
                    
                    if (result > 0)
                    {
                        result = _tpUnsettleService.InsertTpUnsettle(Tp.CQ9, GameType.CQ9_WCC, rollinDataWCC_CQ9.roundid, trans);
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
        
        public int RefundWCC_CQ9(TpCustInfo tpCustInfo, RefundDataRNG_CQ9 refundDataRNG_CQ9, out decimal afterAmount)
        {
            var tpId = Tp.CQ9;
            var tpGameType = GameType.CQ9_WCC;
            var tpCategory = TpCategory.Cockfighting;
            int bonusId = 0;
            afterAmount = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = _mainDbClient.Execute("Service_BettransCF_CQ9_Refund_Set", trans, new
            {
                mtcode = refundDataRNG_CQ9.mtcode,
                before = afterAmount - Math.Abs(refundDataRNG_CQ9.amount),
                balance = afterAmount,
            });

            if (result > 0)
            {
                if (refundDataRNG_CQ9.amount != 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        Math.Abs(refundDataRNG_CQ9.amount),
                        (int)tpId,
                        refundDataRNG_CQ9.roundid,
                        (int)tpGameType,
                        tpCategory,
                        refundDataRNG_CQ9.roundid,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "Refund",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow);
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
        
        #endregion
        
        #region EdayLT
        
        public int BetRequestCancel_Eday(TpCustInfo tpCustInfo, EdayBetRequestData cancelData_Eday, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.Eday;
            var tpGameType = GameType.Eday_Lottery;
            var tpCategory = TpCategory.Lottery;
            var CancelSpName = "Service_BettransLT_Eday_CancelBet_Set";
            var GetSpName = "Service_BettransLT_Eday_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                // 檢查是否有注單
                var bettrans = _mainDbClient.Query<BettransLTEdayDAL>(GetSpName,
                    commandType: CommandType.StoredProcedure,new { billId = cancelData_Eday.billId, player = tpCustInfo.TpUserName });
                
                if (!bettrans.Any(i => i.billId == cancelData_Eday.billId))
                {
                    return -2;
                }
                
                if (bettrans.Any(i => i.TicketType == (int)BettransType.CancelBet))
                {
                    return -3;
                }

                var cancelTransId = $"{cancelData_Eday.billId}_{cancelData_Eday.roundCode}";
                var cancelData = new BettransLTEdayCancelBet()
                {
                    transId = cancelTransId,
                    player = tpCustInfo.TpUserName,
                    TicketType = cancelData_Eday.TicketType,
                    TicketStatus = cancelData_Eday.TicketStatus,
                    SettleFlag = (int)SettleFlag.EndGame
                };
                
                var cancelBetId = $"{cancelData_Eday.roundCode}_{cancelData_Eday.player}";
                var p = new DynamicParameters(cancelData);
                result = _mainDbClient.Execute(CancelSpName, p, conn ,trans);

                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        cancelData_Eday.amount,
                        (int)tpId,
                        //為了取消bonus
                        cancelTransId,
                        (int)tpGameType,
                        tpCategory,
                        cancelBetId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        $"CancelBet {cancelData_Eday.billId}",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    //CancelBet 不送TpUnSettle 等Wager抓單回來一起處理即可
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
        
        public int RoundUpdate_Eday(TpCustInfo tpCustInfo, BettransLTEdayRoundUpdateDAL settleData_Eday, out decimal afterAmount)
        {
            var bonusId = 0;
            var tpId = Tp.Eday;
            var tpGameType = GameType.Eday_Lottery;
            var category = TpCategory.Lottery;
            var InsSpName = "Service_BettransLT_Eday_Ins";
            var GetSpName = "Service_BettransLT_Eday_Round_Get";
            afterAmount = 0;

            var result = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var settleBillId = $"{settleData_Eday.roundCode}_{settleData_Eday.player}";
                var refTransId = $"{settleData_Eday.roundCode}_{settleData_Eday.player}" ;
                var bettransLTEday = _mainDbClient.Query<BettransLTEdayDAL>(GetSpName, 
                    commandType: CommandType.StoredProcedure,new { roundCode = settleData_Eday.roundCode , player = tpCustInfo.TpUserName });

                if (!bettransLTEday.Any())
                {
                    return -2;
                }
                
                //RoundCode整局沒有全取消 就繼續
                if (bettransLTEday.All(i => i.TicketType == (int)BettransType.CancelBet))
                {
                    return -3;
                }

                if (bettransLTEday.Any(i => i.billId == settleBillId))
                {
                    switch (settleData_Eday.eventId)
                    {
                        case 0:
                            return -4;
                        case > 0:
                            settleBillId = $"{settleBillId}_{settleData_Eday.eventId}";
                            break;
                    }
                }

                //拿最後一筆的且不是取消的billId當派彩單號，要不然有Bonus的話Bonus Payout吃不到
                var lastUnCancelBillId = bettransLTEday
                    .Where(i => i.TicketType != (int)BettransType.CancelBet)
                    .Select(i => i.billId)
                    .LastOrDefault();                                       

                var settleEdayData = new BettransLTEdayPayoutRequest
                {
                    txId = settleData_Eday.txId,
                    //RoundUpdate沒有token拿第一筆的token當做Settle Token
                    token = bettransLTEday.FirstOrDefault().token,
                    //結算時沒有billId,目前想法是RoundCode + playerId 當作結算billId.
                    billId = settleBillId,
                    transId = settleBillId,
                    player = settleData_Eday.player,
                    //amount 為 RoundUpdate 的 betTotal
                    amount = settleData_Eday.bet,
                    timestamp = settleData_Eday.timestamp,
                    roundCode = settleData_Eday.roundCode,
                    gameCode = settleData_Eday.gameCode,
                    // 官方文件上無3，創一個3當作Settle標記
                    betType = 3,
                    eventId = settleData_Eday.eventId,
                    betTotal = settleData_Eday.bet,
                    winTotal = settleData_Eday.win,
                    ticketCount = 0,
                    //UTCBetTime 拿第一筆的UTCBetTime 當作下注時間
                    UTCBetTime = bettransLTEday.FirstOrDefault().UTCBetTime,
                    UTCSettleTime = settleData_Eday.UTCSettleBetTime,
                    TicketStatus = settleData_Eday.TicketStatus,
                    TicketType = settleData_Eday.TicketType,
                    SettleFlag = settleData_Eday.SettleFlag
                };
                
                var p = new DynamicParameters(settleEdayData);
                result = _mainDbClient.Execute(InsSpName, p, conn,trans);

                if (result > 0)
                {
                    var payoutAmount = 0.00m;
                    //Win = 總贏額，代表此次Round結算後要派給用戶的金額，用這個數字派發給用戶即可。
                    if (settleData_Eday.win > 0.00m)
                    {
                        payoutAmount = settleData_Eday.win;
                    }
                    
                    result = PayoutV2(
                        tpCustInfo,
                        payoutAmount,
                        (int)tpId,
                        $"{lastUnCancelBillId}_{settleData_Eday.roundCode}",
                        (int)tpGameType,
                        category,
                        refTransId,
                        TransType.Payout,
                        null,
                        out afterAmount,
                        $"Payout",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
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
                    throw mySqlEx;
                }
            }

            return result;
        }
        
        public int RoundUpdateVoidRound_Eday(TpCustInfo tpCustInfo, BettransLTEdayRoundUpdateDAL voidRoundData_Eday, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.Eday;
            var tpGameType = GameType.Eday_Lottery;
            var tpCategory = TpCategory.Lottery;
            var CancelSpName = "Service_BettransLT_Eday_CancelBet_Set";
            var GetSpName = "Service_BettransLT_Eday_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                //針對派彩單
                var voidRoundId = $"{voidRoundData_Eday.roundCode}_{voidRoundData_Eday.player}";
                
                // 檢查是否有注單
                var bettrans = _mainDbClient.Query<BettransLTEdayDAL>( 
                    CancelSpName, commandType: CommandType.StoredProcedure,new { billId = voidRoundId, player = tpCustInfo.TpUserName });
                
                //沒注單無法取消
                if (!bettrans.Any())
                {
                    return -2;
                }
                
                //全取消就不用動了
                if (bettrans.All(i => i.TicketType == (int)BettransType.CancelBet))
                {
                    return -3;
                }
                
                var cancelData = new BettransLTEdayRoundCancelBet()
                {
                    roundCode = voidRoundData_Eday.roundCode,
                    player = tpCustInfo.TpUserName,
                    TicketType = voidRoundData_Eday.TicketType,
                    TicketStatus = voidRoundData_Eday.TicketStatus,
                    SettleFlag = (int)SettleFlag.EndGame
                };
                
                var p = new DynamicParameters(cancelData);
                result = _mainDbClient.Execute(CancelSpName, p, conn,trans);
                
                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        //此function為把payout金額收回
                        voidRoundData_Eday.win * -1,
                        (int)tpId,
                        $"{voidRoundId}",
                        (int)tpGameType,
                        tpCategory,
                        voidRoundId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "VoidRound",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    //CancelBet 不送TpUnSettle 等Wager抓單回來一起處理即可
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
        
        public int RoundUpdateVoidDraw_Eday(TpCustInfo tpCustInfo, BettransLTEdayRoundUpdateDAL voidDrawData_Eday, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.Eday;
            var tpGameType = GameType.Eday_Lottery;
            var tpCategory = TpCategory.Lottery;
            var CancelRoundSpName = "Service_BettransLT_Eday_Round_CancelBet_Set";
            var GetRoundSpName = "Service_BettransLT_Eday_Round_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                // transId
                var voidDrawRoundId = $"{voidDrawData_Eday.roundCode}_{voidDrawData_Eday.player}";
                
                // 檢查是否有注單
                var bettrans = _mainDbClient.Query<BettransLTEdayDAL>(
                    GetRoundSpName, commandType: CommandType.StoredProcedure, new { roundCode = voidDrawData_Eday.roundCode, player = tpCustInfo.TpUserName });
                
                //沒注單無法取消
                if (!bettrans.Any())
                {
                    return -2;
                }
                
                //全取消就不用動了
                if (bettrans.All(i => i.TicketType == (int)BettransType.CancelBet))
                {
                    return -3;
                }
                
                //未結算不給動
                if (!bettrans.Any(i => i.SettleFlag == (int)SettleFlag.Settled))
                {
                    return -4;
                }
                
                var cancelData = new BettransLTEdayRoundCancelBet()
                {
                    roundCode = voidDrawData_Eday.roundCode,
                    player = tpCustInfo.TpUserName,
                    TicketType = voidDrawData_Eday.TicketType,
                    TicketStatus = voidDrawData_Eday.TicketStatus,
                    SettleFlag = (int)SettleFlag.EndGame
                };
                
                var p = new DynamicParameters(cancelData);
                result = _mainDbClient.Execute(CancelRoundSpName, p, conn, trans);
                
                if (result > 0)
                {
                    result = PayoutV2(
                        tpCustInfo,
                        //此function為把payout金額收回
                        voidDrawData_Eday.bet * -1,
                        (int)tpId,
                        voidDrawRoundId,
                        (int)tpGameType,
                        tpCategory,
                        voidDrawRoundId,
                        TransType.Refunds,
                        null,
                        out afterAmount,
                        "VoidDraw",
                        out bonusId,
                        conn,
                        trans,
                        AllowNegative.Allow
                    );
                }

                if (result > 0)
                {
                    //CancelBet 不送TpUnSettle 等Wager抓單回來一起處理即可
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
    }
}
