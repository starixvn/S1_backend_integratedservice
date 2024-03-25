using System.Text.Json.Serialization;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class ThirdPartySetting
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public Dictionary<string, string> TpSetting => JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
        public int Status { set; get; }
        public WalletType WalletType { set; get; }
    }
    public class TpCurrency
    {
        public string TpCurrencyId { set; get; }
        public decimal ExchangeRate { set; get; }
    }
    public class BonusCategory
    {
        /// <summary>
        /// 遊戲類型: TpGameType.Type
        /// </summary>
        public int Type { set; get; }
        /// <summary>
        /// 遊戲分類: TpGameType.CategoryId
        /// </summary>
        public int CategoryId { set; get; }
        /// <summary>
        /// Promotion分類: PromotionCategory.Id = TpGameType.PromoCategoryId
        /// </summary>
        public int Id { set; get; }
        public int TpId { set; get; }
        /// <summary>
        /// 錢包類型
        /// </summary>
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
        public string AppId { set; get; }
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

    public class TpCountryCode
    {
        public string CountryCode { set; get; }
        public int CurrencyId { set; get; }
        public string TpCurrencyId { set; get; }
    }

    public class FinancialLog
    {
        [System.Text.Json.Serialization.JsonIgnore]
        public int Type { set; get; }

        [JsonPropertyName("TransId")]
        public string RefTransId { set; get; }

        [JsonPropertyName("Amount")]
        public decimal ChangeAmount { set; get; }

        [JsonPropertyName("Time")]
        public DateTime CreateTime { set; get; }

        [JsonPropertyName("FinancialType")]
        public FinancialType FinancialType
        {
            get
            {
                if (Type == (int)FinancialType.Deposit || Type.IsBetween(101, 1000))
                {
                    return FinancialType.Deposit;
                }

                if (Type.IsBetween(1001, 2000))
                {
                    return FinancialType.Withdrawal;
                }

                return FinancialType.Others;
            }
        }

        [JsonPropertyName("TypeName")]
        public string TypeName
        {
            get
            {
                return FinancialType.GetEnumDescription();
            }
        }
    }

    public class BonusLog
    {
        public long Id { set; get; }
        public int BonusId { set; get; }
        public int CustId { set; get; }
        public string UserName { set; get; }
        public int SiteId { set; get; }
        public int Type { set; get; }
        public int WalletType { set; get; }
        public int PromoCategoryId { set; get; }
        public int Direction { set; get; }
        public decimal ChangeBonus { set; get; }
        public decimal BeforeBonus { set; get; }
        public decimal AfterBonus { set; get; }
        public string Memo { set; get; }
        public string RefTransId { set; get; }
        public DateTime CreateTime { set; get; }
    }

    public class ServerInfo
    {
        public int SiteId { set; get; }
        public string SiteName { set; get; }
    }
}
