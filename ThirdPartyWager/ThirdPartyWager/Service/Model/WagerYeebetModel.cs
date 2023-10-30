namespace ThirdPartyWager.Service.Model;

public class WagerYeebet_GetbetsResponseModel
{
    public int result { get; set; }
    public string desc { get; set; }
    public int total { get; set; }
    public int arraysize { get; set; }
    public IEnumerable<WagerYeebetModel> array { get; set; }
}

public class WagerYeebet_RemovebetsResponseModel
{
    public int result { get; set; }
    public int errid { get; set; }
    public string desc { get; set; }
}

public class WagerYeebetModel
{
    public int id { get; set; }
    public string uid { get; set; } = "";
    public int userid { get; set; }
    public string username { get; set; } = "";
    public int userstatus { get; set; }
    public string currency { get; set; } = "";
    public int serialnumber { get; set; } = 0;
    public int gametype { get; set; } = 0;
    public int gameid { get; set; } = 0;
    public long gameno { get; set; } = 0;
    public string gameroundid { get; set; } = "";
    public string gameresult { get; set; } = "";
    public int bettype { get; set; } = 0;
    public decimal betamount { get; set; } = 0;
    public decimal commamount { get; set; } = 0;
    public string betpoint { get; set; } = "";
    public decimal betodds { get; set; } = 0;
    public decimal winlost { get; set; } = 0;
    public int state { get; set; }
    public string describe { get; set; } = "";
    public string cid { get; set; } = "";
    public string createtime { get; set; } = "";
    public string settletime { get; set; } = "";
}
