using System.Collections.Generic;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingData_KM
    {
        
    }

    public class BaseResult
    {
        
    }
    public class ErrorResult : BaseResult
    {
        public int err { get; set; }
        public string errdesc { get; set; }
    }
    
    #region token
    public class KMTokenRequest
    {
        public string client_id { get; set; }
        public string client_secret { get; set; }
        public string grant_type { get; set; }
        public string scope { get; set; }
    }

    public class KMTokenResponse
    {
        public string access_token { get; set; }
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string scope { get; set; }
    }
    #endregion

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
        [JsonProperty("unsettledbets")]
        public decimal UnSettledBets { get; set; }
        [JsonProperty("walletcode")]
        public string WalletCode { get; set; }
        [JsonProperty("desc")]
        public string Desc { get; set; }
        [JsonProperty("commission")]
        public double Commission { get; set; }
    }

    public class KMDebitResponse
    {
        public List<KMDebitTransaction> transactions { get; set; }
    }

    public class KMDebitTransaction
    {
        public string txid { get; set; }
        public string ptxid { get; set; }
        public decimal bal { get; set; }
        public string cur { get; set; }
        public bool dup { get; set; }
    }
    #endregion
    

    public class KMCreditRequest
    {
        
    }

    public class KMCreditResponse
    {
        
    }

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
    
}