using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerPTService
    {
        private readonly IConfiguration configuration;

        public WagerPTService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public int InsertWager(BetPT bet)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionStringWager"));
            if (string.Equals(bet.GAMETYPE, "Live Games", StringComparison.OrdinalIgnoreCase))
            {
                // live casino
                return conn.Execute("Service_TpWagerLC_PT_Ins", bet, commandType: CommandType.StoredProcedure);
            }
            else
            {
                // slot
                return conn.Execute("Service_TpWagerRng_PT_Ins", bet, commandType: CommandType.StoredProcedure);
            }
        }
        
        public int InsertTpBetRecord14(BetRecordInfo betRecordInfo ,TpCustInfo tpCustInfo)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Archive_TpBetRecord14_Ins_V2", new
            {
                TpId = betRecordInfo.TpId,
                TransId = betRecordInfo.TransId,
                GameType = (int)betRecordInfo.GameType,
                CustId = tpCustInfo.CustId,
                UserName = tpCustInfo.UserName, 
                CurrencyId = tpCustInfo.CurrencyId,
                SiteId = tpCustInfo.SiteId,
                TpUserName = betRecordInfo.TpUserName,
                TransTime = betRecordInfo.TransTime, 
                SettledTime = betRecordInfo.SettledTime,
                Stake = betRecordInfo.Stake, 
                ValidBet = betRecordInfo.ValidBet, 
                Revenue = betRecordInfo.Revenue, 
                WinlostAmount = betRecordInfo.WinlostAmount,
                SettlementType = (int)betRecordInfo.SettlementType, 
                TicketStatus = (int)betRecordInfo.TicketStatus, 
                TicketKey = betRecordInfo.TicketKey,
                BonusId = betRecordInfo.BonusId,
                CreateTime= DateTime.Now,
                ProgressiveBet = betRecordInfo.ProgressiveBet,
                ProgressiveWin = betRecordInfo.ProgressiveWin,
                UpdateBonus = false
            }, commandType: CommandType.StoredProcedure);

        }
    }
}
