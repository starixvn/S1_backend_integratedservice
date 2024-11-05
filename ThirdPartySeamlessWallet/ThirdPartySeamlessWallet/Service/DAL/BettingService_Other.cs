using System.Data;

using Dapper;

using MySql.Data.MySqlClient;

using Newtonsoft.Json;

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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                result = _mainDbClient.Execute("Service_Bettrans_CF_Ins", p, conn, trans);

                if (result > 0 && bettingData_CF.stake != 0)
                {
                    result = _walletService.SetBalance(
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

        public int CancelBet_CF(TpCustInfo tpCustInfo, CancelBetData_CF cancelBetData_CF)
        {
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            int result = 0;
            var p = new DynamicParameters(new { ticket_id = cancelBetData_CF.ticket_id });
            p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("out_stake_money", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            result = _mainDbClient.Execute("Service_Bettrans_CF_CancelBet_Set", p, conn, trans);

            if (result > 0)
            {
                decimal payout = p.Get<decimal>("out_stake_money");
                result = _walletService.SetBalance(
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
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
            }
            else
            {
                _mainDbClient.RollbackAndDisposeConnection(conn, trans);
            }
            return result;

        }

        public BetRecordData_CF GetBetRecord_CF(CancelBetData_CF cancelBetData_CF)
        {
            return _mainDbClient.QueryFirst<BetRecordData_CF>("Service_Bettrans_CF_Get", new { ticket_id = cancelBetData_CF.ticket_id });
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_Ins", p, conn, trans);

                    if (result > 0)
                    {
                        result = _walletService.SetBalance(
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
                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                var p = new DynamicParameters(new { platformTxId = item.platformTxId });
                p.Add("out_stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);

                result = _mainDbClient.Execute("Service_BettransCF_AWC_CancelBet_Set", p, conn, trans);

                if (result > 0)
                {
                    decimal stake = p.Get<decimal>("out_stake");
                    result = _walletService.SetBalance(
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

            return result;
        }

        public int AdjustBetCF_AWC(TpCustInfo tpCustInfo, AdjustBetCF_AWC adjustBetCF_AWC, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;
            foreach (var item in adjustBetCF_AWC.txns)
            {
                using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_AdjustBet_Set", p, conn, trans);

                    if (result > 0)
                    {
                        decimal adjust = item.adjustAmount;
                        result = _walletService.SetBalance(
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_VoidBet_Set", p, conn, trans);
                    if (result > 0)
                    {
                        decimal stake = Math.Abs(item.betAmount);
                        result = _walletService.SetBalance(
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();
                try
                {
                    result = _mainDbClient.Execute("Service_BettransCF_AWC_UnvoidBet_Set", p, conn, trans);
                    if (result > 0)
                    {
                        decimal stake = p.Get<decimal>("out_stake");
                        result = _walletService.SetBalance(
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
                        _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    }
                    else
                    {
                        _mainDbClient.RollbackAndDisposeConnection(conn, trans);
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
            using var conn = _mainDbClient.GetMySqlConnection();
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
                    result = _mainDbClient.Execute("Service_BettransLT_One789_Ins", p, conn, trans);
                }
                else if (gameType == (int)GameType.One789_Lottery_789)
                {
                    mixUniqueId = $"{gameParseTmp[1]}_{gameParseTmp[2]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    result = _mainDbClient.Execute("Service_BettransLT_One789_789_Ins", p, conn, trans);
                }
                else
                {
                    mixUniqueId = $"{gameParseTmp[2]}_{gameParseTmp[3]}_{gameParseTmp[4]}_{bettingDataLT_One789.Username}";
                    p.Add("TransId", mixUniqueId);
                    category = TpCategory.LiveCasino;
                    result = _mainDbClient.Execute("Service_BettransLC_One789_Ins", p, conn, trans);
                }

                if (result > 0)
                {
                    result = PlaceBet(
                        tpCustInfo,
                        Math.Abs(bettingDataLT_One789.Amount) * -1,
                        (int)Tp.One789,
                        mixUniqueId,
                        gameType,
                        bettingDataLT_One789.Tx,
                        out afterAmount,
                        ref bonusId,
                        out _,
                        conn,
                        trans,
                        memo: $"{mixUniqueId}"
                    );

                    if (result > 0)
                    {
                        if (gameType != (int)GameType.One789_LiveCasino)
                        {
                            _ = _tpUnsettleService.InsertTpUnsettle(Tp.One789, (GameType)gameType, mixUniqueId, trans);
                        }

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
                var p = new DynamicParameters(new
                {
                    username,
                    ref_no,
                });

                return await _mainDbClient.QueryFirstAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"BettransLottery_Get_TCG Exception: username={username}, ref_no={ref_no}");
                return default;
            }
        }


        public async Task<TCGResult> Debit_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGTransactionData data)
        {
            try
            {
                var bonusId = 0;
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
                    TransType = TCGTransType.Debit.ToString(),
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                var bettransTCG = await _mainDbClient.QueryFirstAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p);
                if (bettransTCG != null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionAlreadyExists,
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransLottery_TCG_Ins", p, conn, trans);

                if (result > 0 && data.amount != 0)
                {
                    result = PlaceBet(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(data.amount) * -1,
                    tpId: (int)Tp.TCG,
                    tpTransId: data.ref_no,
                    gameType: (int)gameType,
                    refId: data.order_no,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    out _,
                    conn: conn,
                    trans: trans);
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
                _logger.LogError(ex, $"Debit_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        public async Task<TCGResult> Reserve_TCG(TpCustInfo tpCustInfo, GameType gameType, TCGRequest request, TCGTransactionData data)
        {
            try
            {
                var bonusId = 0;
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
                    TransType = TCGTransType.Reserve.ToString(),
                    TicketType = (int)BettransType.Reserve,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                var bettransTCG = await _mainDbClient.QueryFirstAsync<TCGBettransLottery>("Service_BettransLottery_TCG_Get", p);
                if (bettransTCG != null)
                {
                    return new TCGResult()
                    {
                        Result = TCGReturnCode.TransactionAlreadyExists,
                    };
                }

                var result = _mainDbClient.Execute("Service_BettransLottery_TCG_Ins", p, conn, trans);

                if (result > 0 && data.amount != 0)
                {
                    result = PlaceBet(
                    tpCustInfo: tpCustInfo,
                    amount: Math.Abs(data.amount) * -1,
                    tpId: (int)Tp.TCG,
                    tpTransId: data.ref_no,
                    gameType: (int)gameType,
                    refId: data.order_no,
                    afterAmount: out decimal afterAmount,
                    bonusId: ref bonusId,
                    useBonus: out _,
                    conn: conn,
                    trans: trans);
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
                _logger.LogError(ex, $"Reserve_TCG Exception: {JsonConvert.SerializeObject(request)} {JsonConvert.SerializeObject(data)}");
                throw;
            }
        }

        #endregion

        public async Task<SBOResult> TransferLiveCoin_SBO(TpCustInfo tpCustInfo, LiveCoinTransactionDto_SBO dto)
        {
            try
            {
                await using var conn = _mainDbClient.GetMySqlConnection();
                await conn.OpenAsync();
                await using var trans = await conn.BeginTransactionAsync();

                dto.TicketType = (int)BettransType.Tip;
                dto.TicketStatus = (int)TicketStatus.Win;
                var p = new DynamicParameters(dto);
                var sboResult = new SBOResult();
                var result = _mainDbClient.Execute("Service_BettransLiveCoin_SBO_Ins", p, conn, trans);

                if (result > 0 && dto.Amount != 0)
                {
                    result = _walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(dto.Amount) * -1,
                        (int)Tp.MP,
                        TransType.Wagers,
                        $"LiveCoin: {dto.Selection}",
                        dto.TransactionId,
                        out var afterAmount,
                        conn,
                        trans);

                    if (result > 0)
                    {
                        sboResult.AfterAmount = afterAmount;
                        await _mainDbClient.CommitAndDisposeConnectionAsync(conn, trans);
                    }
                    else
                    {
                        await _mainDbClient.RollbackAndDisposeConnectionAsync(conn, trans);
                    }
                }
                sboResult.Result = result;
                return sboResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new SBOResult()
                {
                    Result = -5
                };
            }
        }

        #region GPI

        public int PlaceBetLT_GPI(TpCustInfo tpCustInfo, int gameType, BettingDataRNG_GPI bettingDataRNG_GPI, out decimal afterAmount, out string Transid)
        {
            var result = 0;
            int bonusId = 0;
            afterAmount = 0;
            var category = TpCategory.Lottery;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            Transid = bettingDataRNG_GPI.trx_id;
            try
            {
                bettingDataRNG_GPI.amount *= -1;
                var p = new DynamicParameters(bettingDataRNG_GPI);
                p.Add("TicketType", (int)BettransType.Bet);
                p.Add("TicketStatus", (int)TicketStatus.UnSettled);
                result = _mainDbClient.Execute("Service_BettransLT_GPI_Ins", p, conn, trans);

                if (result > 0 && bettingDataRNG_GPI.amount != 0)
                {
                    result = PlaceBet(
                        tpCustInfo,
                        bettingDataRNG_GPI.amount,
                        (int)Tp.GPI,
                        bettingDataRNG_GPI.round_id,
                        gameType,
                        bettingDataRNG_GPI.round_id,
                        out afterAmount,
                        ref bonusId,
                        out decimal useBonus,
                        conn,
                        trans
                    );

                    if (bonusId > 0)
                    {
                        AddTpBonus((int)Tp.GPI, bettingDataRNG_GPI.trx_id, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, useBonus, bonusId, trans);
                    }
                }

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

        #region 93Connect

        public Bettrans93Connect CheckBetGames_93Connect(NTCCheckBetRequest request, string tpUserName)
        {
            var GetSpName = "Service_Bettrans_93Connect_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                var partitionTime = DateTime.Now;
                var bettrans = _partitionService.Query<Bettrans93Connect>(partitionTime, GetSpName,
                    new { bet_id = request.ref_no, username = tpUserName });

                return !bettrans.Any() ? null : bettrans.FirstOrDefault();
            }
            catch (MySqlException mySqlEx)
            {
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    return null;
                }
                throw;
            }
        }

        public int PlaceBetGames_93Connect(TpCustInfo tpCustInfo, NTCPlaceBetRequest bettingDataGames_NTConnect,
            out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.NTConnect;
            var tpGameType = GameType.NTConnect_Games;
            var tpCategory = TpCategory.Games;
            var InsSpName = "Service_Bettrans_93Connect_Ins";
            var CheckSpName = "Service_Bettrans_93Connect_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                _ = DateTime.TryParse(bettingDataGames_NTConnect.betTime, out var partitionTime);
                // 檢查是否重複
                var bettrans = _partitionService.Query<Bettrans93Connect>(partitionTime, CheckSpName,
                    new { bet_id = bettingDataGames_NTConnect.bet_id, username = tpCustInfo.TpUserName });
                if (bettrans.Any(i => i.TicketType == (int)BettransType.Bet && i.bet_id == bettingDataGames_NTConnect.bet_id))
                {
                    return -1;
                }


                bettingDataGames_NTConnect.username = tpCustInfo.TpUserName;
                bettingDataGames_NTConnect.stake *= -1;
                var p = new DynamicParameters(bettingDataGames_NTConnect);
                p.Add("status", "R");
                p.Add("SettleFlag", (int)SettleFlag.Running);
                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                if (result > 0 && bettingDataGames_NTConnect.stake != 0)
                {
                    result = PlaceBet(
                        tpCustInfo,
                        bettingDataGames_NTConnect.stake,
                        (int)tpId,
                        bettingDataGames_NTConnect.bet_id,
                        (int)tpGameType,
                        bettingDataGames_NTConnect.bet_id,
                        out afterAmount,
                        ref bonusId,
                        out _,
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
        
        #region CQ9_WWC
        
        public int RolloutWCC_CQ9(TpCustInfo tpCustInfo, BettingDataRNG_CQ9 bettingDataRNG_CQ9, int gameType, out decimal afterAmount)
        {
            var tpId = Tp.CQ9;
            //var tpGameType = GameType.CQ9_WCC;
            var tpCategory = TpCategory.Cockfighting;
            
            int bonusId = 0;
            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            bettingDataRNG_CQ9.amount = Math.Abs(bettingDataRNG_CQ9.amount) * -1;
            try
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
                result = _mainDbClient.Execute("Service_BettransCF_CQ9_Ins", p, conn, trans);

                if (result > 0)
                {
                    if (bettingDataRNG_CQ9.amount != 0)
                    {
                        result = PlaceBet(
                            tpCustInfo,
                            bettingDataRNG_CQ9.amount,
                            (int)tpId,
                            bettingDataRNG_CQ9.roundid,
                            gameType,
                            bettingDataRNG_CQ9.roundid,
                            out afterAmount,
                            ref bonusId,
                            out _,
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
        
        #endregion
        
        #region EdayLT
        
        public int BetRequest_Eday(TpCustInfo tpCustInfo, EdayBetRequestData bettingDataLT_Eday, out decimal afterAmount)
        {
            int bonusId = 0;
            afterAmount = 0;
            var result = 0;

            var tpId = Tp.Eday;
            var tpGameType = GameType.Eday_Lottery;
            var tpCategory = TpCategory.Lottery;
            var InsSpName = "Service_BettransLT_Eday_Ins";
            var CheckSpName = "Service_BettransLT_Eday_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            
            try
            {
                // 檢查是否重複
                var bettrans = _partitionService.Query<BettransLTEday>(bettingDataLT_Eday.UTCBetTime, CheckSpName,
                    new { billId = bettingDataLT_Eday.billId, player = tpCustInfo.TpUserName });
                if (bettrans.Any(i => i.TicketType == (int)BettransType.Bet && i.billId == bettingDataLT_Eday.billId))
                {
                    return -1;
                }
                
                bettingDataLT_Eday.player = tpCustInfo.TpUserName;
                bettingDataLT_Eday.amount *= -1;
                var p = new DynamicParameters(bettingDataLT_Eday);
                p.Add("ticketCount", 0);
                p.Add("eventId", 0);
                p.Add("betTotal", 0.00m);
                p.Add("winTotal", 0.00m);
                p.Add("UTCSettleTime", bettingDataLT_Eday.UTCBetTime);
                p.Add("SettleFlag", (int)SettleFlag.Running);
                result = _partitionService.Execute(bettingDataLT_Eday.UTCBetTime, InsSpName, p, trans);
                if (result > 0 && bettingDataLT_Eday.amount != 0)
                {
                    result = PlaceBet(
                        tpCustInfo,
                        bettingDataLT_Eday.amount,
                        (int)tpId,
                        bettingDataLT_Eday.billId.ToString(),
                        (int)tpGameType,
                        bettingDataLT_Eday.billId.ToString(),
                        out afterAmount,
                        ref bonusId,
                        out _,
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