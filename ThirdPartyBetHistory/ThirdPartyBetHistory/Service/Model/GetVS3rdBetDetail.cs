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
        public long trans_id { get; set; }
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
                return decimal.Parse(BetchoiceList.First().Odds);
            }
            set => odds = value;
        }
    
        public decimal? discount_stake { get; set; } 
        public int? odds_type { get; set; }
        public string tokenmoney { get; set; } = string.Empty;

        public List<Betchoice> BetchoiceList
        {
            get 
            {
                var result = new List<Betchoice>();
                foreach (var item in betchoice.Split("|"))
                {
                    result.Add(new Betchoice(item));
                }
                return result;
            }
        }   
    }

    public class Betchoice
    {
        public Betchoice(string betchoice)
        {
            // "m=belgium - turkey;s=fulltime result;b=home;o=1.75;"
            // "m=england - spain;s=fulltime result;b=home;o=3.52;ht=0-1;ft=1-3; mo = lose;| m = portugal - germany; s = home to score; b = no; o = 3.69; ht = 1 - 1; ft = 1 - 2; mo = lose; ",
            foreach (var item in betchoice.Split(";"))
            {
                if (item.StartsWith("m="))
                    Title = item.Replace("m=", "");
                if (item.StartsWith("s="))
                    BetType = item.Replace("s=", "");
                if (item.StartsWith("b="))
                    BetChoice = item.Replace("b=", "");
                if (item.StartsWith("o="))
                    Odds = item.Replace("o=", "");
            }
        }
        /// <summary>
        /// 球赛队伍
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 投注类型
        /// </summary>
        public string BetType { get; set; }
        /// <summary>
        /// 投注选项
        /// </summary>
        public string BetChoice { get; set; }
        /// <summary>
        /// 赔率
        /// </summary>
        public string Odds { get; set; }
    }

}
