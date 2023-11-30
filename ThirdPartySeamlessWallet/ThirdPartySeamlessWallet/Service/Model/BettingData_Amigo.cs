namespace ThirdPartySeamlessWallet.Service.Model
{
    public class AuthReq
    {
        public string hash { get; set; }
        public string token { get; set; }
        public string providerId { get; set; }
        public string gameId { get; set; }
    }

    public class BalanceReq
    {
        public string hash { get; set; }
        public string providerId { get; set; }
        public string userId { get; set; }
        public string token { get; set; }
        public string gameId { get; set; }
    }

    public class SessionExpiredReq
    {
        public string hash { get; set; }
        public string providerId { get; set; }
        public string sessionId { get; set; }
        public string playerId { get; set; }
        public string token { get; set; }
        public string gameId { get; set; }
    }

    public class BettingData_Amigo
    {
        public string hash { get; set; }
        public string userId { get; set; }
        public string gameId { get; set; }
        public long roundId { get; set; }
        public decimal amount { get; set; }
        public string reference { get; set; }
        public string providerId { get; set; }
        public long timestamp { get; set; }
        public string roundDetails { get; set; } = "";
        public string token { get; set; }
        public string bonusCode { get; set; } = "";
        public int TicketType { get; set; }
    }

    public class PayoutData_Amigo : BettingData_Amigo
    {
        public long jackpotId { get; set; }
    }

    public class AmigoConfig : TpSiteConfig
    {
        public string Brand { get; set; }
        public string SecretKey { get; set; }
        public int StyleName { get; set; }
        public string FunGameUrl { get; set; }
    }
}
