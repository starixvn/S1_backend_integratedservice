using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
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
        /// 用戶是否申請rebate
        /// </summary>
        public bool ApplyRebate { get; set; }
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
        
        public int AgentId
        {
            get
            {
                var result = _agentIdList.TryGetValue(CurrencyId , out var agent);
                if (!result)
                {
                    throw new KeyNotFoundException();
                }
                return agent;
            }
        }
        
        public string AllAgentId => JsonConvert.SerializeObject(new List<int> {AgentId});
        
        [Range(0,2)]
        public int IsKYC { get; set; }
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

        #region CustBank

        public List<BankDetail> CustBankDetail { get; set; } = new();
        public List<EWalletDetail> CustEWalletDetail { get; set; } = new();
        public List<CryptoWalletDetail> CustCryptoWalletDetail { get; set; } = new();

        public class BankDetail
        {
            public int SysId { get; set; }
            public string AccountNo { get; set; }
            public string IFSCCode { get; set; }
        }
        public class EWalletDetail
        {
            public int SysId { get; set; }
            public string AccountNo { get; set; }
        }
        public class CryptoWalletDetail
        {
            public int CurrencyId { get; set; }
            public string Network { get; set; }
            public string Address { get; set; }
            public string NickName { get; set; }
        }
        
        #endregion

        #region Store

        public int RewardPoint { get; set; }

        #endregion
        
        public CustActiveMonthsLevel CustActiveMonthsLevel { get; set; }
        
        #region private

        private readonly Dictionary<int, int> _agentIdList = new()
        {
            { 2, 1 },
            { 3, 2 },
            { 4, 3 },
            { 13, 4 },
            { 15, 5 },
            { 45, 6 },
            { 51, 7 },
            { 61, 8 },
            { 72, 9 },
            { 74, 10 }
        };

        #endregion
    }

    public class MigrationCustomerResponse
    {
        public int CustId { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }

    public class MigrationBalanceRequest
    {
        public int SiteId { get; set; }
        public int CustId { get; set; }
        /// <summary>
        /// 錢包餘額
        /// </summary>
        public decimal Balance { get; set; }
        /// <summary>
        /// 提款限額
        /// </summary>
        public decimal WithdrawalLimit { get; set; }
    }
    
    public class MigrationCustomerBalanceResponse
    {
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

    public class CustActiveMonthsLevel
    {
        public int ActiveMonthsLevel { get; set; }
        public int ActiveMonths { get; set; }
        public int Consecutive { get; set; }
        public int PreActiveMonths { get; set; }
        public int PreConsecutive { get; set; }
        public int MinActiveMonths { get; set; }
        public decimal SpecialRate { get; set; }
        public DateTime JoinDate { get; set; }
    }
    
    public class MigrationAffiliateResponse
    {
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }

}