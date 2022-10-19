using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.DAL
{
    public class WagerPTService
    {
        private readonly MySqlClient _wagerDbClient;
        private readonly MySqlClient _mainDbClient;

        public WagerPTService(IServiceProvider provider)
        {
            _wagerDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Wager);
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }

        public int InsertWager(BetPT bet)
        {
            if (string.Equals(bet.GAMETYPE, "Live Games", StringComparison.OrdinalIgnoreCase))
            {
                // live casino
                return _wagerDbClient.Execute("Service_TpWagerLC_PT_Ins", bet);
            }
            else
            {
                // slot
                return _wagerDbClient.Execute("Service_TpWagerRng_PT_Ins", bet);
            }
        }
        
        public int InsertTpBetRecord14(BetRecordInfo betRecordInfo ,TpCustInfo tpCustInfo)
        {
            return _mainDbClient.Execute("Archive_TpBetRecord_Ins", new
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
                OriginalState= betRecordInfo.OriginalState,
                SettlementType = (int)betRecordInfo.SettlementType, 
                TicketStatus = (int)betRecordInfo.TicketStatus, 
                TicketKey = betRecordInfo.TicketKey,
                BonusId = betRecordInfo.BonusId,
                CreateTime= DateTime.Now,
                ProgressiveBet = betRecordInfo.ProgressiveBet,
                ProgressiveWin = betRecordInfo.ProgressiveWin,
                UpdateBonus = false
            });

        }
    }
}
