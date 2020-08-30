using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class WagerCFModel
    {
        public WagerCF get_cockfight_processed_ticket { get; set; }
    }

    public class WagerCF
    {
        public string status_code { get; set; }
        public string status_text { get; set; }
        public int total_records { get; set; }
        public dynamic data { get; set; }
    }

    public class BetCF
    {
        public long ticket_id { get; set; }
        public string login_id { get; set; }
        public string arena_code { get; set; }
        public string arena_name_cn { get; set; }
        public string match_no { get; set; }
        public string match_type { get; set; }
        public int fight_no { get; set; }
        public string meron_cock { get; set; }
        public string meron_cock_cn { get; set; }
        public string wala_cock { get; set; }
        public string wala_cock_cn { get; set; }
        public string bet_on { get; set; }
        public string odds_type { get; set; }
        public decimal odds_asked { get; set; }
        public decimal odds_given { get; set; }
        public decimal stake { get; set; }
        public decimal stake_money { get; set; }
        public decimal balance_open { get; set; }
        public decimal balance_close { get; set; }
        public string fight_result { get; set; }
        public string status { get; set; }
        public decimal winloss { get; set; }
        public decimal comm_earned { get; set; }
        public decimal payout { get; set; }
        public decimal balance_open1 { get; set; }
        public decimal balance_close1 { get; set; }
        public DateTime created_datetime { get; set; }
        public DateTime processed_datetime { get; set; }
        public DateTime fight_datetime { get; set; }
        public DateTime match_date { get; set; }
        public long createUnix
        {
            get
            {
                return new DateTimeOffset(created_datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long processUnix
        {
            get
            {
                return new DateTimeOffset(processed_datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long fightUnix
        {
            get
            {
                return new DateTimeOffset(fight_datetime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long matchUnix
        {
            get
            {
                return new DateTimeOffset(match_date, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }

        public class WagerCFTotal
        {
            public int wagerCount { get; set; }
        }
    }
}
