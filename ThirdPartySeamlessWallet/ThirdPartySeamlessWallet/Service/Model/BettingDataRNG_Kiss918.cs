using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_Kiss918
    {
    }

    public class SettleData_Kiss918
    {
        public string uuid { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }
}
