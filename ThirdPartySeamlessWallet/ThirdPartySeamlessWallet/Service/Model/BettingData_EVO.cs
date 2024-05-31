using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_EVO
    {
        public Transaction_EVO transaction { get; set; }
        public string sid { get; set; }
        public string userId { get; set; }
        public string uuid { get; set; }
        public string currency { get; set; }
        public Game_EVO game { get; set; }
    }

    public class Transaction_EVO
    {
        public string id { get; set; }
        public string refId { get; set; }
        public decimal amount { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Game_EVO
    {
        public string id { get; set; }
        public string type { get; set; }
        public Details_EVO details { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Details_EVO
    {
        public Table_EVO table { get; set; }
    }

    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Table_EVO
    {
        public string id { get; set; }
        public string vid { get; set; }
    }

    public class PromoDataCommon_EVO
    {
        public string sid { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public Game_EVO? game { get; set; }
        public string uuid { get; set; }
    }

    public class PromoTransactionCommon_EVO
    {
        public string type { get; set; }
        public string id { get; set; }
        public decimal amount { get; set; }
    }

    public class PromoData_EVO : PromoDataCommon_EVO
    {
        public PromoTransaction_EVO promoTransaction { get; set; }
    }

    public class PromoTransaction_EVO : PromoTransactionCommon_EVO
    {
        public List<Jackpot_EVO> jackpots { get; set; }
        public string voucherId { get; set; }
        public decimal playableBalance { get; set; }
        public int remainingRounds { get; set; }
        public string bonusConfigId { get; set; }
        public string rewardId { get; set; }
    }
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class Jackpot_EVO
    {
        public string id { get; set; }
        public decimal winAmount { get; set; }
    }

    public class BettransData_EVO
    {
        public string TransactionId { get; set; }
        public string TransactionRefId { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    public class Reward_EVO
    {
        public int SQ { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public string? game { get; set; }
        public string? promoTransaction { get; set; }
        public string uuid { get; set; }
        public string? GameId { get; set; }
        public string? GameType { get; set; }
        public string? TableId { get; set; }
        public string? TableVId { get; set; }
        public string PromoTransactionType { get; set; }
        public string PromoTransactionId { get; set; }
        public decimal PromoTransactionAmount { get; set; }
        public string? PromoTransactionVoucherId { get; set; }
        public int? PromoTransactionRemainingRounds { get; set; }
        public decimal? PromoTransactionPlayableBalance { get; set; }
        public string? PromoTransactionBonusConfigId { get; set; }
        public string? PromoTransactionRewardId { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public PaidFromEnum PaidFrom { get; set; }
        public BettransType TicketType { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public SettleFlag SettleFlag { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class SettledReward_EVO
    {
        public int SQ { get; set; }
        public int TpId { get; set; }
        public int TpGameType { get; set; }
        public string PromoTransactionId { get; set; }
        public string userId { get; set; }
        public decimal PromoTransactionAmount { get; set; }
        public long CreateTime { get; set; }
        public TicketStatus TicketStatus { get; set; }
        public BettransType TicketType { get; set; }
    }

    public class SettleData_NLC : SettleData
    {
        public string status { get; set; }
    }

    public class SettleData_EVO : SettleData
    {
        public string status { get; set; }
    }

    public class NLCConfig : TpSiteConfig
    {
        public string ApiUrl_Backup { get; set; }
        public string EcId { get; set; }
        public string Ua2Token { get; set; }
        public string AuthToken { get; set; }
        public string CasinoKey { get; set; }
        public string GameProvider { get; set; }
        public string GameHistoryApiToken { get; set; }
    }

    public class EVOConfig : NLCConfig
    {
    }

    public class NetentConfig : NLCConfig
    {
    }

    public class RedTigerConfig : NLCConfig
    {
    }
}
