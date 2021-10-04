using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    public class Game_EVO
    {
        public string id { get; set; }
        public string type { get; set; }
        public Details_EVO details { get; set; }
    }

    public class Details_EVO
    {
        public Table_EVO table { get; set; }
    }

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
        public Game_EVO game { get; set; }
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
    }

    public class Jackpot_EVO
    {
        public string id { get; set; }
        public decimal winAmount { get; set; }
    }

    public class PromoDebitData_EVO : PromoDataCommon_EVO
    {
        public PromoTransactionDebit promoTransaction { get; set; }
    }

    public class PromoTransactionDebit : PromoTransactionCommon_EVO
    {
        public string refId { get; set; }
        public string voucherId { get; set; }
        public int remainingRounds { get; set; }
        public List<JackpotDebit> jackpots { get; set; }
    }

    public class JackpotDebit
    {
        public string id { get; set; }
        public decimal contributionAmount { get; set; }
        public List<ContributionDebit> contributions { get; set; }
    }

    public class ContributionDebit
    {
        public string pot { get; set; }
        public decimal amount { get; set; }
    }

    public class PromoCreditData_EVO : PromoDataCommon_EVO
    {
        public PromoTransactionCredit promoTransaction { get; set; }
    }

    public class PromoTransactionCredit : PromoTransactionCommon_EVO
    {
        public string refId { get; set; }
        public string voucherId { get; set; }
        public int remainingRounds { get; set; }
    }

    public class PromoCancelData_EVO : PromoDataCommon_EVO
    {
        public PromoTransactionCancel promoTransaction { get; set; }
    }

    public class PromoTransactionCancel : PromoTransactionCommon_EVO
    {
        public string refId { get; set; }
        public string voucherId { get; set; }
        public int remainingRounds { get; set; }
        public List<JackpotCancel> jackpots { get; set; }
    }

    public class JackpotCancel
    {
        public string id { get; set; }
        public decimal contributionAmount { get; set; }
    }

    public class PromoPayoutData_EVO : PromoDataCommon_EVO
    {
        public PromoTransactionPayout promoTransaction { get; set; }
    }

    public class PromoTransactionPayout : PromoTransactionCommon_EVO
    {
        public string voucherId { get; set; }
        public int remainingRounds { get; set; }
    }
}
