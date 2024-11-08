using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class MigrationConfig 
    {
        public string Key { get; set; }
    }
    
    public class MigrationCustomerRequest
    {
        public int SiteId { get; set; }

        #region Customer

        /// <summary>
        /// UserName aka login userName
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// Real Name
        /// </summary>
        public string AccountName{ get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        /// <summary>
        /// en,cs,th etc
        /// </summary>
        public string UserLang { get; set; }
        /// <summary>
        /// UTC +0
        /// </summary>
        public DateTime RegTime { get; set; }
        /// <summary>
        /// 1: Running, 2: Suspend, 3: Closed, 4:Lock
        /// </summary>
        public int Status { get; set; }

        #endregion

        #region CustProfile

        public string Email { get; set; }
        /// <summary>
        /// {"Email": "jerrymyrtest004@gmail.com", "Birth_d": "10", "Birth_m": "1", "Birth_y": "1990", "PhoneNo": "154287777", "AreaCode": "+60", "AccountName": "jerry"}
        /// AccountName  = Real Name ,  必填 ex: {"AccountName": "jerry"}
        /// </summary>
        public string Profile { get; set; }
        public string PhoneNo { get; set; }
        public string AreaCode { get; set; }
        public bool PhoneVerified { get; set; }
        public DateTime? PhoneVerifiedTime { get; set; }
        public bool EmailVerified { get; set; }
        public DateTime? EmailVerifiedTime { get; set; }
        /// <summary>
        /// {"City":"Ulu tiram","State":"Johor","Country":"Malaysia","PostCode":"81800","LineFirst":"52","LineSecond":"Jalan pelangi indah"}
        /// 非必填
        /// </summary>
        public string Address { get; set; }
        
        #endregion

        #region CustRegInfo

        /// <summary>
        /// where member sign up from
        /// </summary>
        public string SignupUrl { get; set; }
        /// <summary>
        /// where referral come from
        /// </summary>
        public string ReferralUrl { get; set; }
        public DeviceId DeviceType { get; set; }
        public string SignupIp { get; set; }
        
        #endregion

        #region CustWallet

        /// <summary>
        /// 錢包餘額
        /// </summary>
        public decimal Balance { get; set; }
        
        /// <summary>
        /// 提款限額
        /// </summary>
        public decimal WithdrawalLimit { get; set; }
        /// <summary>
        /// UTC +0
        /// </summary>
        public DateTime? FirstDepositTime { get; set; }
        
        #endregion

        #region CustTransSummary
        
        /// <summary>
        /// 歷史存款總額
        /// </summary>
        public decimal DepositSum { get; set; }
        /// <summary>
        /// 存款筆數
        /// </summary>
        public int DepositCnt { get; set; }
        public decimal WithdrawlSum { get; set; }
        public int WithdrawlCnt { get; set; }
        /// <summary>
        /// 歷史有效投注總額
        /// </summary>
        public decimal EffectiveBetSum { get; set; }

        #endregion

        #region CustVip
        
        /// <summary>
        /// ex : 1.Black Tortois  2.Phoenix  3.Tiger
        /// </summary>
        public int VipLevel { get; set; }

        #endregion

        #region TpCustInfo

        /// <summary>
        /// 建立在Saba的用戶名稱
        /// </summary>
        public string SabaId { get; set; }

        #endregion

    }

    public class MigrationCustomerResponse
    {
        public int CustId { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }

    public class MigrationAffiliateRequest
    {
        public int SiteId { get; set; }
        /// <summary>
        /// AffiliateCode 屬於誰的CustId
        /// </summary>
        public int CustId { get; set; }
        public string AffiliateCode { get; set; }
        public string AffiliateName { get; set; }
        public string ParentAffiliate { get; set; }
        public List<int> AffiliatedCustIdList { get; set; }
    }
    
    public class AffiliateCustList
    {
        public int CustId { get; set; }
    }

    
    public class MigrationAffiliateResponse
    {
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }

}