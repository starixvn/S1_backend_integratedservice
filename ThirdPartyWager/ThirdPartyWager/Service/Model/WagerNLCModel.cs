namespace ThirdPartyWager.Service.Model
{
    public class Participant_NLC : Participant_EVO
    {
        public string playerGameId { get; set; } = "";
        public string skinId { get; set; } = "";
        public string brandId { get; set; } = "";
        public string mathId { get; set; } = "";
        public string currencyRateVersion { get; set; } = "";
    }

    public class Wager_NLC : Wager_EVO
    {
        public string mathId { get; set; }
        public string os { get; set; }
        public string device { get; set; }
        public string playerGameId { get; set; }
        public string skinId { get; set; }
        public string brandId { get; set; }
        public string currencyRateVersion { get; set; }
    }
}
