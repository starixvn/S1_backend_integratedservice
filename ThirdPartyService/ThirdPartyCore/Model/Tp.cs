using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
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
        public decimal Amount { set; get; }
        public Direction direction { set; get; }
        public Dictionary<string, string> inputs { set; get; }
        public int BonusId { set; get; }
    }
    public class AdminTpTransfer : TpTransfer
    {
        public string Operator { set; get; }
        public string Remarks { set; get; }
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
        public decimal ExchangeRate { get; set; }
        public bool IsTest { get; set; }
        public int ActStatus { get; set; }
        public int Status { get; set; }
        public int CStatus { get; set; }
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
        
        [Description("BonusAmount + LockedAmount + WinLossAmount")]
        public decimal RemainingBonus { set; get; }
        
        [Description("RemainingBonus - BonusAmount")]
        public decimal RefundAmount { set; get; }
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
    public class GetBetLimit
    {
        public int TpId { set; get; }
        public int CustId { set; get; }
    }

    public class BetSettingLimit
    {
        public int error_code { get; set; }
        public string message { get; set; }
        public List<BetSetting> Data { get; set; }
    }

    public class BetSetting
    {
        public string sport_type { get; set; }
        public decimal min_bet { get; set; }
        public decimal max_bet { get; set; }
        public decimal max_bet_per_match { get; set; }
    }
}
