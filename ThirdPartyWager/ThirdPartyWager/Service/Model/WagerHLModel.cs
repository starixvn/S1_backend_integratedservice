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
        public List<HLWagerData> WagerData { get; set; } 
    }
    
    public class HLWagerData 
    { 
        
        [JsonProperty("betID")]
        public string BetID { get; set; }
        [JsonProperty("loginID")]
        public string UserName { get; set; }
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
        [JsonProperty("strikePosition")]
        public string StrikePosition  { get; set; }
        [JsonProperty("bet")]
        public decimal BetAmount { get; set; }
        [JsonProperty("strike")]
        public string SettleAmount { get; set; }
        public int SettleFlag { get; set; }
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

    #region DrawResult
    
    public class HLDrawResultResponse
    {
	    [JsonProperty("response")]
	    public DrawResultResponse hl_Response { get; set; }
    }
    
	public class DrawResultResponse 
	{ 
	
		[JsonProperty("errorCode")] 
		public int ErrorCode { get; set; } 
	
		[JsonProperty("desc")] 
		public object Desc { get; set; } 
	
		[JsonProperty("drawResult")] 
		public List<DrawResult> DrawResult { get; set; } 
	}

	
	public class DrawResult 
	{ 
	
		[JsonProperty("drawType")] 
		public int drawType { get; set; } 
		[JsonProperty("drawDate")] 
		public DateTime drawDate { get; set; } 
		public override bool Equals(object obj)
		{
			return obj is DrawResult item &&
			       drawType == item.drawType &&
			       drawDate == item.drawDate;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(drawType, drawDate);
		}
	}

    #endregion
    
    #region DataModel

    public class HLBetTrans
    {
	   public string betID { get; set; }
	   public string loginID { get; set; }
	   public DateTime dateTime { get; set; }
	   public int drawType { get; set; }
	   public DateTime drawDate { get; set; }
	   public string betNumber { get; set; }
	   public string betPosition { get; set; }
	   public string strikePosition { get; set; }
	   public decimal bet { get; set; }
	   public decimal strike { get; set; }
	   public string SettleTimeUnix { get; set; }
	   public int SettleFlag { get; set; }
	   public DateTime CreateTime { get; set; }
    }
    
    #endregion
    
}