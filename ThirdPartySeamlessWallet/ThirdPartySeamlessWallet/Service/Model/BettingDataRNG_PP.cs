using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_PP
    {
        public decimal amount { get; set; }
        public string gameId { get; set; }
        public string ipAddress { get; set; }
        public string language { get; set; }
        public string platform { get; set; }
        public string reference { get; set; }
        public string roundDetails { get; set; }
        public long roundId { get; set; }
        public long timestamp { get; set; }
        public string userId { get; set; }
        public string bonusCode { get; set; }
        public long jackpotId { get; set; }
        public decimal jackpotContribution { get; set; }
        public decimal promoWinAmount { get; set; }
        public string promoWinReference { get; set; }
        public long promoCampaignID { get; set; }
        public string promoCampaignType { get; set; }
        public int TicketStatus { get; set; }
        public BettransType type { get; set; }
    }
}
