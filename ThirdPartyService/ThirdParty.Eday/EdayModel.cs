namespace ThirdParty.Eday
{
    public class EdayConfig
    {
        public string ApiUrl { get; set; }
        public string Operator { get; set; }
        public string ApiKey { get; set; }
        public string AgentName { get; set; }
    }
    
    public class EdayResult
    {
        public string resultCode { get; set; }
        public string message { get; set; }
    }
}