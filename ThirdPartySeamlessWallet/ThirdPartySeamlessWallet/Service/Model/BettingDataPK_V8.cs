using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataPK_V8
    {
        public string account { get; set; }
        public decimal money { get; set; }
        public long timestamp { get; set; }
    }

    public class SettleDataPK_V8
    {
        public string GameID { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal Revenue { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }

}
