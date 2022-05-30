using System;
using System.Collections.Generic;
using System.Linq;
using ThirdPartyBetHistory.Service.Model.Interfaces;

namespace ThirdPartyBetHistory.Service.Model
{
    public class GetVS3rdBetDetail
    {
        public int error_code { get; set; }
        public string message { get; set; }
        public Data Data { get; set; }
    }

    public class Data
    {
        public long last_version_key { get; set; }
        public IEnumerable<Vs3rdbetdetails> VS3rdBetDetails { get; set; }
    }

    public class Vs3rdbetdetails : ISabaBet
    {
        public int trans_id { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime transaction_time { get; set; }
        public decimal stake { get; set; }
        public decimal bet_amount { get; set; }
        public string ticket_status { get; set; }
        public decimal winlost_amount { get; set; }
        public int? bet_type { get; set; }
        public int currency { get; set; }
        public long version_key { get; set; }
        public string betchoice { get; set; }
        public string combo_type { get; set; }

        public decimal odds
        {
            get
            {
                //"m=belgium - turkey;s=fulltime result;b=home;o=1.75;"
                var o = betchoice.Split(";").First(i => i.StartsWith("o")).Replace("o=", "");
                return decimal.Parse(o);
            }
            set => odds = value;
        }
        public decimal? discount_stake { get; set; } 
        public int? odds_type { get; set; }
        public string tokenmoney { get; set; } = string.Empty;
    }

}
