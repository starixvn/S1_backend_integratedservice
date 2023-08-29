namespace ThirdParty.IM;

public partial class TpHelper
{
    public class RegisterModel
    {
        public string MerchantCode { get; set; }
        public string PlayerId { get; set; }
        public string Currency { get; set; }
        public string Password { get; set; }
    }
    
    public class LaunchGameModel
    {
        public string MerchantCode { get; set; }
        public string? PlayerId { get; set; }
        public string GameCode { get; set; }
        public string Language { get; set; }
        public string IpAddress { get; set; }
        public int ProductWallet { get; set; }
        public string LobbyURL { get; set; }
    }
    
    public class RegisterResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string Currency { get; set; }
    }
    
    public class LaunchGameResponseModel
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public string GameUrl { get; set; }
        public string SessionToken { get; set; }
        public int TTL { get; set; }
    }
    
    public enum ProductWallet
    {
        SPRIBE = 101,
        IMSB = 301,
        ESportsbull = 401
    }

    public class WagerDetailSport
    {
        public string Market { get; set; }
        public string EventName { get; set; }
        public string EventDateTime { get; set; }
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; }
        public int HomeTeamId { get; set; }
        public string HomeTeamName { get; set; }
        public int AwayTeamId { get; set; }
        public string AwayTeamName { get; set; }
        public string FavTeam { get; set; }
        public string BetType { get; set; }
        public string BetTypeDesc { get; set; }
        public string Period { get; set; }
        public string Selection { get; set; }
        public decimal Odds { get; set; }
        public string HomeTeamHTScore { get; set; }
        public string AwayTeamHTScore { get; set; }
        public string HomeTeamFTScore { get; set; }
        public string AwayTeamFTScore { get; set; }
        public string WagerHomeTeamScore { get; set; }
        public string WagerAwayTeamScore { get; set; }
        public string OverallScore { get; set; }
        public string Handicap { get; set; }
        public string IsWagerItemCancelled { get; set; }
        public string wagerItemCancelType { get; set; }
        public string wagerItemCancelReason { get; set; }
        public string SportsName { get; set; }
        public string EventId { get; set; }
        public string Specifier { get; set; }
    }
    
    public class WagerDetailESport
    {
        public string Odds { get; set; }
        public string Period { get; set; }
        public string BetType { get; set; }
        public string EventId { get; set; }
        public string FavTeam { get; set; }
        public string Handicap { get; set; }
        public int SportsID { get; set; }
        public string EventName { get; set; }
        public string EventType { get; set; }
        public string GameOrder { get; set; }
        public string IsSettled { get; set; }
        public string Selection { get; set; }
        public string BaseTierID { get; set; }
        public string MarketType { get; set; }
        public string SportsName { get; set; }
        public string IsResettled { get; set; }
        public string AwayTeamName { get; set; }
        public string BaseTierCode { get; set; }
        public string BaseTierName { get; set; }
        public string CancelReason { get; set; }
        public string HomeTeamName { get; set; }
        public string ResultStatus { get; set; }
        public string EventDateTime { get; set; }
        public string AwayTeamFTScore { get; set; }
        public string AwayTeamHTScore { get; set; }
        public string CompetitionName { get; set; }
        public string HomeTeamFTScore { get; set; }
        public string HomeTeamHTScore { get; set; }
        public string WagerAwayTeamScore { get; set; }
        public string WagerHomeTeamScore { get; set; }
        public string WagerItemCancelType { get; set; }
        public string IsWagerItemCancelled { get; set; }
    }
}