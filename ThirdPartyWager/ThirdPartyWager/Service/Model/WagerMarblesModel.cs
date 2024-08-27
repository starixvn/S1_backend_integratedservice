namespace ThirdPartyWager.Service.Model
{ 
    public class MarblesConfig
    {
        public string ApiUrl { get; set; }
        public string GameUrl { get; set; }
        public string WagerUrl { get; set; }
            
        public string MerchantId { get; set; }
        public string MerchantCode { get; set; }
        public string AgentKey { get; set; }
        public string AgentId { get; set; }
    }

    public class MarblesGetWagerRequest
    {
        public string handle { get; set; }
        public string lineCode { get; set; }
        public string agent { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string page { get; set; }
        public string pageSize { get; set; }
        public string mode { get; set; }
    }
    
    public class MBSHandleResponse
    {
        public string code { get; set; }
        public string msg { get; set; }
        public MBSHandleResponseData data { get; set; }
    }

    public class MBSHandleResponseData
    {
        public int total { get; set; }
        public int page { get; set; }
        public int pages { get; set; }
        public int pageSize { get; set; }
        public List<MBSBetsData> bets { get; set; }
    }

    public class MBSBetsData
    {
        public string agent { get; set; }
        public string lineCode { get; set; }
        public string account { get; set; }
        public string currency { get; set; }
        public string transNo { get; set; }
        public string gameNo { get; set; }
        public string gameCode { get; set; }
        public string gameInfo { get; set; }
        public string version { get; set; }
        public long startTime { get; set; }
        public long endTime { get; set; }
        public decimal bet { get; set; }
        public decimal effecBet { get; set; }
        public decimal profit { get; set; }
        public decimal commission { get; set; }
        public decimal transBefore { get; set; }
        public decimal transAfter { get; set; }
        public int status { get; set; }
    }
    
}