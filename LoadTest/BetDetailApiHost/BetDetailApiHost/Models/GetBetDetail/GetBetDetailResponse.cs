using System.Text.Json.Serialization;

namespace BetDetailApiHost.Models.GetBetDetail;

public interface ISabaBet
{
    decimal odds { get; set; }
    decimal stake { get; set; }
    public decimal? discount_stake { get; set; }
    public int? odds_type { get; set; }
    public int? bet_type { get; set; }
    public string ticket_status { get; set; }
    string tokenmoney { get; }
}

public class GetBetDetailResponse
{
    [JsonPropertyName("error_code")]
    public int ErrorCode { get; set; } = 0;

    public string Message { get; set; } = "Success";

    public GetBetDetailData Data { get; set; }
}

public class GetBetDetailData
{
    [JsonPropertyName("last_version_key")]
    public long LastVersionKey { get; set; }

    public IEnumerable<BetDetail> BetDetails { get; set; }

    public IEnumerable<BetNumberDetail> BetNumberDetails { get; set; }

    public IEnumerable<BetVirtualSportDetail> BetVirtualSportDetails { get; set; }

    public IEnumerable<dynamic> BetCasinoDetails { get; set; }
}

public class BetDetail : ISabaBet
{
    public string ref_id { get; set; }
    public long trans_id { get; set; }
    public string vendor_member_id { get; set; }
    public string operator_id { get; set; }
    public int? league_id { get; set; }
    public int? match_id { get; set; }
    public int? home_id { get; set; }
    public int? away_id { get; set; }
    public string match_datetime { get; set; }
    public int? sport_type { get; set; }
    public int? bet_type { get; set; }
    public long? parlay_ref_no { get; set; }
    public decimal odds { get; set; }
    public decimal stake { get; set; }
    public decimal? discount_stake { get; set; }
    public string transaction_time { get; set; }
    public string ticket_status { get; set; }
    public decimal winlost_amount { get; set; }
    public decimal after_amount { get; set; }
    public int currency { get; set; }
    public DateTime winlost_datetime { get; set; }
    public int? odds_type { get; set; }
    public string bet_team { get; set; }
    public string isLucky { get; set; }
    public decimal home_hdp { get; set; }
    public decimal away_hdp { get; set; }
    public decimal? hdp { get; set; }
    public string betfrom { get; set; }
    public string islive { get; set; }
    public int? home_score { get; set; }
    public int? away_score { get; set; }
    public string settlement_time { get; set; }
    public string customInfo1 { get; set; }
    public string customInfo2 { get; set; }
    public string customInfo3 { get; set; }
    public string customInfo4 { get; set; }
    public string customInfo5 { get; set; }
    public string ba_status { get; set; }
    public string tokenmoney { get; set; }
    public int version_key { get; set; }
    public object ParlayData { get; set; }
    public decimal? original_stake { get; set; }
    public IEnumerable<Leaguename> leaguename { get; set; }
    public IEnumerable<Sportname> sportname { get; set; }
    public IEnumerable<Bettypename> bettypename { get; set; }
    public string betchoice { get; set; } = string.Empty;
    public string ticket_extra_status { get; set; } = string.Empty;
}

public class Leaguename
{
    public string lang { get; set; }
    public string name { get; set; }
}

public class Sportname
{
    public string lang { get; set; }
    public string name { get; set; }
}

public class Bettypename
{
    public string lang { get; set; }
    public string name { get; set; }
}

public class BetNumberDetail : ISabaBet
{
    public long trans_id { get; set; }
    public string vendor_member_id { get; set; }
    public string operator_id { get; set; }
    public int? match_id { get; set; }
    public DateTime transaction_time { get; set; }
    public decimal odds { get; set; }
    public decimal stake { get; set; }
    public decimal? discount_stake { get; set; }
    public string ticket_status { get; set; }
    public string betfrom { get; set; }
    public string islive { get; set; }
    public int? last_ball_no { get; set; }
    public string bet_team { get; set; }
    public DateTime winlost_datetime { get; set; }
    public int? bet_type { get; set; }
    public int currency { get; set; }
    public int? odds_type { get; set; }
    public int sport_type { get; set; }
    public decimal winlost_amount { get; set; }
    public decimal after_amount { get; set; }
    public string customInfo1 { get; set; }
    public string customInfo2 { get; set; }
    public string customInfo3 { get; set; }
    public string customInfo4 { get; set; }
    public string customInfo5 { get; set; }
    public string ba_status { get; set; }
    public string tokenmoney { get; set; }
    public long version_key { get; set; }
    public string betchoice { get; set; } = string.Empty;
}

public class BetVirtualSportDetail : ISabaBet
{
    public long trans_id { get; set; }
    public string vendor_member_id { get; set; }
    public string operator_id { get; set; }
    public int? league_id { get; set; }
    public int? match_id { get; set; }
    public int? home_id { get; set; }
    public int? away_id { get; set; }
    public string match_datetime { get; set; }
    public int? sport_type { get; set; }
    public int? bet_type { get; set; }
    public long? parlay_ref_no { get; set; }
    public decimal odds { get; set; }
    public decimal stake { get; set; }
    public decimal? discount_stake { get; set; }
    public DateTime transaction_time { get; set; }
    public string ticket_status { get; set; }
    public decimal winlost_amount { get; set; }
    public decimal after_amount { get; set; }
    public int currency { get; set; }
    public DateTime winlost_datetime { get; set; }
    public int? odds_type { get; set; }
    public string bet_team { get; set; }
    public decimal? home_hdp { get; set; }
    public decimal? away_hdp { get; set; }
    public decimal? hdp { get; set; }
    public string betfrom { get; set; }
    public string islive { get; set; }
    public string customInfo1 { get; set; }
    public string customInfo2 { get; set; }
    public string customInfo3 { get; set; }
    public string customInfo4 { get; set; }
    public string customInfo5 { get; set; }
    public string ba_status { get; set; }
    public string tokenmoney { get; set; }
    public long version_key { get; set; }
    public dynamic ParlayData { get; set; }
    public string race_number { get; set; }
    public string race_lane { get; set; }
}