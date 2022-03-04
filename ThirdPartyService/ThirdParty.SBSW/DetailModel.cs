using System;

namespace ThirdParty.SBSW
{
    public class DetailModel
    {
        public BetDetail[] BetDetails { get; set; }
        public BetNumberDetail[] BetNumberDetails { get; set; }
        public string last_version_key { get; set; }
        public BetVirtualSportDetail[] BetVirtualSportDetails { get; set; }
    }

    public class BetDetail
    {
        public string os { get; set; }
        public string hdp { get; set; }
        public string odds { get; set; }
        public string range { get; set; }
        public decimal stake { get; set; }
        public string islive { get; set; }
        public string ou_hdp { get; set; }
        public string away_id { get; set; }
        public string bet_tag { get; set; }
        public string betfrom { get; set; }
        public string browser { get; set; }
        public string game_no { get; set; }
        public string home_id { get; set; }
        public string isLucky { get; set; }
        public string team_id { get; set; }
        public string away_hdp { get; set; }
        public string bet_team { get; set; }
        public string bet_type { get; set; }
        public string currency { get; set; }
        public string home_hdp { get; set; }
        public string match_id { get; set; }
        public string ref_code { get; set; }
        public string trans_id { get; set; }
        public string ba_status { get; set; }
        public string betchoice { get; set; }
        public string exculding { get; set; }
        public string league_id { get; set; }
        public string odds_Info { get; set; }
        public string odds_type { get; set; }
        public string pool_type { get; set; }
        public Parlaydata[] ParlayData { get; set; }
        public string away_score { get; set; }
        public string bonus_code { get; set; }
        public string combo_type { get; set; }
        public string home_score { get; set; }
        public Leaguename[] leaguename { get; set; }
        public double percentage { get; set; }
        public string sport_type { get; set; }
        public string tokenmoney { get; set; }
        public Cashoutdata[] CashOutData { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string operator_id { get; set; }
        public string parlay_type { get; set; }
        public string total_score { get; set; }
        public string version_key { get; set; }
        public string wallet_type { get; set; }
        public decimal after_amount { get; set; }
        public Awayteamname[] awayteamname { get; set; }
        public Hometeamname[] hometeamname { get; set; }
        public string parlay_ref_no { get; set; }
        public string ticket_status { get; set; }
        public decimal buyback_amount { get; set; }
        public decimal discount_stake { get; set; }
        public string esports_gameid { get; set; }
        public DateTime? match_datetime { get; set; }
        public decimal original_stake { get; set; }
        public decimal validbetamount { get; set; }
        public decimal winlost_amount { get; set; }
        public Colossusbetdata[] ColossusBetData { get; set; }
        public string lottery_bettype { get; set; }
        public DateTime? settlement_time { get; set; }
        public Singleparlaydata[] SingleParlayData { get; set; }
        public Resettlementinfo[] resettlementinfo { get; set; }
        public DateTime? transaction_time { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

    public class Parlaydata
    {
        public string hdp { get; set; }
        public string odds { get; set; }
        public string islive { get; set; }
        public string away_id { get; set; }
        public string bet_tag { get; set; }
        public string home_id { get; set; }
        public string away_hdp { get; set; }
        public string bet_team { get; set; }
        public string bet_type { get; set; }
        public string home_hdp { get; set; }
        public string match_id { get; set; }
        public string league_id { get; set; }
        public string parlay_id { get; set; }
        public string away_score { get; set; }
        public string home_score { get; set; }
        public string sport_type { get; set; }
        public string ticket_status { get; set; }
        public DateTime? match_datetime { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

    public class Leaguename
    {
        public string lang { get; set; }
        public string name { get; set; }
    }

    public class Cashoutdata
    {
        public decimal stake { get; set; }
        public int bet_type { get; set; }
        public DateTime? transdate { get; set; }
        public string cashout_id { get; set; }
        public decimal real_stake { get; set; }
        public string ticket_status { get; set; }
        public decimal buyback_amount { get; set; }
        public decimal winlost_amount { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

    public class Awayteamname
    {
        public string lang { get; set; }
        public string name { get; set; }
    }

    public class Hometeamname
    {
        public string lang { get; set; }
        public string name { get; set; }
    }

    public class Colossusbetdata
    {
        public string line { get; set; }
        public string sold { get; set; }
        public string cb_id { get; set; }
        public decimal stake { get; set; }
        public string status { get; set; }
        public string remains { get; set; }
        public string bet_type { get; set; }
        public string betcheck { get; set; }
        public DateTime? transdate { get; set; }
        public decimal winlost_amount { get; set; }
    }

    public class Singleparlaydata
    {
        public string status { get; set; }
        public string selection_name { get; set; }
        public string selection_name_cs { get; set; }
    }

    public class Resettlementinfo
    {
        public string winlost { get; set; }
        public DateTime? actionDate { get; set; }
        public string balancechange { get; set; }
        public string ticket_status { get; set; }
    }

    public class BetNumberDetail
    {
        public string os { get; set; }
        public string odds { get; set; }
        public decimal stake { get; set; }
        public string islive { get; set; }
        public string betfrom { get; set; }
        public string browser { get; set; }
        public string bet_team { get; set; }
        public string bet_type { get; set; }
        public string currency { get; set; }
        public string match_id { get; set; }
        public string trans_id { get; set; }
        public string ba_status { get; set; }
        public string betchoice { get; set; }
        public string odds_type { get; set; }
        public string sport_type { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string operator_id { get; set; }
        public string version_key { get; set; }
        public string after_amount { get; set; }
        public string last_ball_no { get; set; }
        public string ticket_status { get; set; }
        public decimal winlost_amount { get; set; }
        public DateTime? transaction_time { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

    public class BetVirtualSportDetail
    {
        public string os { get; set; }
        public string hdp { get; set; }
        public string odds { get; set; }
        public decimal stake { get; set; }
        public string islive { get; set; }
        public string away_id { get; set; }
        public string betfrom { get; set; }
        public string browser { get; set; }
        public string home_id { get; set; }
        public string away_hdp { get; set; }
        public string bet_team { get; set; }
        public string bet_type { get; set; }
        public string currency { get; set; }
        public string home_hdp { get; set; }
        public string match_id { get; set; }
        public string trans_id { get; set; }
        public string ba_status { get; set; }
        public string exculding { get; set; }
        public string league_id { get; set; }
        public string odds_type { get; set; }
        public string race_lane { get; set; }
        public Parlaydata1[] ParlayData { get; set; }
        public string sport_type { get; set; }
        public string customInfo1 { get; set; }
        public string customInfo2 { get; set; }
        public string customInfo3 { get; set; }
        public string customInfo4 { get; set; }
        public string customInfo5 { get; set; }
        public string operator_id { get; set; }
        public string race_number { get; set; }
        public string version_key { get; set; }
        public string after_amount { get; set; }
        public int parlay_ref_no { get; set; }
        public string ticket_status { get; set; }
        public DateTime? match_datetime { get; set; }
        public decimal winlost_amount { get; set; }
        public DateTime? transaction_time { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

    public class Parlaydata1
    {
        public string hdp { get; set; }
        public string odds { get; set; }
        public string islive { get; set; }
        public string away_id { get; set; }
        public string bet_tag { get; set; }
        public string home_id { get; set; }
        public string away_hdp { get; set; }
        public string bet_team { get; set; }
        public string bet_type { get; set; }
        public string home_hdp { get; set; }
        public string match_id { get; set; }
        public string league_id { get; set; }
        public string parlay_id { get; set; }
        public string away_score { get; set; }
        public string home_score { get; set; }
        public string sport_type { get; set; }
        public string ticket_status { get; set; }
        public DateTime? match_datetime { get; set; }
        public DateTime? winlost_datetime { get; set; }
    }

}
