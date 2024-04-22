using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.Model
{
    #region Request
    public class WonCasinoBaseRequest
    {
        public string appid { get; set; }
        public string username { get; set; }
        public string notifyid { get; set; }
        public string sign { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }
    public class WonCasinoDeductRequest : WonCasinoBaseRequest
    {
        public decimal amount { get; set; }
        public string serialnumber { get; set; }
        public int type { get; set; }
        public string currency { get; set; }
        public WonCasinoBetsData bets { get; set; }
    }
    
    public class WonCasinoDepositRequest : WonCasinoBaseRequest
    {
        public decimal amount { get; set; }
        public string serialnumber { get; set; }
        public int type { get; set; }
        public string currency { get; set; }
        public WonCasinoBetsData bets { get; set; }
    }
    public class WonCasinoRollBackRequest : WonCasinoBaseRequest
    {
        public int type { get; set; } // type
        public decimal amount { get; set; } // amount
        public string serialnumber { get; set; } // serialnumber
        public string username { get; set; } // username
        public string notifyid { get; set; } // notifyid
        public string errmsg { get; set; } // errmsg
        public string sign { get; set; } // sign
        public string token { get; set; } // token
    }
    public class WonCasinoBetsData
    {
        public int gameid { get; set; }
        public long createtime { get; set; }
        public int serialnumber { get; set; }
        public int userstatus { get; set; }
        public string betpoint { get; set; }
        public decimal betodds { get; set; }
        public int userid { get; set; }
        public decimal commamount { get; set; }
        public int gameroundid { get; set; }
        public long settletime { get; set; }
        public string gameresult { get; set; }
        public decimal winlost { get; set; }
        public int gametype { get; set; }
        public int gamestyle { get; set; }
        public string currency { get; set; }
        public int id { get; set; }
        public long gameroundno { get; set; }
        public int state { get; set; }
        public long gameno { get; set; }
        public int bettype { get; set; }
        public int cid { get; set; }
        public decimal betamount { get; set; }
        public decimal foregift { get; set; }
    }
    #endregion
    
    #region Response

    public class WonCasinoBaseResponse
    {
        public int result { get; set; }
        public string desc { get; set; }
    }
    
    public class WonCasinoGetBalanceResponse : WonCasinoBaseResponse
    {
        public decimal balance { get; set; }
    }

    public class WonCasinoDeductResponse : WonCasinoBaseResponse
    {
        public decimal balance { get; set; }
        public string orderno { get; set; }
    }
    
    public class WonCasinoDepositResponse : WonCasinoBaseResponse
    {
        public decimal balance { get; set; }
        public string orderno { get; set; }
    }

    public class WonCasinoRollBackResponse : WonCasinoBaseResponse
    {
        public decimal balance { get; set; }
    }
    
    #endregion

    #region DAL

    public class BettransWonCasino : WonCasinoBaseRequest
    {
        public long SQ { get; set; }
        public long id { get; set; }
        public int type { get; set; }
        public decimal amount { get; set; }
        public decimal betamount { get; set; }
        public decimal winlost { get; set; }
        public string serialnumber { get; set; }
        public int state { get; set; }
        public long createtime { get; set; }
        public long? settletime { get; set; }
        public string currency { get; set; }
        public int gameid { get; set; }
        public int userid { get; set; }
        public byte userstatus { get; set; }
        public string betpoint { get; set; }
        public decimal betodds { get; set; }
        public decimal commamount { get; set; }
        public decimal foregift { get; set; }
        public int gameroundid { get; set; }
        public string gameresult { get; set; }
        public byte gametype { get; set; }
        public byte gamestyle { get; set; }
        public long gameroundno { get; set; }
        public long gameno { get; set; }
        public byte bettype { get; set; }
        public int cid { get; set; }
        public DateTime UTCBetTime { get; set; }
        public DateTime UTCSettleTime { get; set; }
        public DateTime UTCCreateTime { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public class RollbackWonCasino
    {
        public long id { get; set; }
        public string username { get; set; }
        public string notifyid { get; set; }
        public decimal amount { get; set; }
        public int type { get; set; }
        public string currency { get; set; }
        public string appid { get; set; }
        public string sign { get; set; }
        public string serialnumber { get; set; }
        public int gameid { get; set; }
        public int userstatus { get; set; }
        public string betpoint { get; set; }
        public decimal betodds { get; set; }
        public int userid { get; set; }
        public decimal commamount { get; set; }
        public int gameroundid { get; set; }
        public long createtime { get; set; }
        public long? settletime { get; set; }
        public string gameresult { get; set; }
        public decimal winlost { get; set; }
        public int gametype { get; set; }
        public int gamestyle { get; set; }
        public long gameroundno { get; set; }
        public int state { get; set; }
        public long gameno { get; set; }
        public int bettype { get; set; }
        public int cid { get; set; }
        public decimal betamount { get; set; }
        public decimal foregift { get; set; }
        public DateTime UTCBetTime { get; set; }
        public DateTime UTCSettleTime { get; set; }
        public int TicketType { get; set; }
        public int TicketStatus { get; set; }
        public int SettleFlag { get; set; }
    }

    #endregion


}