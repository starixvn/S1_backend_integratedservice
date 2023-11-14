using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    
    public class HLResponse
    {
        [JsonProperty("response")]
        public HLResponseData hl_Response { get; set; }
    }
    
    public class HLResponseData { 
        
        [JsonProperty("errorCode")]
        public int ErrorCode { get; set; } 
        [JsonProperty("desc")]
        public string Desc { get; set; }
        [JsonProperty("betData")]
        public List<HLBetData> BetData { get; set; } 
    }
    
    public class HLBetData 
    { 
        
        [JsonProperty("betID")]
        public string BetID { get; set; }
        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; }
        [JsonProperty("drawType")]
        public int DrawType { get; set; }
        [JsonProperty("drawDate")]
        public DateTime DrawDate { get; set; } 
        [JsonProperty("betNumber")]
        public string BetNumber { get; set; } 
        [JsonProperty("betPosition")]
        public string BetPosition { get; set; } 
        [JsonProperty("bet")]
        public decimal BetAmount { get; set; }
    }
    
    #region WagerSettledData

    public class HLSettledData
    {
        
        [JsonProperty("betID")]
        public string BetID { get; set; }
        [JsonProperty("dateTime")]
        public DateTime DateTime { get; set; }
        [JsonProperty("drawType")]
        public int DrawType { get; set; }
        [JsonProperty("drawDate")]
        public DateTime DrawDate { get; set; } 
        [JsonProperty("betNumber")]
        public string BetNumber { get; set; } 
        [JsonProperty("strikePosition")]
        public string BetPosition { get; set; } 
        [JsonProperty("bet")]
        public decimal BetAmount { get; set; }
        [JsonProperty("strike")]
        public decimal SettledAmount { get; set; }
        
    }
    #endregion
}