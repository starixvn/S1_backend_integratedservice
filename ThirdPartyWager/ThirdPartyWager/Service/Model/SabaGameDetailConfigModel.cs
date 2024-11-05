using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class SabaGameDetailConfigModel
    {
        public string ApiUrl { get; set; }

        public string VendorId { get; set; }
    }
    
    public class SabaGameDetailResponse
    {
        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("Data")]
        public List<SabaGameDetailResponseModel> Data { get; set; }
    }
    
    public class SabaGameDetailResponseModel
    {
        [JsonProperty("league_id")]
        public int LeagueId { get; set; }

        [JsonProperty("match_id")]
        public int MatchId { get; set; }

        [JsonProperty("home_id")]
        public int HomeId { get; set; }

        [JsonProperty("away_id")]
        public int AwayId { get; set; }

        [JsonProperty("match_datetime")]
        public DateTime MatchDatetime { get; set; }

        [JsonProperty("sport_type")]
        public int SportType { get; set; }

        [JsonProperty("home_score")]
        public int HomeScore { get; set; }

        [JsonProperty("away_score")]
        public int AwayScore { get; set; }

        [JsonProperty("ht_home_score")]
        public int HtHomeScore { get; set; }

        [JsonProperty("ht_away_score")]
        public int HtAwayScore { get; set; }

        [JsonProperty("game_status")]
        public string GameStatus { get; set; }

        [JsonProperty("VirtualSport_info")]
        public string VirtualSportInfo { get; set; }

        [JsonProperty("first_ball")]
        public string FirstBall { get; set; }

        [JsonProperty("second_ball")]
        public string SecondBall { get; set; }

        [JsonProperty("third_ball")]
        public string ThirdBall { get; set; }

        [JsonProperty("fourth_ball")]
        public string FourthBall { get; set; }

        [JsonProperty("fifth_ball")]
        public string FifthBall { get; set; }

        [JsonProperty("GameNo")]
        public string GameNo { get; set; }

        [JsonProperty("total_sum")]
        public int TotalSum { get; set; }

        [JsonProperty("over_count")]
        public int OverCount { get; set; }

        [JsonProperty("under_count")]
        public int UnderCount { get; set; }

        [JsonProperty("is_neutral")]
        public bool IsNeutral { get; set; }

        [JsonProperty("goal_sequence")]
        public string GoalSequence { get; set; }

        [JsonProperty("corner_sequence")]
        public string CornerSequence { get; set; }

        [JsonProperty("win_item")]
        public string WinItem { get; set; }

        [JsonProperty("qX_home_score")]
        public int QXHomeScore { get; set; }

        [JsonProperty("qX_away_score")]
        public int QXAwayScore { get; set; }
    }
}