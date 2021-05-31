using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BetRecordInfo
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public GameType GameType { get; set; }
        public string TpUserName { get; set; }
        public long SettledTime { get; set; }
        public long TransTime { get; set; }
        public decimal Stake { get; set; }
        public decimal? ValidBet { get; set; }
        public decimal? Revenue { get; set; }
        public decimal WinlostAmount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public string OriginalState { get; set; }
        public int TicketKey { get; set; }
        public int BonusId { get; set; }
    }

    public class SettleData
    {
        public string TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public decimal? Revenue { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }
}
