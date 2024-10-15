using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_PG
    {
        public TransferType type { get; set; }

        public string TraceId { get; set; }

        [JsonProperty("game_id")]
        public int GameId { get; set; }

        [JsonProperty("parent_bet_id")]
        public string ParentBetId { get; set; }

        [JsonProperty("bet_id")]
        public string BetId { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("transfer_amount")]
        public decimal TransferAmount { get; set; }
        
        [JsonProperty("real_transfer_amount")]
        public decimal RealTransferAmount { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("bet_transaction_id")]
        public string BetTransactionId { get; set; }

        [JsonProperty("updated_time")]
        public long UpdatedTime { get; set; }

        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        [JsonProperty("is_end_round")]
        public bool IsEndRound { get; set; }
    }

    public class SettleData_PG : SettleData
    {
        public string TransactionIdList { get; set; }
        public string ParentBetId { get; set; }
    }

    public class AdjustmentDataRNG_PG : BettingDataRNG_PG
    {
        [JsonProperty("operator_token")]
        public string OperatorToken { get; set; }

        [JsonProperty("secret_key")]
        public string SecretKey { get; set; }

        [JsonProperty("adjustment_id")]
        public string AdjustmentId { get; set; }

        [JsonProperty("adjustment_transaction_id")]
        public string AdjustmentTransactionId { get; set; }

        [JsonProperty("adjustment_time")]
        public long AdjustmentTime { get; set; }

        [JsonProperty("transaction_type")]
        public string TransactionType { get; set; }

        [JsonProperty("bet_type")]
        public int BetType { get; set; }
    }

    public class TransferInOutDataRNG_PG
    {
        public TransferType type { get; set; }

        public string TraceId { get; set; }

        [JsonProperty("player_name")]
        public string PlayerName { get; set; }

        [JsonProperty("game_id")]
        public int GameId { get; set; }

        [JsonProperty("parent_bet_id")]
        public string ParentBetId { get; set; }

        [JsonProperty("bet_id")]
        public string BetId { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("bet_amount")]
        public decimal BetAmount { get; set; }

        [JsonProperty("win_amount")]
        public decimal WinAmount { get; set; }

        [JsonProperty("transfer_amount")]
        public decimal TransferAmount { get; set; }
        
        [JsonProperty("real_transfer_amount")]
        public decimal RealTransferAmount { get; set; }

        [JsonProperty("transaction_id")]
        public string TransactionId { get; set; }

        [JsonProperty("wallet_type")]
        public string WalletType { get; set; }

        [JsonProperty("is_minus_count")]
        public bool IsMinusCount { get; set; }

        [JsonProperty("bet_type")]
        public int BetType { get; set; }

        [JsonProperty("updated_time")]
        public long UpdatedTime { get; set; }

        [JsonProperty("is_validate_bet")]
        public bool IsValidateBet { get; set; }

        [JsonProperty("is_adjustment")]
        public bool IsAdjustment { get; set; }

        [JsonProperty("is_parent_zero_stake")]
        public bool IsParentZeroStake { get; set; }

        [JsonProperty("is_feature")]
        public bool IsFeature { get; set; }

        [JsonProperty("is_feature_buy")]
        public bool IsFeatureBuy { get; set; }

        [JsonProperty("is_wager")]
        public bool IsWager { get; set; }

        [JsonProperty("free_game_transaction_id")]
        public string FreeGameTransactionId { get; set; }

        [JsonProperty("free_game_name")]
        public string FreeGameName { get; set; }

        [JsonProperty("free_game_id")]
        public int FreeGameId { get; set; }

        [JsonProperty("bonus_transaction_id")]
        public string BonusTransactionId { get; set; }

        [JsonProperty("bonus_name")]
        public string BonusName { get; set; }

        [JsonProperty("bonus_id")]
        public int BonusId { get; set; }

        [JsonProperty("bonus_balance_amount")]
        public decimal BonusBalanceAmount { get; set; }

        [JsonProperty("bonus_ratio_amount")]
        public decimal BonusRatioAmount { get; set; }

        [JsonProperty("jackpot_rtp_contribution_amount")]
        public decimal JackpotRtpContributionAmount { get; set; }

        [JsonProperty("is_end_round")]
        public bool IsEndRound { get; set; }

        [JsonProperty("platform")]
        public string Platform { get; set; }

        [JsonProperty("create_time")]
        public long CreateTime { get; set; }

        [JsonProperty("jackpot_win_amount")]
        public decimal JackpotWinAmount { get; set; }

        [JsonProperty("jackpot_pool_id")]
        public string JackpotPoolId { get; set; }

        [JsonProperty("jackpot_type")]
        public string JackpotType { get; set; }

        [JsonProperty("bet_transaction_id")]
        public string BetTransactionId { get; set; }
    }

    public class TpBetFailureRecordInfo_PG
    {
        public decimal CurrentBalance { get; set; }
        public decimal BetAmount { get; set; }
        public decimal WinAmount { get; set; }
        public decimal TransferAmount { get; set; }
        public decimal ExchangeRate { get; set; }
        public decimal InsufficientBalance { get; set; }
    }

    public class ResettleDataRNG_PG
    {
        public TransferType type { get; set; }

        public string TraceId { get; set; }

        [JsonProperty("GameId")]
        public int GameId { get; set; }

        [JsonProperty("ParentBetId")]
        public string ParentBetId { get; set; }

        [JsonProperty("BetId")]
        public string BetId { get; set; }

        [JsonProperty("PlayerName")]
        public string PlayerName { get; set; }

        [JsonProperty("CurrencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("TransferAmount")]
        public decimal TransferAmount { get; set; }

        [JsonProperty("TransactionId")]
        public string TransactionId { get; set; }

        [JsonProperty("BetTransactionId")]
        public string BetTransactionId { get; set; }

        [JsonProperty("IsEndRound")]
        public string IsEndRound { get; set; }

        [JsonProperty("BetAmount")]
        public decimal BetAmount { get; set; }

        [JsonProperty("WinAmount")]
        public decimal WinAmount { get; set; }

        [JsonProperty("WalletType")]
        public string WalletType { get; set; }
    }
}
