using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BaseResult
    {
        
    }
    public class ErrorResult : BaseResult
    {
        public int err { get; set; }
        public string errdesc { get; set; }
    }

    #region debit
    public class KMDebitRequest
    {
        public string testmode { get; set; }
        public bool transactional { get; set; }
        public List<KMDebitRequestTransaction> transactions { get; set; }
    }

    public class KMDebitRequestTransaction
    {
        [JsonProperty("userid")]
        public string UserId { get; set; }
        [JsonProperty("authtoken")]
        public string AuthToken { get; set; }
        [JsonProperty("brandcode")]
        public string BrandCode { get; set; }
        [JsonProperty("amt")]
        public decimal Amount { get; set; }
        [JsonProperty("cur")]
        public string Currency { get; set; }
        [JsonProperty("ipaddress")]
        public string IpAddress { get; set; }
        [JsonProperty("ptxid")]
        public string PtxId { get; set; }
        [JsonProperty("refptxid")]
        public string RefPtxId { get; set; }
        [JsonProperty("txtype")]
        public int TxType { get; set; }
        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }
        [JsonProperty("platformtype")]
        public int PlatformType { get; set; }
        [JsonProperty("gpcode")]
        public string GpCode { get; set; }
        [JsonProperty("gamecode")]
        public string GameCode { get; set; }
        [JsonProperty("gamename")]
        public string GameName { get; set; }
        [JsonProperty("gametype")]
        public int GameType { get; set; }
        [JsonProperty("externalgameid")]
        public string ExternalGameId { get; set; }
        [JsonProperty("roundid")]
        public string RoundId { get; set; }
        [JsonProperty("externalroundid")]
        public string ExternalRoundId { get; set; }
        [JsonProperty("betid")]
        public string BetId { get; set; }
        [JsonProperty("externalbetid")]
        public string Externalbetid { get; set; }
        [JsonProperty("senton")]
        public string Senton { get; set; }
        [JsonProperty("isclosinground")]
        public bool IsClosingRound { get; set; }
        [JsonProperty("ggr")]
        public decimal Ggr { get; set; }
        [JsonProperty("turnover")]
        public decimal Turnover { get; set; }
        [JsonProperty("walletcode")]
        public string WalletCode { get; set; }
        [JsonProperty("bonustype")]
        public int BonusType { get; set; }
        [JsonProperty("bonuscode")]
        public string BonusCode { get; set; }
        [JsonProperty("desc")]
        public string Desc { get; set; }
        [JsonProperty("commission")]
        public double? Commission { get; set; }
    }

    public class KMDebitResponse : BaseResult
    {
        public List<KMDebitResponseTransaction> transactions { get; set; }
    }

    public class KMDebitResponseTransaction
    {
        public string txid { get; set; }
        public string ptxid { get; set; }
        public decimal bal { get; set; }
        public string cur { get; set; }
        public bool dup { get; set; }
    }
    #endregion

    #region credit

    public class KMCreditRequest
    {
        public List<KMCreditRequestTransaction> transactions { get; set; }
    }
    
    public class KMCreditRequestTransaction
    {
        public string userid { get; set; }
        public string authtoken { get; set; }
        public string brandcode { get; set; }
        public decimal amt { get; set; }
        public string cur { get; set; }
        public string ipaddress { get; set; }
        public string ptxid { get; set; }
        public string refptxid { get; set; }
        public int txtype { get; set; }
        public string timestamp { get; set; }
        public int platformtype { get; set; }
        public string gpcode { get; set; }
        public string gamecode { get; set; }
        public string gamename { get; set; }
        public int gametype { get; set; }
        public string externalgameid { get; set; }
        public string roundid { get; set; }
        public string externalroundid { get; set; }
        public string betid { get; set; }
        public string senton { get; set; }
        public bool isclosinground { get; set; }
        public decimal ggr { get; set; }
        public decimal turnover { get; set; }
        public int bonustype { get; set; }
        public string bonuscode { get; set; }
        public string desc { get; set; }
        public decimal unsettledbets { get; set; }
        public string walletcode { get; set; }
        public string commission { get; set; }
    }
    
    public class KMCreditResponse: BaseResult
    {
        public List<KMCreditBaseTransaction> transactions { get; set; }
    }
    public class KMCreditResponseTransaction: KMCreditBaseTransaction
    {
        public decimal bal { get; set; }
        public string cur { get; set; }
        public bool dup { get; set; }
    }

    public class KMCreditErrorTransaction: KMCreditBaseTransaction
    {
        public string txid { get; set; }
        public string ptxid { get; set; }
        public int err { get; set; }
        public string errdesc { get; set; }
    }

    public class KMCreditBaseTransaction
    {
        public string txid { get; set; }
        public string ptxid { get; set; }
    }

    #endregion

    #region balance
    public class KMBalanceRequest
    {
        public string testmode { get; set; }
        public List<KMBalanceRequestUser> users { get; set; }
    }
    
    public class KMBalanceRequestUser
    {
        [JsonProperty("authtoken")]
        public string AuthToken { get; set; }
        
        [JsonProperty("userid")]
        public string UserId { get; set; }
        
        [JsonProperty("lang")]
        public string Lang { get; set; }
        
        [JsonProperty("brandcode")]
        public string BrandCode { get; set; }
        
        [JsonProperty("cur")]
        public string Cur { get; set; }
        
        [JsonProperty("walletcode")]
        public object WalletCode { get; set; }
    }

    public class KMBalanceResponse : BaseResult
    {
        public List<KMUsers> users { get; set; }
    }

    public class KMUsers
    {
        public string userid { get; set; }
        public List<UserWallet> wallets { get; set; }
    }

    public class UserWallet
    {
        public string code { get; set; }
        public decimal bal { get; set; }
        public string cur { get; set; }
    }
    #endregion
    
    public class KmDbResult
    {
        public int SPResult { get; set; }

        public long Trx_id { get; set; }

        public decimal AfterAmount { get; set; }

        public decimal ReturnAmount { get; set; }
    }
}