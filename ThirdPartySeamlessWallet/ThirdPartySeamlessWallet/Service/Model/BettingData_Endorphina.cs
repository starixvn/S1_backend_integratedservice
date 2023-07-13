using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_Endorphina
    {
        public decimal amount { get; set; } = 0;
        public string currency { get; set; }
        public long date { get; set; }
        public string game { get; set; }
        public int gameId { get; set; }
        public long id { get; set; }
        public string player { get; set; } = "";
        public string token { get; set; }
        public string sign { get; set; }
        [JsonIgnore]
        public decimal InnerAmount { get; set; }
    }

    public class CancelBetData_Endorphina : BettingData_Endorphina
    {
    }

    public class PayoutData_Endorphina
    {
        public long amount { get; set; } = 0;
        public string betSessionId { get; set; } = "";
        public string betTransactionId { get; set; } = null;
        public string currency { get; set; }
        public long date { get; set; }
        public string game { get; set; }
        public int gameId { get; set; }
        public long id { get; set; }
        public string player { get; set; } = "";
        public bool? progressive { get; set; } = null;
        public string progressiveDesc { get; set; } = "";
        public string token { get; set; }
        public string sign { get; set; }
        [JsonIgnore]
        public decimal InnerAmount { get; set; }
    }

    public class EndSessionData_Endorphina
    {
        public long bet { get; set; } = 0;
        public long bonusBet { get; set; } = 0;
        public int bonusGames { get; set; }
        public long bonusWin { get; set; } = 0;
        public string currency { get; set; }
        public string game { get; set; } = "";
        public int games { get; set; }
        public long jackpotWin { get; set; } = 0;
        public int jackpots { get; set; }
        public string player { get; set; } = "";
        public long win { get; set; } = 0;
        public string token { get; set; }
        public string sign { get; set; }
        [JsonIgnore]
        public decimal InnerBet { get; set; }
        [JsonIgnore]
        public decimal InnerBonusBet { get; set; }
        [JsonIgnore]
        public decimal InnerBonusWin { get; set; }
        [JsonIgnore]
        public decimal InnerJackpotWin { get; set; }
        [JsonIgnore]
        public decimal InnerWin { get; set; }
    }

    public class BettransData_Endorphina
    {
        public int gameId { get; set; }
        public long id { get; set; }
        public string player { get; set; }
        public string token { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
    }
}
