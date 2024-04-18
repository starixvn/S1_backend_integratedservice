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
        #region SA
        public int PlaceBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SA_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;

            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_SA_Ins_V3",
                    new
                    {
                        Txnid = bettingDataLC_SA.txnid,
                        TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}",
                        Currency = bettingDataLC_SA.currency,
                        Username = bettingDataLC_SA.username,
                        Gametype = bettingDataLC_SA.gametype,
                        Amount = bettingDataLC_SA.amount,
                        bettingDataLC_SA.BetAmount,
                        bettingDataLC_SA.Betdetails,
                        Gameid = bettingDataLC_SA.gameid
                    }, conn, trans);

                if (result > 0)
                {
                    result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_SA.amount) * -1, (int)Tp.SA, $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", gameType, $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);

                    if (bonusId > 0)
                    {
                        AddTpBonus((int)Tp.SA, bettingDataLC_SA.txnid, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, bettingDataLC_SA.amount, bonusId, TpCategory.LiveCasino, conn, trans);
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
                if (mySqlEx.Number == 1062)
                {
                    /*Duplicate for key 'PRIMARY'*/
                    result = -1;
                }
            }
            return result;
        }
        #endregion

        #region AG
        public int PlaceBetLC_AG(TpCustInfo tpCustInfo, BettingDataLC_AG bettingDataLC_AG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.AG_LiveCasino;
            afterAmount = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            var p = new DynamicParameters(new { transactionID = bettingDataLC_AG.TransactionID, currency = bettingDataLC_AG.Currency, value = bettingDataLC_AG.Value, playname = bettingDataLC_AG.Playname, betTime = tools.ConvertLocTime(bettingDataLC_AG.BetTime, -4), round = bettingDataLC_AG.Round, gametype = bettingDataLC_AG.Gametype, gameCode = bettingDataLC_AG.GameCode, tableCode = bettingDataLC_AG.TableCode, transactionCode = bettingDataLC_AG.TransactionCode, playtype = bettingDataLC_AG.Playtype });
            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_AGV2_Bet_Ins", p, conn, trans);

                if (result > 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_AG.Value) * -1, (int)Tp.AG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_AG.Value).ToString("#,##0.##")}", bettingDataLC_AG.TransactionID, out afterAmount, conn, trans);
                    result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_AG.Value) * -1, (int)Tp.AG, bettingDataLC_AG.TransactionID, gameType, bettingDataLC_AG.TransactionID, null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);

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

        #region DG
        public int PlaceBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.DG_LiveCasino;

            afterAmount = 0;
            TransferType transferType = TransferType.PlaceBet;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            var result = 0;
            var p = new DynamicParameters(new { data = bettingDataLC_DG.data, TransId = bettingDataLC_DG.TransId, Username = bettingDataLC_DG.Username, Amount = bettingDataLC_DG.Amount, Memo = (string)null, TicketType = (int)transferType });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = _mainDbClient.Execute("Service_BettransLC_DGV2_Ins", p, conn, trans);

            if (result > 0)
            {
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_DG.Amount) * -1, (int)Tp.DG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_DG.Amount).ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_DG.Amount) * -1, (int)Tp.DG, bettingDataLC_DG.TransId, gameType, bettingDataLC_DG.TransId, null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);
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
        #endregion

        #region Sexy
        public int PlaceBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_Bet bettingDataLC_Sexy_Bet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            foreach (var txn in bettingDataLC_Sexy_Bet.Txns)
            {
                DateTime.TryParse(txn.BetTime, out DateTime bBet);
                DateTime.TryParse(txn.UpdateTime, out DateTime uT);
                DateTimeOffset betTime = new DateTimeOffset(bBet, new TimeSpan(8, 0, 0));
                DateTimeOffset updateTime = new DateTimeOffset(uT, new TimeSpan(8, 0, 0));

                var p = new DynamicParameters(new { txId = txn.TxId, userId = bettingDataLC_Sexy_Bet.UserId, IP = bettingDataLC_Sexy_Bet.Ip, currency = bettingDataLC_Sexy_Bet.Currency, roundId = bettingDataLC_Sexy_Bet.RoundId, gameType = bettingDataLC_Sexy_Bet.RoundId, betAmount = txn.BetAmount, betTime = betTime.ToUnixTimeSeconds(), updateTime = updateTime.ToUnixTimeSeconds(), category = txn.Category });
                result = _mainDbClient.Execute("Service_BettransLC_Sexy_Ins", p, conn, trans);
                if (result > 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, txn.BetAmount * -1, (int)Tp.Sexy, TransType.Wagers, $"Stake: {Math.Abs(txn.BetAmount).ToString("#,##0.##")}", txn.TxId, out afterAmount, conn, trans);
                    result = PlaceBetV2(tpCustInfo, Math.Abs(txn.BetAmount) * -1, (int)Tp.Sexy, txn.TxId, gameType, txn.TxId, null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);

                    if (result == 0)
                    {
                        return 0;
                    }
                }
                else
                {
                    return 0;
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

            return result;
        }
        #endregion

        #region Sexy (AWC)

        public int PlaceBetLC_AWC_Sexy(TpCustInfo tpCustInfo, BetLC_Sexy betLC_Sexy, out decimal afterAmount)
        {
            int bonusId = 0;
            var result = 0;
            afterAmount = 0;

            var cntTxn = betLC_Sexy.txns.Count;
            var cntExet = 0;

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            foreach (var txn in betLC_Sexy.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = txn.platformTxId,
                    userId = txn.userId,
                    currency = txn.currency,
                    platform = txn.platform,
                    gameType = txn.gameType,
                    gameCode = txn.gameCode,
                    gameName = txn.gameName,
                    betType = txn.betType,
                    betAmount = txn.betAmount,
                    betUnix = new DateTimeOffset(txn.betTime).ToUnixTimeSeconds(),
                    roundId = txn.roundId,
                    gameInfo = JsonConvert.SerializeObject(txn.gameInfo),
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Bet,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                try
                {
                    result = 0;
                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_Ins", p, conn, trans);
                    if (result > 0)
                    {
                        result = PlaceBetV2(
                            tpCustInfo,
                            Math.Abs(txn.betAmount) * -1,
                            (int)Tp.Sexy,
                            txn.platformTxId,
                            (int)GameType.Sexy_LiveCasino,
                            txn.platformTxId,
                            null,
                            TpCategory.LiveCasino,
                            out afterAmount,
                            ref bonusId,
                            conn,
                            trans
                        );
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
                    /*Duplicate for key 'PRIMARY'*/
                    if (mySqlEx.Number == 1062)
                    {
                        result = -1;
                        cntExet++;
                        continue;
                    }
                    else
                    {
                        throw mySqlEx;
                    }
                }
            }
            if (cntExet == cntTxn)
            {
                _mainDbClient.CommitAndDisposeConnection(conn, trans);
                return result;
            }
            return 0;
        }

        public int TipLC_AWC_Sexy(TpCustInfo tpCustInfo, TipLC_Sexy tipLC_Sexy, out decimal afterAmount)
        {
            var result = 0;
            afterAmount = 0;

            foreach (var txn in tipLC_Sexy.txns)
            {
                var p = new DynamicParameters(new
                {
                    platformTxId = txn.platformTxId,
                    userId = txn.userId,
                    currency = txn.currency,
                    platform = txn.platform,
                    gameType = txn.gameType,
                    gameCode = txn.gameCode,
                    gameName = txn.gameName,
                    betType = "",
                    roundId = "",
                    tip = txn.tip,
                    txUnix = txn.txUnix,
                    tipInfo = JsonConvert.SerializeObject(txn.tipInfo),
                    SiteId = tpCustInfo.SiteId,
                    CustId = tpCustInfo.CustId,
                    CurrencyId = tpCustInfo.CurrencyId,
                    TicketType = (int)BettransType.Tip,
                    TicketStatus = (int)TicketStatus.UnSettled,
                    SettleFlag = (int)SettleFlag.Running
                });

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                try
                {
                    result = _mainDbClient.Execute("Service_BettransLC_AWC_Sexy_Tip_Ins", p, conn, trans);

                    if (result > 0)
                    {
                        result = walletService.SetBalance(
                            tpCustInfo.CustId,
                            tpCustInfo.SiteId,
                            tpCustInfo.UserName,
                            Math.Abs(txn.tip) * -1,
                            (int)Tp.Sexy,
                            TransType.Tip,
                            $"Tip: {Math.Abs(txn.tip):#,##0.##}",
                            txn.platformTxId,
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
                        throw;
                    }
                }
            }
            return result;
        }

        #endregion

        #region EBet
        public int PlaceBetLC_EBet(TpCustInfo tpCustInfo, BettingData_EBet bettingData_EBet, out decimal afterAmount)
        {

            int bonusId = 0;
            int gameType = (int)GameType.EBet_LiveCasino;
            afterAmount = 0;
            string mixUniqueId = $"{bettingData_EBet.detail.roundCode}_{bettingData_EBet.username}";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            try
            {
                result = _mainDbClient.Execute("Service_BettransLC_EBet_Bet_Ins_V2",
                    new
                    {
                        TransId = mixUniqueId,
                        username = bettingData_EBet.username,
                        channelId = bettingData_EBet.channelId,
                        money = bettingData_EBet.money,
                        type = bettingData_EBet.type,
                        platform = bettingData_EBet.platform,
                        currency = bettingData_EBet.currency,
                        seqNo = bettingData_EBet.seqNo,
                        Event = bettingData_EBet.Event,
                        timestamp = bettingData_EBet.timestamp,
                        tableCode = bettingData_EBet.detail.tableCode,
                        roundCode = bettingData_EBet.detail.roundCode,
                        betTime = bettingData_EBet.detail.betTime,
                        totalBet = bettingData_EBet.detail.totalBet,
                        validBet = bettingData_EBet.detail.validBet,
                        detail = JsonConvert.SerializeObject(bettingData_EBet.detail),
                        betList = JsonConvert.SerializeObject(bettingData_EBet.detail.betList),
                        TicketStatus = (int)TicketStatus.UnSettled,
                        TicketType = (bettingData_EBet.type == 1) ? (int)BettransType.Bet : (int)BettransType.Reserve
                    },
                    conn, trans
                );

                if (result > 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingData_EBet.money) * -1,
                        (int)Tp.EBet,
                        mixUniqueId,
                        gameType,
                        mixUniqueId,
                        null,
                        TpCategory.LiveCasino,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

                    if (bonusId > 0 && result > 0)
                    {
                        AddTpBonus(
                            (int)Tp.EBet,
                            bettingData_EBet.seqNo,
                            gameType,
                            tpCustInfo.CustId,
                            tpCustInfo.UserName,
                            tpCustInfo.CurrencyId,
                            tpCustInfo.SiteId,
                            Math.Abs(bettingData_EBet.money),
                            bonusId,
                            TpCategory.LiveCasino,
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
        #endregion

        #region WM

        public int PlaceBetLC_WM(TpCustInfo tpCustInfo, BettingDataLC_WM bettingDataLC_WM, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.WM_LiveCasino;
            afterAmount = 0;
            string mixUniqueId = $"{bettingDataLC_WM.gameno}_{bettingDataLC_WM.user}";

            var p = new DynamicParameters(new
            {
                TransId = mixUniqueId,
                user = bettingDataLC_WM.user,
                money = bettingDataLC_WM.money,
                dealid = bettingDataLC_WM.dealid,
                gtype = bettingDataLC_WM.gtype,
                type = bettingDataLC_WM.type,
                betdetail = bettingDataLC_WM.betdetail,
                gameno = bettingDataLC_WM.gameno,
                code = bettingDataLC_WM.code,
                category = bettingDataLC_WM.category,
                requestUnix = new DateTimeOffset(bettingDataLC_WM.requestDate, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                TicketType = bettingDataLC_WM.category == "1" ? (int)BettransType.Bet : (int)BettransType.Tip,
                SettleFlag = 2,
                bettingDataLC_WM.RealBetAmount,
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = _mainDbClient.Execute("Service_BettransLC_WM_Ins_V2", p, conn, trans);

            if (result > 0)
            {
                // 1:下注单
                if (bettingDataLC_WM.category == "1")
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(bettingDataLC_WM.money) * -1,
                        (int)Tp.WM,
                        mixUniqueId,
                        gameType,
                        mixUniqueId,
                        null,
                        TpCategory.LiveCasino,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );

                    if (bonusId > 0 && result > 0)
                    {
                        AddTpBonus(
                            (int)Tp.WM,
                            bettingDataLC_WM.dealid,
                            gameType,
                            tpCustInfo.CustId,
                            tpCustInfo.UserName,
                            tpCustInfo.CurrencyId,
                            tpCustInfo.SiteId,
                            Math.Abs(bettingDataLC_WM.money),
                            bonusId,
                            TpCategory.LiveCasino,
                            conn,
                            trans
                        );
                    }
                }
                else
                {
                    // 2:小费单
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        Math.Abs(bettingDataLC_WM.money) * -1,
                        (int)Tp.WM,
                        TransType.Tip,
                        $"Tip: {Math.Abs(bettingDataLC_WM.money):#,##0.##}",
                        bettingDataLC_WM.dealid,
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

            return result;
        }

        #endregion

        #region N2Live

        public int PlaceBetLC_N2Live(TpCustInfo tpCustInfo, BettingDataLC_N2Live bettingDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;
            afterAmount = 0;
            string TransId = $"{bettingDataLC_N2Live.request.gameid}_{bettingDataLC_N2Live.request.userid}";
            var p = new DynamicParameters(new
            {
                TransId = TransId,
                tradeid = bettingDataLC_N2Live.request.tradeid,
                userid = bettingDataLC_N2Live.request.userid,
                vendorid = bettingDataLC_N2Live.request.vendorid,
                gameid = bettingDataLC_N2Live.request.gameid,
                gamecode = bettingDataLC_N2Live.request.gamecode,
                Stake = bettingDataLC_N2Live.request.totalamount,
                StakeTrades = JsonConvert.SerializeObject(bettingDataLC_N2Live.request.trades),
                //totalamount = bettingDataLC_N2Live.request.totalamount,
                //trades = JsonConvert.SerializeObject(bettingDataLC_N2Live.request.trades),
                timeUnix = new DateTimeOffset(DateTime.Parse(bettingDataLC_N2Live.request.timestamp), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 0
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = _mainDbClient.Execute("Service_BettransLC_N2Live_Ins", p, conn, trans);

            if (result > 0)
            {

                result = PlaceBetV2(
                    tpCustInfo,
                    Math.Abs(bettingDataLC_N2Live.request.totalamount) * -1,
                    (int)Tp.N2Live,
                    bettingDataLC_N2Live.request.tradeid,
                    gameType,
                    bettingDataLC_N2Live.request.tradeid,
                    null,
                    TpCategory.LiveCasino,
                    out afterAmount,
                    ref bonusId,
                    conn,
                    trans
                );

                if (bonusId > 0 && result > 0)
                {
                    // for group by round settle worker
                    AddTpBonus(
                        (int)Tp.N2Live,
                        TransId,
                        gameType,
                        tpCustInfo.CustId,
                        tpCustInfo.UserName,
                        tpCustInfo.CurrencyId,
                        tpCustInfo.SiteId,
                        Math.Abs(bettingDataLC_N2Live.request.totalamount),
                        bonusId,
                        TpCategory.LiveCasino,
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

        public int SmartPlaceBetLC_N2Live(TpCustInfo tpCustInfo, SmartBettingDataLC_N2Live smartBettingDataLC_N2Live, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.N2Live_LiveCasino;
            afterAmount = 0;

            var p = new DynamicParameters(new
            {
                schemeid = smartBettingDataLC_N2Live.request.schemeid,
                userid = smartBettingDataLC_N2Live.request.userid,
                vendorid = smartBettingDataLC_N2Live.request.vendorid,
                Stake = smartBettingDataLC_N2Live.request.amount,
                startUnix = smartBettingDataLC_N2Live.request.startUnix,
                endUnix = smartBettingDataLC_N2Live.request.endUnix,
                timeUnix = smartBettingDataLC_N2Live.request.timeUnix,
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = 0
            });

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = _mainDbClient.Execute("Service_SmartBettransLC_N2Live_Ins", p, conn, trans);

            if (result > 0)
            {

                result = PlaceBetV2(
                    tpCustInfo,
                    Math.Abs(smartBettingDataLC_N2Live.request.amount) * -1,
                    (int)Tp.N2Live,
                    smartBettingDataLC_N2Live.request.schemeid,
                    gameType,
                    smartBettingDataLC_N2Live.request.schemeid,
                    null,
                    TpCategory.LiveCasino,
                    out afterAmount,
                    ref bonusId,
                    conn,
                    trans
                );

                //if (bonusId > 0 && result > 0)
                //{
                //    AddTpBonus(
                //        (int)Tp.N2Live,
                //        smartBettingDataLC_N2Live.request.schemeid,
                //        gameType,
                //        tpCustInfo.CustId,
                //        tpCustInfo.UserName,
                //        tpCustInfo.CurrencyId,
                //        tpCustInfo.SiteId,
                //        Math.Abs(smartBettingDataLC_N2Live.request.amount),
                //        bonusId,
                //        TpCategory.LiveCasino,
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
            return result;
        }

        #endregion

        #region MG

        public MGPlaceBetResult PlaceBetLC_MG(TpCustInfo tpCustInfo, BettingDataRNG_MG bettingDataRNG_MG, int gameType)
        {
            var bonusId = 0;
            var afterAmount = 0M;
            var result = 0;
            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeMilliseconds(bettingDataRNG_MG.creationTimeMs).DateTime;

                // 檢查該局是否已經結算
                var checkBetId = _partitionService.Query<BettingDataRNG_MG>(partitionTime, "Service_BettransLC_MG_Check", new { bettingDataRNG_MG.betId });
                if (checkBetId.Any())
                {
                    logger.LogError($"{bettingDataRNG_MG.betId} is completed");
                    return new MGPlaceBetResult()
                    {
                        Result = 1,
                        AfterAmount = afterAmount
                    };
                }

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                using var trans = conn.BeginTransaction();

                bettingDataRNG_MG.amount *= -1;
                bettingDataRNG_MG.TransType = "D";
                bettingDataRNG_MG.TicketType = (int)BettransType.Bet;
                bettingDataRNG_MG.TicketStatus = (int)TicketStatus.UnSettled;
                bettingDataRNG_MG.SettleFlag = (int)SettleFlag.Running;

                var p = new DynamicParameters(bettingDataRNG_MG);
                p.Add("extTxnId", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, "Service_BettransLC_MG_Ins_V2", p, trans);
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
                        TpCategory.LiveCasino,
                        out afterAmount,
                        ref bonusId,
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
                    logger.LogError(mySqlEx, $"PlaceBetLC_MG failed: {JsonConvert.SerializeObject(bettingDataRNG_MG)}");
                    throw;
                }
            }
        }

        #endregion MG

        #region EVO

        public int PlaceBetLC_EVO(TpCustInfo tpCustInfo, BettingData_EVO bettingData_EVO, out decimal afterAmount)
        {
            int bonusId = 0;

            afterAmount = 0;
            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;

            try
            {
                var check = _mainDbClient.Query<BettransData_EVO>("Service_BettransLC_EVO_Get", commandType: CommandType.StoredProcedure, new { TransactionRefId = bettingData_EVO.transaction.refId });
                if (check.Where(x => x.TicketType == (int)BettransType.Bet).Any())
                {
                    // alreay has this bet data
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return -1;
                }
                else if (check.Where(x => x.TicketType == (int)BettransType.CancelBet && x.TransactionId == bettingData_EVO.transaction.id).Any())
                {
                    // cancelled
                    _mainDbClient.CommitAndDisposeConnection(conn, trans);
                    return -2;
                }

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

                var category = TpCategory.LiveCasino;
                var gameType = (int)GameType.EVO_LiveCasino;
                result = _mainDbClient.Execute("Service_BettransLC_EVO_Ins_V2", p, conn, trans);

                if (result > 0 && bettingData_EVO.transaction.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingData_EVO.transaction.amount,
                        (int)Tp.EVO,
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

        #region Yeebet

        public IEnumerable<BettingDataLC_Yeebet> IsExist_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data)
        {
            var query = new { notifyid = data.notifyid, type = data.type, serialnumber = data.serialnumber };
            return _partitionService.Query<BettingDataLC_Yeebet>(partitionTime, "Service_BettransLC_Yeebet_Check", query);
        }

        public bool PlaceBet_Yeebet(DateTime partitionTime, BettingDataLC_Yeebet data, MySqlTransaction trans, out int SQ)
        {
            SQ = 0;
            var insertSQ = _partitionService.Execute(partitionTime, "Service_BettransLC_Yeebet_Ins", data, trans);

            if (insertSQ == 0)
                return false;

            SQ = insertSQ;
            return true;
        }

        public bool SetBalance_Yeebet(TpCustInfo tpCustInfo, BettingDataLC_Yeebet data, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PlaceBetV2(tpCustInfo,
                                data.amount,
                                (int)Tp.Yeebet,
                                data.serialnumber,  //TpTransId
                                (int)GameType.Yeebet_LiveCasino,
                                data.serialnumber,  //RefId
                                null,
                                TpCategory.LiveCasino,
                                out afterAmount,
                                ref bonusId,
                                conn,
                                trans,
                                memo: $" - {memo}_{data.notifyid} ");

            if (result <= 0)
                return false;

            return true;
        }

        #endregion

        #region BG

        public IEnumerable<BettingDataLC_BG> IsExist_BG(DateTime partitionTime, string random, IEnumerable<long> orderIds)
        {
            var query = new { random = random, orderIds = JsonConvert.SerializeObject(orderIds) };
            return _partitionService.Query<BettingDataLC_BG>(partitionTime, "Service_BettransLC_BG_Check", query);
        }

        public bool PlaceBet_BG(DateTime partitionTime, BettingDataLC_BG data, MySqlTransaction trans)
        {
            var result = _partitionService.Execute(partitionTime, "Service_BettransLC_BG_Ins", data, trans);

            if (result == 0)
                return false;

            return true;
        }

        public bool SetBalance_BG(TpCustInfo tpCustInfo, BettingDataLC_BG data, string memo,
            out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            var result = PlaceBetV2(tpCustInfo,
                                data.orderAmount,
                                (int)Tp.BG,
                                data.orderId,  //TpTransId
                                (int)GameType.BG_LiveCasino,
                                data.orderId,  //RefId
                                null,
                                TpCategory.LiveCasino,
                                out afterAmount,
                                ref bonusId,
                                conn,
                                trans,
                                memo: $" - {memo}_{data.random}_{data.orderId} ");

            if (result <= 0)
                return false;

            return true;
        }

        #endregion
        
        #region WonCasino
        
        public int DeductLC_WonCasino(TpCustInfo tpCustInfo, WonCasinoDeductRequest bettingDataLC_WonCasino,
            out decimal afterAmount,out string orderNo)
        {
            int bonusId = 0;
            afterAmount = 0;
            orderNo = "99999999";
            var result = 0;

            var tpId = Tp.WonCasino;
            var tpGameType = GameType.WonCasino_LiveCasino;
            var tpCategory = TpCategory.LiveCasino;
            var InsSpName = "Service_Bettrans_WonCasino_Ins";
            var CheckSpName = "Service_Bettrans_WonCasino_Get";

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var partitionTime = DateTimeOffset.FromUnixTimeSeconds(bettingDataLC_WonCasino.bets.createtime).DateTime;
                var bettrans = _partitionService.Query<BettransWonCasino>(partitionTime, CheckSpName, 
                    new { id = bettingDataLC_WonCasino.bets.id , username = tpCustInfo.TpUserName });
                
                //若TicketStatus已經為 Refund = 3,/*Reject, Refund, Void, Draw*/,就不給再次扣款了。
                if (bettrans.Any(i => i.TicketStatus == (int)TicketStatus.Refund))
                {
                    orderNo = bettrans.LastOrDefault().SQ.ToString();
                    return -2;
                }
                
                bettingDataLC_WonCasino.username = tpCustInfo.TpUserName;
                bettingDataLC_WonCasino.notifyid = bettingDataLC_WonCasino.notifyid + 
                                                   bettingDataLC_WonCasino.type +
                                                   bettingDataLC_WonCasino.bets.id;
                var p = new DynamicParameters(new
                {
                    id = bettingDataLC_WonCasino.bets.id,
                    username = bettingDataLC_WonCasino.username,
                    notifyid = bettingDataLC_WonCasino.notifyid,
                    amount = bettingDataLC_WonCasino.amount,
                    type = bettingDataLC_WonCasino.type,
                    currency = bettingDataLC_WonCasino.currency,
                    appid = bettingDataLC_WonCasino.appid,
                    sign = bettingDataLC_WonCasino.sign,
                    gameid = bettingDataLC_WonCasino.bets.gameid,
                    userid = bettingDataLC_WonCasino.bets.userid,
                    userstatus = bettingDataLC_WonCasino.bets.userstatus,
                    betpoint = bettingDataLC_WonCasino.bets.betpoint,
                    betodds = bettingDataLC_WonCasino.bets.betodds,
                    betamount = bettingDataLC_WonCasino.bets.betamount,
                    commamount = bettingDataLC_WonCasino.bets.commamount,
                    winlost = bettingDataLC_WonCasino.bets.winlost,
                    foregift = bettingDataLC_WonCasino.bets.foregift,
                    gameroundid = bettingDataLC_WonCasino.bets.gameroundid,
                    gameresult = bettingDataLC_WonCasino.bets.gameresult,
                    gametype = bettingDataLC_WonCasino.bets.gametype,
                    gamestyle = bettingDataLC_WonCasino.bets.gamestyle,
                    serialnumber = bettingDataLC_WonCasino.serialnumber,
                    gameroundno = bettingDataLC_WonCasino.bets.gameroundno,
                    gameno = bettingDataLC_WonCasino.bets.gameno,
                    bettype = bettingDataLC_WonCasino.bets.bettype,
                    cid = bettingDataLC_WonCasino.bets.cid,
                    state = bettingDataLC_WonCasino.bets.state,
                    createtime =  bettingDataLC_WonCasino.bets.createtime,
                    settletime = bettingDataLC_WonCasino.bets.settletime,
                    UTCBetTime = DateTimeOffset.FromUnixTimeSeconds(bettingDataLC_WonCasino.bets.createtime).UtcDateTime,
                    UTCSettleTime = DateTimeOffset.FromUnixTimeSeconds(bettingDataLC_WonCasino.bets.settletime).UtcDateTime,
                    TicketType = bettingDataLC_WonCasino.TicketType,
                    TicketStatus = bettingDataLC_WonCasino.TicketStatus,
                    SettleFlag = bettingDataLC_WonCasino.SettleFlag
                });
                p.Add("SQ", dbType: DbType.Int64, direction: ParameterDirection.Output);
                result = _partitionService.Execute(partitionTime, InsSpName, p, trans);
                if (result > 0 && bettingDataLC_WonCasino.amount != 0)
                {
                    result = PlaceBetV2(
                        tpCustInfo,
                        bettingDataLC_WonCasino.amount,
                        (int)tpId,
                        bettingDataLC_WonCasino.serialnumber,
                        (int)tpGameType,
                        bettingDataLC_WonCasino.serialnumber,
                        null,
                        tpCategory,
                        out afterAmount,
                        ref bonusId,
                        conn,
                        trans
                    );
                }

                if (result > 0)
                {
                    orderNo = p.Get<long>("SQ").ToString();
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
