namespace ThirdParty.SBSW
{
    public class Result
    {
        public int error_code { get; set; }
        public string message { get; set; }
    }

    public class Req
    {
        public string vendor_id { set; get; }
        public string vendor_member_id { set; get; }
    }

    public class CreateMemberReq : Req
    {
        public string operatorId { set; get; }
        public string userName { set; get; }
        public string oddsType { set; get; }
        public int currency { set; get; }
        public decimal maxtransfer { set; get; }
        public decimal mintransfer { set; get; }
    }

    public class LoginResult : Result
    {
        public string Data { set; get; }
    }

    public class SetMemberBetSettingReq : Req
    {
        public string bet_setting { get; set; }
    }

    public class GetBetSettingLimitReq
    {
        public string vendor_id { set; get; }
        public string currency { set; get; }
    }

    public class SetMemberBetSettingResult
    {
        public int error_code { get; set; }
        public string message { get; set; }
    }

    public class GetGameDetailResult : Result
    {
        public GameDetail[] Data { set; get; }
    }

    public class GameDetail
    {
        public int match_id { get; set; }
        public int league_id { get; set; }
        public int home_id { get; set; }
        public int away_id { get; set; }
        public int home_score { get; set; }
        public int away_score { get; set; }
        public int ht_home_score { get; set; }
        public int ht_away_score { get; set; }
        public string game_status { get; set; }
        public int sport_type { get; set; }
        public bool is_neutral { get; set; }
    }


    public class GetTokenResult : Result
    {
        public string Data { set; get; }
    }

    public class VerifyTokenResult : Result
    {
        public VerifyResultModel Data { set; get; }
    }

    public class VerifyResultModel
    {
        public string vendor_member_id { set; get; }
    }
}
