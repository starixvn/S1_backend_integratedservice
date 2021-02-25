using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataFish_YL
    {
        public string userId { get; set; }
        public string gameNumber { get; set; }
        public int gameId { get; set; }
        public int chair { get; set; }
        public string roomId { get; set; }
        public int roomLevel { get; set; }
        public string txId { get; set; }
        public string currency { get; set; }
        public string transTime { get; set; }
        public decimal betAmount { get; set; }
        public decimal payAmount { get; set; }
        public decimal commission { get; set; }
        public decimal profit { get; set; }
        public decimal validBet { get; set; }
        public TicketStatus TicketStatus { get; set; }
    }

    public class RefundDataFish_YL
    {
        public string userId { get; set; }
        public string gameId { get; set; }
        public string txId { get; set; }
        public string gameNumier { get; set; }
        public string updateTime { get; set; }
    }

    public class SettleDataFish_YL
    {
        public string TransId { get; set; }
        public string Account { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public long SettleTime { get; set; }
        public int TicketStatus { get; set; }
    }

}
