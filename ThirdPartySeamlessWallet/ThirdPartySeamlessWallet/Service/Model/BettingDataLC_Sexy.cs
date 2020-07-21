using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.enums;

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
}
