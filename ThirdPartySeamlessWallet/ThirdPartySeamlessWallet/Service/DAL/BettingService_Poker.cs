using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Globalization;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public partial class BettingService
    {
        #region OneGaming

        public int MoneyOutPK_OneGaming(TpCustInfo tpCustInfo, BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var amount = bettingDataPK_OneGaming.amount * -1;
            var ticketType = (int)BettransType.Bet;

            var p = new DynamicParameters(new
            {
                amount = amount,
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

                if (p.Get<int>("result") == 1)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.OneGaming,
                        TransType.Wagers,
                        $"Transfer Out: {Math.Abs(bettingDataPK_OneGaming.amount):#,##0.##}",
                        bettingDataPK_OneGaming.ref_tx_id ?? bettingDataPK_OneGaming.transaction_id,
                        out afterAmount,
                        conn,
                        trans
                    );
                }

                trans.Commit();
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

        #region AMB

        public int PlaceBetPK_AMB(TpCustInfo tpCustInfo, BettingDataPK_AMB bettingDataPK_AMB, out decimal afterAmount)
        {
            afterAmount = 0;
            var result = 0;

            var amount = bettingDataPK_AMB.amount * -1;
            
            var p = new DynamicParameters(new
            {
                TransId = $"{bettingDataPK_AMB.roundId}_{bettingDataPK_AMB.username}",
                username = bettingDataPK_AMB.username,
                agent = bettingDataPK_AMB.agent,
                game = bettingDataPK_AMB.game,
                gameId = bettingDataPK_AMB.gameId,
                roundId = bettingDataPK_AMB.roundId,
                amount = amount,
                currency = bettingDataPK_AMB.currency,
                refId = bettingDataPK_AMB.refId,
                timeUnix = new DateTimeOffset(DateTime.Parse(bettingDataPK_AMB.timestamp.ToString()), new TimeSpan(0, 0, 0)).ToUnixTimeSeconds(),
                outStanding = bettingDataPK_AMB.outStanding,
                TicketType = (int)BettransType.Bet,
                TicketStatus = (int)TicketStatus.UnSettled,
                SettleFlag = (int)SettleFlag.Running
            });

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            try
            {
                result = conn.Execute("Service_BettransPK_AMB_Bet_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

                if (result > 0)
                {
                    result = walletService.SetBalance(
                        tpCustInfo.CustId,
                        tpCustInfo.SiteId,
                        tpCustInfo.UserName,
                        amount,
                        (int)Tp.AMB,
                        TransType.Wagers,
                        $"Stake: {bettingDataPK_AMB.amount:#,##0.##}",
                        bettingDataPK_AMB.refId,
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
            catch (Exception ex)
            {
                throw;
            }

            return result;
        }

        #endregion
    }
}