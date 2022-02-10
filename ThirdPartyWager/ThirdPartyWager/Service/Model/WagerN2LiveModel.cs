using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class WagerN2LiveModel
    {
        public MessageN2Live message { get; set; }
    }

    public class MessageN2Live
    {
        public string status { get; set; }
        public ResultN2Live result { get; set; }
    }

    public class ResultN2Live
    {
        public GameinfoN2Live gameinfo { get; set; }
        public Element element { get; set; }
    }

    public class Element
    {
        public IList<Properties> properties { get; set; }

    }

    public class GameinfoN2Live
    {
        //public IList<GameN2Live> game { get; set; }
        public dynamic game { get; set; }
    }

    public class GameN2Live
    {
        [JsonProperty("@code")]
        public string code { get; set; }
        //public IList<DealN2Live> deal { get; set; }
        public dynamic deal { get; set; }
    }

    public class DealN2Live
    {
        [JsonProperty("@id")]
        public string id { get; set; }

        [JsonProperty("@startdate")]
        public DateTime startdate { get; set; }

        [JsonProperty("@enddate")]
        public DateTime enddate { get; set; }

        [JsonProperty("@status")]
        public string status { get; set; }

        [JsonProperty("@code")]
        public string code { get; set; }

        public Betinfo betinfo { get; set; }

        public Dealdetails dealdetails { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public Results? results { get; set; }
    }
    public class Betinfo
    {
        public dynamic clientbet { get; set; }

    }
    public class Clientbet
    {
        [JsonProperty("@login")]
        public string login { get; set; }

        [JsonProperty("@bet_amount")]
        public decimal bet_amount { get; set; }

        [JsonProperty("@payout_amount")]
        public decimal payout_amount { get; set; }

        [JsonProperty("@valid_turnover")]
        public decimal valid_turnover { get; set; }

        [JsonProperty("@handle")]
        public decimal handle { get; set; }

        [JsonProperty("@hold")]
        public decimal hold { get; set; }

        [JsonProperty("@betid")]
        public string betid { get; set; }

        //public Betdetail betdetail { get; set; }
        public dynamic betdetail { get; set; }
    }

    public class Betdetail
    {
        [JsonProperty("@schemeid")]
        public string schemeid { get; set; }

        [JsonProperty("@channel")]
        public string @channel { get; set; }
        public string player { get; set; }
    }

    public class Dealdetails
    {
        //public IList<Dealdetail> dealdetail { get; set; }
        public dynamic dealdetail { get; set; }
    }

    public class Dealdetail
    {
        public string side { get; set; }
        public string type { get; set; }
        public string value { get; set; }

    }

    public class Results
    {
        public dynamic result { get; set; }
    }

    public class Properties
    {
        [JsonProperty("@name")]
        public string name { get; set; }

        [JsonProperty("#text")]
        public string text { get; set; }
    }

    public class BetN2Live
    {

        public int gamecode { get; set; }
        public long id { get; set; }
        public DateTime startdate { get; set; }
        public DateTime enddate { get; set; }
        public int status { get; set; }
        public string gameid { get; set; }
        public string login { get; set; }
        public decimal bet_amount { get; set; }
        public decimal payout_amount { get; set; }
        public decimal valid_turnover { get; set; }
        public decimal hold { get; set; }
        public decimal handle { get; set; }
        public long betid { get; set; }
        public long schemeid { get; set; }
        public string channel { get; set; }
        public string betdetail { get; set; }
        public string dealdetail { get; set; }
        public string result { get; set; }

        public string TransId
        {
            get
            {
                return $"{gameid}_{login}";
            }
        }

        public long startUnix
        {
            get
            {
                return new DateTimeOffset(startdate, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
        public long endUnix
        {
            get
            {
                return new DateTimeOffset(enddate, new TimeSpan(0, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }
}
