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

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region Set EffectiveBet of Cust
        public int PlaceBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
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
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_SA.amount) * -1, (int)Tp.SA, TransType.Wagers, $"Stake: {Math.Abs(bettingDataLC_SA.amount).ToString("#,##0.##")}", $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", out afterAmount, conn, trans);

                if (result > 0)
                {
                    trans.Commit();
                }
            }

            return result;
        }
        public int CancelBetLC_SA(TpCustInfo tpCustInfo, BettingDataLC_SA bettingDataLC_SA, out decimal afterAmount)
        {
            afterAmount = 0;
            int result = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();

            var refBettransLC = conn.Execute("Service_CancelBetLC_SA_Set", new { Txnid = bettingDataLC_SA.txn_reverse_id }, transaction: trans, commandType: CommandType.StoredProcedure);

            /*BetRecordInfo betRecordInfo = new BetRecordInfo();
            betRecordInfo.TpId = (int)Tp.SA;
            betRecordInfo.Stake = bettingDataLC_SA.amount;
            betRecordInfo.TransId = $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}";
            betRecordInfo.GameType = GameType.SA_LiveCasino;
            betRecordInfo.TpUserName = bettingDataLC_SA.username;

            result = CancelBetByCall(tpCustInfo, betRecordInfo, out afterAmount, conn, trans);*/
            if (refBettransLC > 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, Math.Abs(bettingDataLC_SA.amount), (int)Tp.SA, TransType.Refunds, $"Cancel Bet: {Math.Abs(bettingDataLC_SA.amount).ToString("#,##0.##")}", $"{bettingDataLC_SA.gameid}_{bettingDataLC_SA.username}", out afterAmount, conn, trans);

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
        #endregion
    }
}
