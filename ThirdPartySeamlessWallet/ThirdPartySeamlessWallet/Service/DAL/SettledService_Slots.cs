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
        public int SettledRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            int gameType = (int)GameType.SG_RNG;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_SG.TicketStatus = bettingDataRNG_SG.amount > 0 ? (int)TicketStatus.Win : (int)TicketStatus.Lost;
            var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, acctId = bettingDataRNG_SG.acctId, currency = bettingDataRNG_SG.currency, amount = bettingDataRNG_SG.amount, type = bettingDataRNG_SG.type, channel = bettingDataRNG_SG.channel, playerIp = bettingDataRNG_SG.playerIp, transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(), gameCode = bettingDataRNG_SG.gameCode, ticketId = bettingDataRNG_SG.ticketId, referenceId = bettingDataRNG_SG.referenceId, specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame), refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds), TicketStatus = bettingDataRNG_SG.TicketStatus });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);

            try
            {
                result = conn.Execute("Service_BettransRng_SG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);
                int bonusId = 0;
                if (result > 0)
                {
                    if (bettingDataRNG_SG.amount > 0)
                    {
                        //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, bettingDataRNG_SG.amount, (int)Tp.SG, TransType.Settled, $"{bettingDataRNG_SG.type}: {Math.Abs(bettingDataRNG_SG.amount).ToString("#,##0.##")}", bettingDataRNG_SG.transferId, out afterAmount, conn, trans);
                        result = PayoutV2(tpCustInfo, bettingDataRNG_SG.amount, (int)Tp.SG, bettingDataRNG_SG.referenceId, gameType, TpCategory.Slots, bettingDataRNG_SG.transferId, TransType.Settled, null, out afterAmount, $"{bettingDataRNG_SG.type}", out bonusId, conn, trans);
                    }
                    else
                    {
                        GetTpBonus((int)Tp.SG, bettingDataRNG_SG.referenceId, gameType, TpCategory.Slots, out int custId, out bonusId);
                    }

                    if (result > 0)
                    {
                        decimal Stake = p.Get<decimal>("Stake");
                        decimal WinLostAmount = p.Get<decimal>("WinLostAmount");
                        BetRecordInfo betRecordInfo = new BetRecordInfo
                        {
                            TpId = (int)Tp.SG,
                            Stake = Stake,
                            WinlostAmount = WinLostAmount,
                            TransId = bettingDataRNG_SG.referenceId,
                            GameType = GameType.SG_RNG,
                            TpUserName = bettingDataRNG_SG.acctId,
                            SettledTime = 0,
                            SettlementType = SettlementType.SettledForValid,
                            BonusId = bonusId
                        };

                        result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                        if (result > 0)
                        {
                            trans.Commit();
                        }
                    }
                }
                else
                {
                    result = -1;
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
        public int CancelBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            int gameType = (int)GameType.SG_RNG;
            afterAmount = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            var p = new DynamicParameters(new { bettingDataRNG_SG.transferId });
            p.Add("CancelStake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_CancelBetRng_SG_Set", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                decimal cancelStake = p.Get<decimal>("CancelStake");
                int bonusId = 0;
                if (cancelStake != bettingDataRNG_SG.amount)
                {
                    throw new Exception($"SG Cancel amount {bettingDataRNG_SG.amount.ToString("#,##0.##")} it not equal {cancelStake.ToString("#,##0.##")}");
                }

                if (cancelStake != 0)
                {
                    //result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, cancelStake, (int)Tp.SG, TransType.Refunds, $"Refund: {cancelStake.ToString("#,##0.##")}", bettingDataRNG_SG.referenceId, out afterAmount, conn, trans);
                    result = PayoutV2(tpCustInfo, bettingDataRNG_SG.amount, (int)Tp.SG, bettingDataRNG_SG.referenceId, gameType, TpCategory.Slots, bettingDataRNG_SG.referenceId, TransType.Refunds, null, out afterAmount, $"Refund", out bonusId, conn, trans);
                }
                else
                {
                    GetTpBonus((int)Tp.SG, bettingDataRNG_SG.referenceId, gameType, TpCategory.Slots, out int custId, out bonusId);
                }

                if (result > 0)
                {
                    BetRecordInfo betRecordInfo = new BetRecordInfo
                    {
                        TpId = (int)Tp.SG,
                        Stake = cancelStake,
                        WinlostAmount = 0,
                        TransId = bettingDataRNG_SG.referenceId,
                        GameType = GameType.SG_RNG,
                        TpUserName = bettingDataRNG_SG.acctId,
                        SettledTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(),
                        SettlementType = SettlementType.SettledForValid,
                        TicketStatus = TicketStatus.Refund,
                        BonusId = bonusId
                    };

                    result = SettleTicket(tpCustInfo, betRecordInfo, 0, out _, conn, trans, false/*Don't need Payout*/);

                    if (result > 0)
                    {
                        trans.Commit();
                    }
                }
            }
            else
            {
                return -1;
            }

            return result;
        }

        private int PayoutV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, TpCategory tpCategory, string refId, TransType transType, string betData, out decimal afterAmount, string memoHead,out int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            bonusId = 0;
            int result = GetTpBonus(tpId, tpTransId, gameType, TpCategory.Slots, out int custId, out bonusId);

            if (bonusId > 0 || transType == TransType.Prize)
            {
                if (bonusId > 0 && custId != tpCustInfo.CustId)
                {
                    return 0;
                }

                result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out decimal afterBonus, ref bonusId, conn, trans);

                if ((transType == TransType.Prize && bonusId == 0) || afterBonus < 0 || bonusId == -1)
                {
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, afterBonus, tpId, transType, $"{memoHead}: {Math.Abs(afterBonus).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                }
                else
                {
                    afterAmount = afterBonus + walletService.GetBalance(tpCustInfo.CustId);
                }
            }
            else
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, transType, $"{memoHead}: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
            }

            return result;
        }
        private int GetTpBonus(int tpId, string tpTransId, int gameType, TpCategory tpCategory, out int CustId, out int BonusId)
        {
            CustId = 0;
            BonusId = 0;

            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { TpId = tpId, TransId = tpTransId, GameType = gameType });
            p.Add("CustId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            p.Add("BonusId", dbType: DbType.Int32, direction: ParameterDirection.Output);
            var result = conn.Execute($"Service_Tp{tpCategory}Bonus_Get", p, commandType: CommandType.StoredProcedure);

            if (result > 0)
            {
                CustId = p.Get<int>("CustId");
                BonusId = p.Get<int>("BonusId");
            }

            return result;
        }
    }
}
