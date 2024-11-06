using System.ComponentModel;

using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore.Model
{
    public class TpInfo
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public string LangId { set; get; }
        public int AppType { set; get; }
        public int SiteId { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }
    public class TpTransfer
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public string LangId { set; get; }
        public decimal Amount { set; get; }
        public Direction direction { set; get; }
        public Dictionary<string, string> inputs { set; get; }
        public int BonusId { set; get; }
    }
    public class AdminTpTransfer : TpTransfer
    {
        public string Operator { set; get; }
        public string Remarks { set; get; }
        public int Category { set; get; }
        public int DepositId { set; get; }
        public bool IsDepositId { set; get; }
    }

    public class TransferResult
    {
        public TransferResult()
        {
            Status = TransferStatus.Pending;
        }
        public TransferStatus Status { get; set; }
        public string ThirdId { get; set; }
        public decimal Difference { get; set; }
    }
    public class AdjustWalletResult : TransferResult
    {
        public AdjustWalletResult() { }
        public AdjustWalletResult(TransferResult result, string transId)
        {
            base.Status = result.Status;
            base.ThirdId = result.ThirdId;
            base.Difference = result.Difference;
            TransId = transId;
        }
        public string TransId { get; set; }
    }

    public class AutoTransferResult
    {
        public string Guid { get; set; }
        public int FromTpId { get; set; }
        public int ToTpId { get; set; }
        public int CustId { get; set; }
        public string MainBalanceBefore { get; set; }
        public string MainBalanceAfter { get; set; }
        public string GameBalance { get; set; }
        public int AutoTransfer { get; set; }
    }
    public class UserBalance
    {
        public decimal balance { get; set; }
        public decimal bonus { get; set; }
        public decimal outstanding { get; set; }
        public byte Currency { get; set; }
    }

    public class TpCustInfo
    {
        public int TpId { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string TpUserName { get; set; }
        public string TpPwd { get; set; }
        public string TpCurrencyId { get; set; }
        public int CurrencyId { get; set; }
        public decimal ExchangeRate { get; set; }
        public bool IsTest { get; set; }
        public int ActStatus { get; set; }
        public int Status { get; set; }
        public int CStatus { get; set; }
        public string IsoCode { get; set; }
        public string TpCustId { get; set; } = ""; //Default 空字串
        public CustomerMemberTypeEnum MemberType { get; set; }
        public string GameId { get; set; }
    }

    public class TpMerchantInfo
    {
        /// <summary>
        /// 第三方Id
        /// </summary>
        public int TpId { get; set; }

        /// <summary>
        /// 站台Id
        /// </summary>
        public int SiteId { get; set; }

        /// <summary>
        /// 幣別Id
        /// </summary>
        public string TpCurrencyId { get; set; }
    }

    public class TransIdModel
    {
        public string ThirdId { set; get; }
        public bool Implemented { set; get; }
    }

    public class TpTransferInfoModel
    {
        public string TransId { set; get; }
        public int TpId { set; get; }
        public string ThirdId { set; get; }
        public int CustId { set; get; }
        public int Direction { set; get; }
        public decimal Amount { set; get; }
        public int Status { set; get; }
        public DateTime CreateTime { set; get; }
        public string ExtensionInfo { set; get; }
    }

    public class TpPendingBonusTransferModel
    {
        public int BonusId { get; set; }
        public int CustId { get; set; }
    }

    public class TpRemainingTransferBonusModel
    {
        public int BonusId { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public decimal Bonus { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal LockedAmount { get; set; }
        public int TpId { get; set; }
        public int PromotionType { get; set; }
        public decimal RemainingAmount { get; set; }
        public int PromoCategoryId { get; set; }
        public int TransferStatus { get; set; }
    }

    public class TpPendingBonusTransferInfoModel
    {
        public int TransferBonusId { get; set; }
        public string TransId { get; set; }
        public int TpId { get; set; }
        public int CustId { get; set; }
        public string UserName { get; set; }
        public int SiteId { get; set; }
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public decimal BonusAmount { get; set; }
        public decimal LockedAmount { get; set; }
        public decimal RolloverAmount { get; set; }
        public int ExpiredDate { get; set; }
        public string Memo { get; set; }
        public int Status { get; set; }
        public string CreateTime { get; set; }
        public string ThirdId { get; set; }
    }

    public class TpBonusTransfer
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public decimal Bonus { set; get; }
        public decimal BonusAmount { set; get; }
        public decimal LockedAmount { set; get; }
        public decimal RolloverAmount { set; get; }
        public long ExpiryTime { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }
    public class TpBonusCancelTransfer
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
        public int PromoCategoryId { set; get; }
        public int BonusId { set; get; }

        [Description("剩餘紅利: BonusAmount + LockedAmount + WinLossAmount")]
        public decimal RemainingBonus { set; get; }

        [Description("退還本金: RemainingBonus - BonusAmount")]
        public decimal RefundAmount { set; get; }

        public bool UpdateBonusTransferStatus { set; get; }
    }
    public class TpTransferUserList
    {
        public string TransId { set; get; }
        public int TpId { set; get; }
        public int CustId { set; get; }
        public decimal Amount { set; get; }
        public Direction direction { set; get; }
    }

    /// <summary>
    /// 餘額第三方客戶資料模型
    /// </summary>
    public class BalanceTpCustInfoModel
    {
        public int TpId { get; set; }
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public int CurrencyId { get; set; }
        public string UserName { get; set; }
        public string TpUserName { get; set; }
        public string TpPwd { get; set; }
        public string TpCurrencyId { get; set; }
        public decimal ExchangeRate { get; set; }
        public bool IsTest { get; set; }
        public int ActStatus { get; set; }
        public int Status { get; set; }
        public int CStatus { get; set; }
    }

    /// <summary>
    /// 接收第三方多筆餘額模型
    /// </summary>
    public class ReceiveTpUserBalanceModel
    {
        public ErrorCode ErrorCode { get; set; }
        public string Message { get; set; }
        public string TpUserName { get; set; }
        public decimal Balance { get; set; }
    }

    /// <summary>
    /// 第三方客戶餘額模型(for Insert)
    /// </summary>
    public class TpCustBalanceModel
    {
        public int TpId { get; set; }

        public int CustId { get; set; }

        public int SiteId { get; set; }

        public int CurrencyId { get; set; }

        public decimal Balance { get; set; }

        public string RequestTime { get; set; }

        public string ResponseTIme { get; set; }

        //public decimal bonus { get; set; }
        //public decimal outstanding { get; set; }
    }

    public class SetBetLimit
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public string BetSetting { get; set; }
    }

    public class AddFreeSpin
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
        public int SiteId { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }

    public class CreateBonusCode
    {
        public int TpId { set; get; }
        public int SiteId { set; get; }
        public Dictionary<string, string> inputs { set; get; }
    }
}
