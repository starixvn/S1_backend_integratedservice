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
    public partial class BettingService
    {
        public int PlaceBetRNG_SG(TpCustInfo tpCustInfo, BettingDataRNG_SG bettingDataRNG_SG, out decimal afterAmount)
        {
            int bonusId = 0;
            int gameType = (int)GameType.SG_RNG;

            afterAmount = 0;
            decimal afterBonus = 0;
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using var trans = conn.BeginTransaction();
            var result = 0;
            bettingDataRNG_SG.amount *= -1;
            bettingDataRNG_SG.TicketStatus = (int)TicketStatus.UnSettled;
            var p = new DynamicParameters(new { transferId = bettingDataRNG_SG.transferId, acctId = bettingDataRNG_SG.acctId, currency = bettingDataRNG_SG.currency, amount = bettingDataRNG_SG.amount, type = bettingDataRNG_SG.type, channel = bettingDataRNG_SG.channel, playerIp = bettingDataRNG_SG.playerIp, transferTime = new DateTimeOffset(bettingDataRNG_SG.transferTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds(), gameCode = bettingDataRNG_SG.gameCode, ticketId = bettingDataRNG_SG.ticketId, referenceId = bettingDataRNG_SG.referenceId, specialGame = JsonConvert.SerializeObject(bettingDataRNG_SG.specialGame), refTicketIds = JsonConvert.SerializeObject(bettingDataRNG_SG.refTicketIds), TicketStatus = bettingDataRNG_SG.TicketStatus });
            p.Add("Stake", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            p.Add("WinLostAmount", dbType: DbType.Decimal, direction: ParameterDirection.Output);
            result = conn.Execute("Service_BettransRng_SG_Ins", p, transaction: trans, commandType: CommandType.StoredProcedure);

            if (result > 0 && bettingDataRNG_SG.amount != 0)
            {
                //gameType
                result = PlaceBetV2(tpCustInfo, bettingDataRNG_SG.amount, (int)Tp.SG, gameType, bettingDataRNG_SG.transferId, null, out afterAmount, ref bonusId, conn, trans);
            }

            if (result > 0)
            {
                trans.Commit();
            }

            return result;
        }

        private int AddTpBonus(int tpId, string transId, int gameType, int custId, string userName, int currencyId, int siteId, decimal stake, int bonusId, TpCategory tpCategory, MySqlConnection conn, MySqlTransaction trans)
        {
            int result = conn.Execute($"Service_Tp{tpCategory}Bonus_Ins", new { TpId = tpId, TransId = transId, GameType = gameType, CustId = custId, UserName = userName, CurrencyId = currencyId, SiteId = siteId, Stake = stake, BonusId = bonusId }, transaction: trans, commandType: CommandType.StoredProcedure);

            return result;
        }
        private int PlaceBetV2(TpCustInfo tpCustInfo, decimal amount, int tpId, string tpTransId, int gameType, string refId, string betData, TpCategory tpCategory, out decimal afterAmount, ref int bonusId, MySqlConnection conn, MySqlTransaction trans)
        {
            afterAmount = 0;
            int result = walletService.SetBonus(gameType, tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out decimal afterBonus, ref bonusId, conn, trans);
            if (bonusId > 0)
            {
                AddTpBonus(tpId, tpTransId, gameType, tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.CurrencyId, tpCustInfo.SiteId, amount, bonusId, tpCategory, conn, trans);
                if (afterBonus < 0)
                {
                    amount = afterBonus;
                    result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
                }
            }
            else if (bonusId == 0)
            {
                result = walletService.SetBalance(tpCustInfo.CustId, tpCustInfo.SiteId, tpCustInfo.UserName, amount, tpId, TransType.Wagers, $"Stake: {Math.Abs(amount).ToString("#,##0.##")}", refId, out afterAmount, conn, trans);
            }

            return result;
        }
    }
}
