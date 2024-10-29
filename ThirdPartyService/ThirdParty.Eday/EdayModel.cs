using Newtonsoft.Json;

namespace ThirdParty.Eday
{
    public class EdayConfig
    {
        public string ApiUrl { get; set; }
        public string Operator { get; set; }
        public string ApiKey { get; set; }
        public string AgentName { get; set; }
    }
    
    public class EdayResult<T>
    {
        public string resultCode { get; set; }
        public string message { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public T data { get; set; }
    }

    public class EdayLoginResponse
    {
        public string sessionId { get; set; }
        public string requestId { get; set; }
        public string url { get; set; }
        public string gameUrl { get; set; }
    }
}