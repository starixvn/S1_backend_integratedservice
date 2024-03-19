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
        public string token { get; set; }
    }
    public class WonCasinoDeduct : WonCasinoBaseRequest
    {
        public decimal amount { get; set; }
        public string serialnumber { get; set; }
        public int type { get; set; }
        public List<WonCasinoBetsData> bets { get; set; }
    }
    public class WonCasinoDeposit : WonCasinoBaseRequest
    {
        public decimal amount { get; set; }
        public string serialnumber { get; set; }
        public int type { get; set; }
        public List<WonCasinoBetsData> bets { get; set; }
    }
    public class WonCasinoRollBack : WonCasinoBaseRequest
    {
        public string appid { get; set; } // appid
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
        public decimal id { get; set; } // 注单 ID
        public string serialnumber { get; set; } // 注单编号
        public decimal userid { get; set; } // 用户 ID
        public string username { get; set; } // 用户名称
        public string gametype { get; set; } // 游戏类型
        public decimal gameid { get; set; } // 游戏 id
        public decimal gamerounno { get; set; } // 游戏局编号
        public string choosebankpoint { get; set; } // 三公选庄
        public decimal betpoint { get; set; } // 投注点
        public decimal betamount { get; set; } // 投注金额
        public decimal winlost { get; set; } // 输赢
        public decimal commamount { get; set; } // 洗码量
        public string commratio { get; set; } // 占成字段（主播 id可根据这个传如 ID10001_0.5
        public DateTime createtime { get; set; } // 创建时间
        public string state { get; set; } // 订单状态
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
        public string serialnumber { get; set; }
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
}