using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_SG
    {
        public string acctId { get; set; }
        public string transferId { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public TransferType type { get; set; }
        public string ticketId { get; set; }
        public string channel { get; set; }
        public string playerIp { get; set; }
        public string token { get; set; }
        public DateTime transferTime { get; set; }
        public string gameCode { get; set; }
        public string merchantCode { get; set; }
        public string serialNo { get; set; }
        public string referenceId { get; set; }
        public SpecialGame specialGame { get; set; }
        public IEnumerable<string> refTicketIds { get; set; }
        public int TicketStatus { get; set; }
    }
    public class SpecialGame
    {
        public string type { get; set; }
        public int count { get; set; }
        public int sequence { get; set; }
    }

    public class SettleData_SG
    {
        public decimal Stake { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public string referenceId { get; set; }
        public string ticketId { get; set; }
        public long SettleTime { get; set; }
    }
}
