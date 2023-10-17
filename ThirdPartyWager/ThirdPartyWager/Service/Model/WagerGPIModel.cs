using Newtonsoft.Json;

using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.Model
{
    public class WagerGPIModel
    {
        public Resp_GPI resp { get; set; }
    }

    public class Resp_GPI
    {
        public int error_code { get; set; }
        public string error_msg { get; set; }
        public Items_GPI items { get; set; }

    }
    public class Items_GPI
    {
        [JsonProperty("@page_num")]
        public int page_num { get; set; }

        [JsonProperty("@page_size")]
        public int page_size { get; set; }

        [JsonProperty("@total_row")]
        public int total_row { get; set; }

        [JsonProperty("@total_page")]
        public int total_page { get; set; }

        public IList<Bet_GPI> item { get; set; }

    }

    public class Bet_GPI
    {
        [JsonProperty("@winlose")]
        public decimal winlose { get; set; }

        [JsonProperty("@bet_id")]
        public string bet_id { get; set; }

        [JsonProperty("@bundle_id")]
        public string bundle_id { get; set; }

        [JsonProperty("@trans_date")]
        public DateTime trans_date { get; set; }

        [JsonProperty("@user_id")]
        public string user_id { get; set; }

        [JsonProperty("@game_type")]
        public string game_type { get; set; }

        [JsonProperty("@balance")]
        public decimal balance { get; set; }

        [JsonProperty("@bet")]
        public decimal bet { get; set; }

        [JsonProperty("@status")]
        public string status { get; set; }

        [JsonProperty("@game_result")]
        public string game_result { get; set; }

        [JsonProperty("@currency")]
        public string currency { get; set; }

        [JsonProperty("@player_hand")]
        public string player_hand { get; set; }

        [JsonProperty("@table_id")]
        public string table_id { get; set; }

        [JsonProperty("@game_id")]
        public string game_id { get; set; }

        [JsonProperty("@lucky_num")]
        public string lucky_num { get; set; }

        [JsonProperty("@super_pay")]
        public string super_pay { get; set; } = "";

        [JsonProperty("@platform")]
        public string platform { get; set; }

        [JsonProperty("@round_id")]
        public string round_id { get; set; }

        [JsonProperty("@game_code")]
        public string game_code { get; set; }

        [JsonProperty("@fround")]
        public string fround { get; set; }

        [JsonProperty("@jcon")]
        public string jcon { get; set; }

        [JsonProperty("@jwin")]
        public string jwin { get; set; }

        [JsonProperty("@rebate_amount")]
        public string rebate_amount { get; set; }

        public long transUnix
        {
            get
            {
                return new DateTimeOffset(trans_date, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }

        public decimal jconVal
        {
            get
            {
                return (jcon == "") ? 0 : Convert.ToDecimal(jcon);
            }
        }

        public decimal jwinVal
        {
            get
            {
                return (jwin == "") ? 0 : Convert.ToDecimal(jwin);
            }
        }

        public decimal rebateVal
        {
            get
            {
                return (rebate_amount == "") ? 0 : Convert.ToDecimal(rebate_amount);
            }
        }

        public int TpGameType
        {
            get
            {
                var gameType = (int)GameType.GPI_RNG;
                if (string.Equals(game_id, "keno", StringComparison.OrdinalIgnoreCase))
                {
                    gameType = (int)GameType.GPI_Keno;
                }
                else if (string.Equals(game_id, "sode", StringComparison.OrdinalIgnoreCase))
                {
                    gameType = (int)GameType.GPI_Sode;
                }
                else if (string.Equals(game_id, "thailottery", StringComparison.OrdinalIgnoreCase))
                {
                    gameType = (int)GameType.GPI_Thai_Lottery;
                }
                else if (string.Equals(game_id, "fast3", StringComparison.OrdinalIgnoreCase))
                {
                    gameType = (int)GameType.GPI_Fast3;
                }
                return gameType;
            }
        }
    }

}
