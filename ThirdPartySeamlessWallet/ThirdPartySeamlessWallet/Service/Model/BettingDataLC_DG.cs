using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_DG
    {
        public string data { get; set; }
        public string TransId { get; set; }
        public string Username { get; set; }
        public decimal Amount { get; set; }

        public TicketStatus TicketStatus { get; set; }
    }

    public class SettleData_DG : SettleData
    {
        public new long TransId { get; set; }
        public int GameType { get; set; }
        public new int GameId { get; set; }
    }
}
