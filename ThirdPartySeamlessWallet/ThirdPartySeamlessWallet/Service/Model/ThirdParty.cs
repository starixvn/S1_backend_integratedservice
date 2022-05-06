using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class ThirdPartySetting
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> TpSetting => JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
        public int Status { set; get; }
        public int WalletType { set; get; }
    }
    public class TpCurrency
    {
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }
    public class BonusCategory
    {
        public int Type { set; get; }
        public int Id { set; get; }
        public int TpId { set; get; }
        public int WalletType { set; get; }
    }
    public class BonusMinBet
    {
        public int GameType { set; get; }
        public int CurrencyId { set; get; }
        public decimal MinBet { set; get; }
    }
    public class TpGameCasino
    {
        public int TpId { set; get; }
        public int CategoryId { set; get; }
        public int TypeId { set; get; }
        public string GameId { set; get; }
    }
    public class TpBetFailureRecord
    {
        public string TransId { set; get; }
        public int TpId { set; get; }
        public int TpGameType { set; get; }
        public int SiteId { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int Code { set; get; }
        public string Info { set; get; }
        public int Status { set; get; }
        public long TransTime { set; get; }
    }
}
