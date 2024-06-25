using System.ComponentModel;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BalanceData_SimplePlay
    {
        public string username { get; set; }
        public string currency { get; set; }
    }

    public class CommonData_SimplePlay
    {
        public string username { get; set; }
        public string currency { get; set; }
        public decimal amount { get; set; }
        public string txnid { get; set; }
        public DateTime timestamp { get; set; }
        public string gametype { get; set; }
        public string gamecode { get; set; }
        public string gameid { get; set; }
        public string TransId
        {
            get
            {
                return $"{gameid}_{username}";
            }
        }
    }

    public class BettingData_SimplePlay : CommonData_SimplePlay
    {
        public string ip { get; set; }
        public int platform { get; set; }
        public decimal JackpotContribution { get; set; }
    }

    public class PayoutData_SimplePlay : CommonData_SimplePlay
    {

        public DateTime Payouttime { get; set; }
        public bool JackpotWin { get; set; }
        public int PayoutType { get; set; }
    }


    public class CancelBetData_SimplePlay : CommonData_SimplePlay
    {
        public string txn_reverse_id { get; set; }
    }

    public class SettleData_SimplePlay : SettleData
    {
        [Description("游戏代码")]
        public string GameCode { get; set; }
    }

    public class SimplePlayConfig : TpSiteConfig
    {
        public string Lobby { get; set; }
        public string MD5Key { get; set; }
        public string EncryptKey { get; set; }
        public string SecretKey { get; set; }
    }
}
