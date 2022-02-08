using System;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class BettingDataRNG_HB
    {
        
    }
    
    #region Auth
    public class HBPlayerDetailRequest
    {
        public string type { get; set; }
        public DateTime dtsent { get; set; }
        public Basegame basegame { get; set; }
        public Auth auth { get; set; }
        public Playerdetailrequest playerdetailrequest { get; set; }
    }
    
    public class Basegame
    {
        public string brandgameid { get; set; }
        public string keyname { get; set; }
    }

    public class Auth
    {
        public string username { get; set; }
        public string passkey { get; set; }
        public string machinename { get; set; }
        public string locale { get; set; }
        public string brandid { get; set; }
    }

    public class Playerdetailrequest
    {
        public string token { get; set; }
        public bool gamelaunch { get; set; }
    }

    public class HBPlayerDetailResponse
    {
        public playerdetailresponse playerdetailresponse { get; set; }
    }
    
    public class HBStatus
    {
        public bool success { get; set; }
        public bool autherror { get; set; }
        public string message { get; set; }
    }

    public class playerdetailresponse
    {
        public HBStatus Status { get; set; }
        public string accountid { get; set; }
        public string accountname { get; set; }
        public decimal balance { get; set; }
        public string currencycode { get; set; }
    }
    
    #endregion
}