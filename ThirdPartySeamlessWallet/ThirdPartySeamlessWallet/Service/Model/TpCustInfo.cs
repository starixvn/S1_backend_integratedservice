using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class TpCustInfo
    {
        public int CustId { set; get; }
        public int CurrencyId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
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
}
