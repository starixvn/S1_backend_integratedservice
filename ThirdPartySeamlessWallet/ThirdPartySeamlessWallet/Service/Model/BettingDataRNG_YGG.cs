using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_YGG
    {
        public string playerid { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public string reference { get; set; }
        public string subreference { get; set; }
        public string description { get; set; }
        public string cat1 { get; set; }
        public string cat2 { get; set; }
        public string cat3 { get; set; }
        public string cat4 { get; set; }
        public string cat5 { get; set; }
        public string tag1 { get; set; }
        public string tag2 { get; set; }
        public string tag3 { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class PayoutDataRNG_YGG : BettingDataRNG_YGG
    {
        public decimal bonusprize { get; set; }
        public int tickets { get; set; }
    }

    public class SettleData_YGG 
    {
        /// <summary>
        /// 注單號碼
        /// </summary>
        public string TransId { get; set; }

        /// <summary>
        /// 投注額
        /// </summary>
        public decimal Stake { get; set; }

        /// <summary>
        /// 輸贏值，如果 NULL 表示 Wager 還沒拉到結算的注單
        /// </summary>
        public decimal? WinLostAmount { get; set; }
    }
}
