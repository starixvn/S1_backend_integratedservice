using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataLC_Sexy_Bet
    {
        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("IP")]
        public string Ip { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("roundId")]
        public string RoundId { get; set; }

        [JsonProperty("roundStartTime")]
        public string RoundStartTime { get; set; }

        [JsonProperty("txns")]
        public List<BetTxn> Txns { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("validBetTime")]
        public long ValidBetTime { get; set; }
    }

    public class BetTxn
    {
        [JsonProperty("txId")]
        public string TxId { get; set; }

        [JsonProperty("gameType")]
        public string GameType { get; set; }

        [JsonProperty("betAmount")]
        public decimal BetAmount { get; set; }

        [JsonProperty("betTime")]
        public string BetTime { get; set; }

        [JsonProperty("updateTime")]
        public string UpdateTime { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }

    public class BettingDataLC_Sexy_Settle
    {
        [JsonProperty("validBetTime")]
        public long ValidBetTime { get; set; }

        [JsonProperty("dealerDomain")]
        public string DealerDomain { get; set; }

        [JsonProperty("tableId")]
        public long TableId { get; set; }

        [JsonProperty("roundId")]
        public string RoundId { get; set; }

        [JsonProperty("roundStartTime")]
        public string RoundStartTime { get; set; }

        [JsonProperty("result")]
        public string Result { get; set; }

        [JsonProperty("winner")]
        public string Winner { get; set; }

        [JsonProperty("txns")]
        public List<SettleTxn> Txns { get; set; }
    }

    public class SettleTxn
    {
        [JsonProperty("txId")]
        public string TxId { get; set; }

        [JsonProperty("gameType")]
        public string GameType { get; set; }

        [JsonProperty("betAmount")]
        public decimal BetAmount { get; set; }

        [JsonProperty("updateTime")]
        public string UpdateTime { get; set; }

        [JsonProperty("betTime")]
        public string BetTime { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        [JsonProperty("winLoss")]
        public decimal WinLoss { get; set; }

        [JsonProperty("odds")]
        public decimal Odds { get; set; }

        [JsonProperty("settleWinLoss")]
        public decimal SettleWinLoss { get; set; }

        [JsonProperty("validBet")]
        public decimal ValidBet { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class CancelBetTxn
    {
        public string dealerDomain { get; set; }
        public int tableId { get; set; }
        public string roundId { get; set; }
        public string txId { get; set; }
        public string updateTime { get; set; }
    }
    public class BettingDataLC_Sexy_CancelBet
    {
        public string action { get; set; }
        public string requestId { get; set; }
        public string extension3 { get; set; }
        public string userId { get; set; }
        public IList<CancelBetTxn> txns { get; set; }
    }
    public class Unsettle_VoidRoundTxn
    {
        public string txId { get; set; }
        public decimal betAmount { get; set; }
        public string updateTime { get; set; }
        public string userId { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }
    public class Unsettle_VoidRound
    {
        public string action { get; set; }
        public string requestId { get; set; }
        public long validBetTime { get; set; }
        public string dealerDomain { get; set; }
        public int tableId { get; set; }
        public string roundId { get; set; }
        public IList<Unsettle_VoidRoundTxn> txns { get; set; }
    }

    public class VoidTxnTxn
    {
        public string txId { get; set; }
        public decimal betAmount { get; set; }
        public string updateTime { get; set; }
        public string userId { get; set; }
        public decimal winLoss { get; set; }
        public string status { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class VoidTxn
    {
        public string action { get; set; }
        public string requestId { get; set; }
        public long validBetTime { get; set; }
        public IList<VoidTxnTxn> txns { get; set; }
    }
    public class Sexy_Treat
    {
        public string treatId { get; set; }
        public decimal amount { get; set; }
        public string userId { get; set; }
        public string txId { get; set; }
        public string remark { get; set; }
        public string transferTime { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    #region AESexy (AWC)

    public class CommonData_Sexy
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameCode { get; set; }
        public string gameType { get; set; }
        public string roundId { get; set; }
    }

    public class BetLC_Sexy
    {
        public IList<BettingData_Sexy> txns { get; set; }
    }

    public class BettingData_Sexy : CommonData_Sexy
    {
        public string currency { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public DateTime betTime { get; set; }
        public GameInfoDataLC_Sexy gameInfo { get; set; }
        public long betUnix
        {
            get
            {
                return new DateTimeOffset(betTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class GameInfoDataLC_Sexy
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime? roundStartTime { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? streamerId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? tableId { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? dealerDomain { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public dynamic? result { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? winner { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? ip { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? odds { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? winLoss { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? status { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? realWinAmt { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? voidType { get; set; }
    }

    public class CancelLC_Sexy
    {
        public IList<CancelBetDataLC_Sexy> txns { get; set; }
    }

    public class CancelBetDataLC_Sexy : CommonData_Sexy
    {
        public GameInfoDataLC_Sexy gameInfo { get; set; }
    }

    public class VoidBetLC_Sexy
    {
        public IList<VoidBetDataLC_Sexy> txns { get; set; }
    }

    public class VoidBetDataLC_Sexy : CommonData_Sexy
    {
        public long txId { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public int voidType { get; set; }
        public DateTime updateTime { get; set; }
        public GameInfoDataLC_Sexy gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class PayoutLC_Sexy
    {
        public IList<SettleDataLC_Sexy> txns { get; set; }
    }

    public class SettleDataLC_Sexy : CommonData_Sexy
    {
        public long txId { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime txTime { get; set; }
        public string settleType { get; set; }
        public string refPlatformTxId { get; set; }
        public GameInfoDataLC_Sexy gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class UnPayoutLC_Sexy
    {
        public IList<UnsettleDataLC_Sexy> txns { get; set; }
    }

    public class UnsettleDataLC_Sexy : CommonData_Sexy
    {
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public GameInfoDataLC_Sexy gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class VoidSettleLC_Sexy
    {
        public IList<VoidSettleDataLC_Sexy> txns { get; set; }
    }

    public class VoidSettleDataLC_Sexy : CommonData_Sexy
    {
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public int voidType { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class RewardLC_Sexy
    {
        public IList<RewardDataLC_Sexy> txns { get; set; }
    }

    public class RewardDataLC_Sexy
    {
        public string promotionTxId { get; set; }
        public string promotionId { get; set; }
        public string promotionTypeId { get; set; }
        public string platform { get; set; }
        public string userId { get; set; }
        public decimal amount { get; set; }
        public string currency { get; set; }
        public DateTime txTime { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class BettransDataLC_Sexy
    {
        public string platformTxId { get; set; }
        public decimal betAmount { get; set; }
        public decimal turnover { get; set; }
        public decimal winAmount { get; set; }
        public decimal winLoss { get; set; }
        public int TicketStatus { get; set; }
        public int TicketType { get; set; }
    }

    public class TipLC_Sexy
    {
        public IList<TipData_Sexy> txns { get; set; }
    }

    public class TipData_Sexy
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameCode { get; set; }
        public string gameType { get; set; }
        public string gameName { get; set; }
        public decimal tip { get; set; }
        public string currency { get; set; }
        public DateTime txTime { get; set; }
        public dynamic tipInfo { get; set; }
        public long txUnix
        {
            get
            {
                return new DateTimeOffset(txTime).ToUnixTimeSeconds();
                //return new DateTimeOffset(txTime, new TimeSpan(8, 0, 0)).ToUnixTimeSeconds();
            }
        }
    }

    public class CancelTipLC_Sexy
    {
        public IList<CancelTipDataLC_Sexy> txns { get; set; }
    }

    public class CancelTipDataLC_Sexy
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameCode { get; set; }
        public string gameType { get; set; }
        public string gameName { get; set; }
        public dynamic tipInfo { get; set; }
    }

    public class ResettleLC_Sexy
    {
        public IList<ResettleDataLC_Sexy> txns { get; set; }
    }

    public class ResettleDataLC_Sexy : CommonData_Sexy
    {
        public string currency { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public GameInfoDataLC_Sexy gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
        public DateTime txTime { get; set; }
        public string settleType { get; set; }
        public string refPlatformTxId { get; set; }
    }

    #endregion

    #region Settle

    public class SettleData_Sexy : SettleData
    {
        public new string TransId { get; set; }
        public new string GameId { get; set; }
    }

    #endregion
}
