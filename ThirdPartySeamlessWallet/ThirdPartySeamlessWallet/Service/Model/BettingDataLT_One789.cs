using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLT_One789
    {
        public string Username { get; set; }
        public string Tx { get; set; }
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public long CreatedAt { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string RefSource { get; set; }
    }

    public class SettleLT_One789
    {
        public string TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }

    public class SettleData_One789 : SettleData
    {
        public int Status { get; set; }
    }
}
