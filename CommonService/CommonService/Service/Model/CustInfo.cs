namespace CommonService.Service.Model
{
    public class CustInfoProfile
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public int CurrencyId { get; set; }
        public string IsoCode { get; set; }
        public int UserLevel { get; set; }
        public string Agent { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public bool IsTest { get; set; }
        public string SiteName { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public string Email { get; set; }
        //public string AccountName { get; set; }
        public string CountryCode { get; set; }
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
        public string Birth_d { get; set; }
        public string Birth_m { get; set; }
        public string Birth_y { get; set; }
        public string RealName { get; set; }
        public bool PhoneVerified { get; set; }
        public bool EmailVerified { get; set; }
        public string TpUserName { get; set; }

        /// <summary>
        /// 回傳區碼加手機號碼，手機號碼會移除第一碼的0，區碼會移除第一碼+號： ex:886912345678
        /// </summary>
        public string GetPhoneNo()
        {
            var phoneNo = PhoneNo.StartsWith("0") ? PhoneNo.Substring(1) : PhoneNo;
            var areaCode = AreaCode.StartsWith("+") ? AreaCode.Substring(1) : AreaCode;
            return $"{areaCode}{phoneNo}";
        }
    }
}
