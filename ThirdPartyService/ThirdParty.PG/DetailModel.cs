using System;
using System.Collections.Generic;
using System.Text;

namespace ThirdParty.PG
{
    public class DetailModel
    {
        public long betId { get; set; }
        public int gameId { get; set; }
        public long betTime { get; set; }
        public int betType { get; set; }
        public string currency { get; set; }
        public int platform { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public long betEndTime { get; set; }
        public string playerName { get; set; }
        public long rowVersion { get; set; }
        public int handsStatus { get; set; }
        public long parentBetId { get; set; }
        public decimal balanceAfter { get; set; }
        public decimal balanceBefore { get; set; }
        public int transactionType { get; set; }
        public decimal jackpotWinAmount { get; set; }
        public decimal jackpotContributionAmount { get; set; }
        public decimal jackpotRtpContributionAmount { get; set; }
    }
}
