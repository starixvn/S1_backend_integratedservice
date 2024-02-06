using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    #region Base
    
    public class NTConnectResponse
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string message { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Error> errors { get; set; }
    }
    public class Error
    {
        public string message { get; set; }
        public string description { get; set; }
        public string code { get; set; }
    }
    
    #endregion
    
    public class GetTokenResponse : NTConnectResponse
    {
        public string token { get; set; }
        public DateTime token_expire { get; set; }
    }
    
    #region Wager

    public class NTConnectWagerReuqest
    {
        public string token { get; set; }
        public string date_from { get; set; }
        public string date_to { get; set; }
        public int page { get; set; }
    }
    
    public class Wager93ConnectModel : NTConnectResponse
    {
        //public string status { get; set; }
        public int? page { get; set; }
        public int? totalpage { get; set; }
        //public string message { get; set; }
        public string totalrecs { get; set; }
        public List<Wager93Data> data { get; set; }
    }
    
    public class Wager93Data
    {
        public string player { get; set; }
        public string ticket { get; set; }
        public string bet_date { get; set; }
        public string gamecode { get; set; }
        public string game { get; set; }
        public string drawno { get; set; }
        public string bet_amount { get; set; }
        public string bet_odd { get; set; }
        public string bet_typegroup { get; set; }
        public string bet_type { get; set; }
        public string winlosso { get; set; }
        public string status { get; set; }
    }
    
    #endregion
    
}

