using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{

    public class CommonDataPK_AMB
    {
        public string username { get; set; }
        public string agent { get; set; }
        public string game { get; set; }
        public string roundId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string refId { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class BettingDataPK_AMB : CommonDataPK_AMB
    {
        public string gameId { get; set; }
        public decimal outStanding { get; set; }
    }

    public class CancelDataPK_AMB : CommonDataPK_AMB
    {
        public string gameId { get; set; }
        public string product { get; set; }
    }

    public class PayoutDataPK_AMB : CommonDataPK_AMB
    {
        public string gameId { get; set; }
        public string product { get; set; }
        public decimal commission { get; set; }
        public decimal winlose { get; set; }
        public decimal betAmount { get; set; }
        public decimal outStanding { get; set; }
    }

    public class VoidDataPK_AMB : CommonDataPK_AMB
    {
        public string gameId { get; set; }
        public string product { get; set; }
    }

    public class BettransDataPK_AMB : CommonDataPK_AMB
    {
        public string TransId { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
    }
}
