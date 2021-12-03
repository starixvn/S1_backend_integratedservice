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

    public class SettleDataPK_V8 : SettleData
    {
        public string GameID { get; set; }
        public int KindID { get; set; }
    }

}
