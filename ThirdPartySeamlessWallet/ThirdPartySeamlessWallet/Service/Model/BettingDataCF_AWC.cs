using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BetCF_AWC
    {
        public IList<BettingDataCF_AWC> txns { get; set; }
    }

    public class BettingDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string currency { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public DateTime betTime { get; set; }
        public string roundId { get; set; }
        public GameInfoBet gameInfo { get; set; }
    }

    public class CancelCF_AWC
    {
        public IList<CancelBetDataCF_AWC> txns { get; set; }
    }

    public class CancelBetDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameCode { get; set; }
        public string gameType { get; set; }
        public string roundId { get; set; }
        public GameInfoBet gameInfo { get; set; }
    }

    public class AdjustBetCF_AWC
    {
        public IList<AdjustBetDataCF_AWC> txns { get; set; }
    }

    public class AdjustBetDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public decimal adjustAmount { get; set; }
        public DateTime updateTime { get; set; }
        public GameInfoBet gameInfo { get; set; }
    }

    public class VoidBetCF_AWC
    {
        public IList<VoidBetDataCF_AWC> txns { get; set; }
    }

    public class VoidBetDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public GameInfoBet gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class UnVoidBetCF_AWC
    {
        public IList<UnvoidBetDataCF_AWC> txns { get; set; }
    }

    public class UnvoidBetDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class GameInfoBet
    {
        public string arenaCode { get; set; }
        public decimal odds { get; set; }
        public int matchno { get; set; }
        public string location { get; set; }
        public decimal realBetAmount { get; set; }
        public int live { get; set; }
        public string matchId { get; set; }
        public DateTime eventDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? voidType { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public decimal? realWinAmt { get; set; }
    }

    public class PayoutCF_AWC
    {
        public IList<SettleDataCF_AWC> txns { get; set; }
    }

    public class SettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public DateTime txTime { get; set; }
        public string roundId { get; set; }
        public GameInfoSettle gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class UnPayoutCF_AWC
    {
        public IList<UnsettleDataCF_AWC> txns { get; set; }
    }

    public class UnsettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public GameInfoSettle gameInfo { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class GameInfoSettle
    {
        public int result { get; set; }
        public int settled { get; set; }
        public string matchResult { get; set; }
        public decimal odds { get; set; }
        public string ip { get; set; }
        public int oddsMode { get; set; }
        public int matchno { get; set; }
        public string arena { get; set; }
        public int bddType { get; set; }
        public string status { get; set; }
    }

    public class VoidSettleCF_AWC
    {
        public IList<VoidSettleDataCF_AWC> txns { get; set; }
    }

    public class VoidSettleDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public decimal betAmount { get; set; }
        public DateTime updateTime { get; set; }
        public int voidType { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class UnVoidSettleCF_AWC
    {
        public IList<UnvoidSettleDataCF_AWC> txns { get; set; }
    }

    public class UnvoidSettleDataCF_AWC
    {
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public DateTime updateTime { get; set; }
        public int voidType { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class RefundCF_AWC
    {
        public IList<RefundDataCF_AWC> txns { get; set; }
    }

    public class RefundDataCF_AWC
    {
        public long txId { get; set; }
        public string platformTxId { get; set; }
        public string userId { get; set; }
        public string platform { get; set; }
        public string gameType { get; set; }
        public string gameCode { get; set; }
        public string gameName { get; set; }
        public string betType { get; set; }
        public decimal betAmount { get; set; }
        public decimal winAmount { get; set; }
        public decimal turnover { get; set; }
        public DateTime betTime { get; set; }
        public DateTime updateTime { get; set; }
        public string roundId { get; set; }
        public string refundPlatformTxId { get; set; }
        public GameInfoRefund gameInfo { get; set; }
    }

    public class RewardCF_AWC
    {
        public IList<RewardDataCF_AWC> txns { get; set; }
    }

    public class RewardDataCF_AWC
    {
        public string promotionTxId { get; set; }
        public string promotionId { get; set; }
        public string promotionTypeId { get; set; }
        public string platform { get; set; }
        public string userId { get; set; }
        public decimal amount { get; set; }
        public DateTime txTime { get; set; }
        public TpCustInfo tpCustInfo { get; set; }
    }

    public class GameInfoRefund
    {
        public string arenaCode { get; set; }
        public int settled { get; set; }
        public string matchResult { get; set; }
        public int oddsMode { get; set; }
        public string arena { get; set; }
        public int bddType { get; set; }
        public int result { get; set; }
        public string odds { get; set; }
        public int matchno { get; set; }
        public string location { get; set; }
        public string refundId { get; set; }
        public int live { get; set; }
        public string status { get; set; }
    }

    public class SettleCF_AWC
    {
        public decimal Stake { get; set; }
        public decimal ValidBet { get; set; }
        public decimal WinLostAmount { get; set; }
        public string userId { get; set; }
        public string platformTxId { get; set; }
        public long SettleTime { get; set; }
        public int TicketStatus { get; set; }
    }

}
