using System.ComponentModel.DataAnnotations;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public string TpUserName { get; set; }
        public string TpPwd { set; get; }
        public int SiteId { set; get; }
        public string TpCustId { get; set; }
        public string TpCurrencyId { get; set; }
        public int MemberType { get; set; }
        public bool IsTest { get; set; }
    }
    public class TpCustInfoAuth
    {
        public int TpId { get; set; }
        public string TpPwd { get; set; }
        public string TpLangId { get; set; }
        public string ClientIP { get; set; }
        public int CustId { get; set; }
        public bool IsTest { get; set; }
        public int SiteId { get; set; }
        public int Status { get; set; }
        public int CStatus { get; set; }
        public string UserName { get; set; }
        public int ActStatus { get; set; }
        public string TpUserName { get; set; }
        public decimal ExchangeRate { get; set; }
        public string TpCurrencyId { get; set; }
        public int MemberType { get; set; }
        public string GameId { get; set; }
    }
    public class TpCustBalance
    {
        public decimal Balance { get; set; }
        public decimal ExchangeRate { get; set; }
        public string TpCurrencyId { get; set; }
    }
    public class TpCheck
    {
        public int CustStatus { get; set; }
        public int TpStatus { get; set; }
        public int TpCustStatus { get; set; }
        public string IP { get; set; }
        public string Token { get; set; }
        public bool Locked => CustStatus != 1 || TpStatus != 1 || TpCustStatus != 1;
    }
    
    public class Customer
    {
        public int CustId { get; set; }
        public int SiteId { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public int CurrencyId { get; set; }
        public string UserLang { get; set; }
        public int ActStatus { get; set; }
        public int AgentId { get; set; }
        public string AllAgentId { get; set; }
        public int UserLevel { get; set; }
        public bool IsTest { get; set; }
        public DateTime LastLoginTime { get; set; }
        public DateTime RegTime { get; set; }
        public int Status { get; set; }
        public bool IsResetPwd { get; set; }
        public string Aff { get; set; }
        public string AffCode { get; set; }
    }

    public class CustBonus
    {
        public int BonusId { get; set; }
        public decimal Bonus { get; set; }
        public int PromoCategoryId { get; set; }
    }

    public class CustProfile 
    { 
        public int CustId { get; set; }
        public string Birth_d { get; set; }
        public string Birth_m { get; set; }
        public string Birth_y { get; set; }
        //public string CurrentAddress { get; set; }
        public Occupation Occupation { get; set; }
        public SourceOfIncome SourceOfIncome { get; set; }
        public string Email { get; set; }
        public string AccountName { get; set; }
        public string UserName { get; set; }
        public CustStatus Status { get; set; }
        public DateTime RegTime { get; set; }
        public string AreaCode { get; set; }
        public string PhoneNo { get; set; }
        public string SignupIp { get; set; }
        public int CurrencyId { get; set; }
        public string Nationality { get; set; }
        public string Country { get; set; }
        public string City { get; set; }
        public string PostCode { get; set; }
        public string LineFirst { get; set; }
    }
    public class CustInfo
    {
        public int IBCCustID { get; set; }
        public string RegisteredName { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public bool Active { get; set; }
        public bool Locked { get; set; }
        public DateTime DateOfRegistration { get; set; }
        public string RegistrationIP { get; set; }
        public string DateOfBirth { get; set; }
        public string CurrentAddress { get; set; }
        public string Nationality { get; set; }
        public string Occupation { get; set; }
    }
}
