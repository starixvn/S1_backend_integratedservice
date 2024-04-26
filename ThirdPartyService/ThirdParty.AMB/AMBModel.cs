namespace ThirdParty.AMB
{
    public class CommonResponse
    {
        public Status status { get; set; }
        public Data? data { get; set; }
    }

    public class DemoUrlResponse
    {
        public Status status { get; set; }
        public string data { get; set; }
    }

    public class GameReportResponse
    {
        public Status status { get; set; }
        public Data data { get; set; }
    }

    public class GameListResponse
    {
        public Status status { get; set; }
        public IList<AMBGames> data { get; set; }
    }
    public class Status
    {
        public int code { get; set; }
        public string message { get; set; }
    }
    public class AMBGames
    {
        public string _id { get; set; }
        public int status { get; set; }
        public Name name { get; set; }
        public string prefix { get; set; }
        public string type { get; set; }
        public int sort { get; set; }
        public string partner { get; set; }
        public string demoUrl { get; set; }
    }
    public class Name
    {
        public string th { get; set; }
        public string en { get; set; }
        public string zh { get; set; }
        public string my { get; set; }
    }

    public class Data
    {
        public string url { get; set; }
    }
    
    public class AMBConfig
    {
        public string PokerAgent { get; set; }
        public string PokerApiUrl { get; set; }
        public string PokerSecretKey { get; set; }
        public List<SlotCurrencyAgent> SlotCurrencyAgent { get; set; }
    }
    public class SlotCurrencyAgent
    {
        public string CurrencyId { get; set; }
        public string Agent { get; set; }
        public string SlotApiUrl { get; set; }
        public string SecretKey { get; set; }
    }
}