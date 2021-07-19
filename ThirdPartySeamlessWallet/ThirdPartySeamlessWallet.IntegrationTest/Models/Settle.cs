using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.IntegrationTest.Models
{
    public class TpUnsettle
    {
        public string TransId { get; set; }
        public int SettleFlag { get; set; }
    }

    public class Settle
    {
        public int SettleFlag { get; set; }
    }

    public class TpBetRecord
    {
        public int TpId { get; set; }
        public string TransId { get; set; }
        public string TpUserName { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinlostAmount { get; set; }
        public SettlementType SettlementType { get; set; }
        public TicketStatus TicketStatus { get; set; }
    }
}
