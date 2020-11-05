using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_WM
    {
        public string user { get; set; }
        public decimal money { get; set; }
        public string dealid { get; set; }
        public string gtype { get; set; }
        public string type { get; set; }
        public string betdetail { get; set; }
        public string gameno { get; set; }
        public string code { get; set; }
        public string category { get; set; }
        public DateTime requestDate { get; set; }
        public string cmd { get; set; }
        public string signature { get; set; }
    }

    public class SettleData_WM
    {
        public string TransId { get; set; }
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
    }
}
