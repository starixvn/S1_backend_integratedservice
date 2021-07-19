using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_PG
    {
        public TransferType type { get; set; }
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

    public class SettleData_PG
    {
        public string TransactionIdList { get; set; }
        public string ParentBetId { get; set; }
        public decimal Stake { get; set; }
        public decimal WinLostAmount { get; set; }
        public string Account { get; set; }
        public long SettleTime { get; set; }
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
}
