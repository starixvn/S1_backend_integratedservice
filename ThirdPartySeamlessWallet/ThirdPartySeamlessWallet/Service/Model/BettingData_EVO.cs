using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
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

    #region Response Model

    //public class CommonResponse
    //{
    //    public string status { get; set; }
    //    public string uuid { get; set; }
    //}
    ///// <summary>
    ///// CheckUserResponse(Check、Sid)
    ///// </summary>
    //public class CheckUserResponse : CommonResponse
    //{
    //    public string sid { get; set; }
    //}

    ///// <summary>
    ///// StandardResponse(Balance、Debit、Credit、Cancel、PromoPayout)
    ///// </summary>
    //public class StandardResponse : CommonResponse
    //{
    //    public decimal balance { get; set; }
    //    public decimal bonus { get; set; }
    //    public bool retransmission { get; set; }
    //}

    #endregion

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


    //public class PromoDebitData_EVO : PromoDataCommon_EVO
    //{
    //    public PromoTransactionDebit promoTransaction { get; set; }
    //}

    //public class PromoTransactionDebit : PromoTransactionCommon_EVO
    //{
    //    public string refId { get; set; }
    //    public string voucherId { get; set; }
    //    public int remainingRounds { get; set; }
    //    public List<JackpotDebit> jackpots { get; set; }
    //}

    //public class JackpotDebit
    //{
    //    public string id { get; set; }
    //    public decimal contributionAmount { get; set; }
    //    public List<ContributionDebit> contributions { get; set; }
    //}

    //public class ContributionDebit
    //{
    //    public string pot { get; set; }
    //    public decimal amount { get; set; }
    //}

    //public class PromoCreditData_EVO : PromoDataCommon_EVO
    //{
    //    public PromoTransactionCredit promoTransaction { get; set; }
    //}

    //public class PromoTransactionCredit : PromoTransactionCommon_EVO
    //{
    //    public string refId { get; set; }
    //    public string voucherId { get; set; }
    //    public int remainingRounds { get; set; }
    //}

    //public class PromoCancelData_EVO : PromoDataCommon_EVO
    //{
    //    public PromoTransactionCancel promoTransaction { get; set; }
    //}

    //public class PromoTransactionCancel : PromoTransactionCommon_EVO
    //{
    //    public string refId { get; set; }
    //    public string voucherId { get; set; }
    //    public int remainingRounds { get; set; }
    //    public List<JackpotCancel> jackpots { get; set; }
    //}

    //public class JackpotCancel
    //{
    //    public string id { get; set; }
    //    public decimal contributionAmount { get; set; }
    //}

    //public class PromoPayoutData_EVO : PromoDataCommon_EVO
    //{
    //    public PromoTransactionPayout promoTransaction { get; set; }
    //}

    //public class PromoTransactionPayout : PromoTransactionCommon_EVO
    //{
    //    public string voucherId { get; set; }
    //    public int remainingRounds { get; set; }
    //}
}
