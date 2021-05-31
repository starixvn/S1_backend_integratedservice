using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using Microsoft.Extensions.Configuration;
using ThirdPartySeamlessWallet.Service.Model.enums;
using Newtonsoft.Json;

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
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_SAV2_Ins", new { Txnid = bettingDataLC_SA.txnid, TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", Currency = bettingDataLC_SA.currency, Username = bettingDataLC_SA.username, Gametype = bettingDataLC_SA.gametype, Amount = bettingDataLC_SA.amount, Betdetails = bettingDataLC_SA.Betdetails, Gameid = bettingDataLC_SA.gameid }, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                ////BetRecordInfo betRecordInfo = new BetRecordInfo();
                ////betRecordInfo.TpId = (int)Tp.SA;
                ////betRecordInfo.Stake = bettingDataLC_SA.amount;
                ////betRecordInfo.TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}";
                ////betRecordInfo.GameType = GameType.SA_LiveCasino;
                ////betRecordInfo.TpUserName = bettingDataLC_SA.username;

                ////result = PlaceBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_SA.amount) * -1, (int)Tp.SA, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_SA.amount).ToString("#,##0.##")}", $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", out afterAmount, conn, trans);
                result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_SA.amount) * -1, (int)Tp.SA, $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", gameType, $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);

                if (bonusId > 0)
                {
                    AddTpBonus((int)Tp.SA, bettingDataLC_SA.txnid, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, bettingDataLC_SA.amount, bonusId, TpCategory.LiveCasino, conn, trans);
                }

                if (result > 0)
                {
                    trans.Commit();
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

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { transactionID = bettingDataLC_AG.TransactionID, currency = bettingDataLC_AG.Currency, value = bettingDataLC_AG.Value, playname = bettingDataLC_AG.Playname, betTime = tools.ConvertLocTime(bettingDataLC_AG.BetTime, -4), round = bettingDataLC_AG.Round, gametype = bettingDataLC_AG.Gametype, gameCode = bettingDataLC_AG.GameCode, tableCode = bettingDataLC_AG.TableCode, transactionCode = bettingDataLC_AG.TransactionCode, playtype = bettingDataLC_AG.Playtype });
            try
            {
                result = conn.Execute("Service_BettransLC_AGV2_Bet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_AG.Value) * -1, (int)Tp.AG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_AG.Value).ToString("#,##0.##")}", bettingDataLC_AG.TransactionID, out afterAmount, conn, trans);
                    result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_AG.Value) * -1, (int)Tp.AG, bettingDataLC_AG.TransactionID, gameType, bettingDataLC_AG.TransactionID, null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);

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
        #endregion

        #region DG
        public int PlaceBetLC_DG(TpCustInfo tpCustInfo, BettingDataLC_DG bettingDataLC_DG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.DG_LiveCasino;

            afterAmount = 0;
            TransferType transferType = TransferType.PlaceBet;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { data = bettingDataLC_DG.data, TransId = bettingDataLC_DG.TransId, Username = bettingDataLC_DG.Username, Amount = bettingDataLC_DG.Amount, Memo = (string)null, TicketType = (int)transferType });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransLC_DGV2_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_DG.Amount) * -1, (int)Tp.DG, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_DG.Amount).ToString("#,##0.##")}", bettingDataLC_DG.TransId, out afterAmount, conn, trans);
                result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_DG.Amount) * -1, (int)Tp.DG, bettingDataLC_DG.TransId, gameType, bettingDataLC_DG.TransId, null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }
        #endregion
        #region Allbet
        public int PlaceBetLC_Allbet(TpCustInfo tpCustInfo, BettingDataLC_Allbet bettingDataLC_Allbet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Allbet_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { tranId = bettingDataLC_Allbet.TranId, transferType = bettingDataLC_Allbet.TransferType, client = bettingDataLC_Allbet.Client, currency = bettingDataLC_Allbet.Currency, amount = bettingDataLC_Allbet.Amount, details = JsonConvert.SerializeObject(bettingDataLC_Allbet.Details) });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            try
            {
                result = conn.Execute("Service_BettransLC_Allbet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0 && bettingDataLC_Allbet.Amount != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataLC_Allbet.Amount * -1, (int)Tp.Allbet, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_Allbet.Amount).ToString("#,##0.##")}", bettingDataLC_Allbet.TranId.ToString(), out afterAmount, conn, trans);

                    //AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans);
                    result = PlaceBetV2(tpCustInfo, Math.Abs(bettingDataLC_Allbet.Amount) * -1, (int)Tp.Allbet, $"T{bettingDataLC_Allbet.TranId}", gameType, bettingDataLC_Allbet.TranId.ToString(), null, TpCategory.LiveCasino, out afterAmount, ref bonusId, conn, trans);
                    if (bonusId > 0)
                    {
                        foreach (var bet in bettingDataLC_Allbet.Details)
                        {
                            AddTpBonus((int)Tp.Allbet, bet.BetNum.ToString(), gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, bet.Amount, bonusId, TpCategory.LiveCasino, conn, trans);
                        }
                    }
                }

                if (result > 0)
                {
                    trans.Commit();
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
        #region Sexy
        public int PlaceBetLC_Sexy(TpCustInfo tpCustInfo, BettingDataLC_Sexy_Bet bettingDataLC_Sexy_Bet, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.Sexy_LiveCasino;

            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
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
                result = conn.Execute("Service_BettransLC_Sexy_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
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
                trans.Commit();
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

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_EBet_Bet_Ins", new
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
                betList = JsonConvert.SerializeObject(bettingData_EBet.detail.betList)
            },
            transaction: trans,
            commandType: CommandType.StoredProcedure
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
                    trans.Commit();
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
                SettleFlag = 2
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_WM_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

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
                    trans.Commit();
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

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_BettransLC_N2Live_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {

                result = PlaceBetV2(
                    tpCustInfo,
                    Math.Abs(bettingDataLC_N2Live.request.totalamount) * -1,
                    (int)Tp.N2Live,
                    TransId,
                    gameType,
                    bettingDataLC_N2Live.request.tradeid,
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
                //        TransId,
                //        gameType,
                //        tpCustInfo.CustId,
                //        tpCustInfo.UserName,
                //        tpCustInfo.CurrencyId,
                //        tpCustInfo.SiteId,
                //        Math.Abs(bettingDataLC_N2Live.request.totalamount),
                //        bonusId,
                //        TpCategory.LiveCasino,
                //        conn,
                //        trans
                //    );
                //}

                if (result > 0)
                {
                    trans.Commit();
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

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            result = conn.Execute("Service_SmartBettransLC_N2Live_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

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
                    trans.Commit();
                }
            }

            return result;
        }

        #endregion
    }
}
