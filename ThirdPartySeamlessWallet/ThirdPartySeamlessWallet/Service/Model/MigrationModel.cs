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
        public bool EmailVerified { get; set; }
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
        public DateTime FirstDepositTime { get; set; }
        
        #endregion
        
    }

    public class MigrationCustomerResponse
    {
        public int CustId { get; set; }
        public string Message { get; set; }
        public int ErrorCode { get; set; }
    }
    

}