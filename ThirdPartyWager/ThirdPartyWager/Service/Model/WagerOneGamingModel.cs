using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerOneGamingModel
    {
        public string status_code { get; set; }
        public string error_code { get; set; }
        public string message { get; set; }
        public string currency { get; set; }
        public IList<BetOneGaming> data { get; set; }
        public bool has_rollback { get; set; }
        public IList<RollOneGaming> rollback { get; set; }
        public DateTime datetime { get; set; }
    }

    public class BetOneGaming
    {
        public string ticket { get; set; }
        public string game { get; set; }
        public string player { get; set; }
        public string table { get; set; }
        public string period { get; set; }
        public string card { get; set; }
        public string type { get; set; }
        public decimal bet { get; set; }
        public decimal realbet { get; set; }
        public decimal comm { get; set; }
        public string status { get; set; }
        public decimal amount { get; set; }
        public DateTime datetime { get; set; }
        public long dateUnix
        {
            get
            {
                return new DateTimeOffset(datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class RollOneGaming
    {
    }

    //public class WagerOneGamingTotal
    //{
    //    public int wagerCount { get; set; }
    //    public decimal bet { get; set; }
    //    public decimal realbet { get; set; }
    //    public decimal comm { get; set; }
    //    public decimal amount { get; set; }
    //}
}
