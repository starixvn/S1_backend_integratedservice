using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyBetHistory.Service.Model.enums;

namespace ThirdPartyBetHistory.Service.Model
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
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public string OriginalState { get; set; }
        public long TicketKey { get; set; }
        public int BonusId { get; set; }
    }
    public class BonusFulfill
    {
        public int BonusId { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
    }
}
