using Newtonsoft.Json;
using ThirdPartyCore.Model;

namespace ThirdParty.PP
{
    public class PPConfig : TpSiteConfig
    {
        public string SecureKey { get; set; }
        public string FunGameUrl { get; set; }
        public string ProviderId { get; set; }
        public string SecureLogin { get; set; }
        public string ApiUrl_Stream { get; set; }
        public string SecurePassword { get; set; }
        public string SecureKey_Stream { get; set; }
        public string SecureLogin_Stream { get; set; }
    }

    public class UserReq
    {
        public string username { get; set; }
        public string password { get; set; }
        public string providerId { get; set; }
    }
    public class gameReq
    {
        public string gameID { get; set; }
        public string gameName { get; set; }
        public string gameTypeID { get; set; }
        public string typeDescription { get; set; }
        public string technology { get; set; }
        public string platform { get; set; }
        public bool demoGameAvailable { get; set; }
        public string aspectRatio { get; set; }
        public string technologyID { get; set; }
        public int gameIdNumeric { get; set; }
        public List<string> jurisdictions { get; set; }
    }
    public class LoginReq
    {
        public string token { get; set; } //由玩家的运营商生成的安全令牌
        public string symbol { get; set; } //游戏的唯一标识符，如 vs20bl、vs15ad 等
        public string technology { get; set; } // H5 或 F（用于 WEB 平台）或 H5（用于 MOBILE 平台）
        public string platform { get; set; } //WEB（用于桌面设备）或 MOBILE（用于移动设备）
        public string language { get; set; } //ISO 639 - 1 标准的玩家语言（如 en、fr、it）
        public string cashierUrl { get; set; } //用于打开运营商网站收银台页面的 URL；
        public string lobbyUrl { get; set; } //用于打开运营商网站大厅页面的 URL；
        public string operatorGameHistoryUrl { get; set; }  //用于在运营商侧打开游戏历史记录页面的 URL
        //public string secureLogin { get; set; }  //运营商在 Pragmatic Play 侧的唯一标识符。
    }

    public class LoginResult
    {
        public int error { get; set; }
        public string description { get; set; }
        public string gameURL { get; set; }
    }

    public class UserResult
    {
        public string userId { get; set; }
        public string currency { get; set; }
        public decimal cash { get; set; }
        public decimal bonus { get; set; }
        public int error { get; set; }
        public string description { get; set; }
        //"userId":"421", "currency":"USD", "cash": 99999.99, "bonus": 99.99, "error":0, "description":"Success"
    }

    public class AddFreeSpinResult
    {
        public int error { get; set; }
        public string description { get; set; }
    }

    public class CreateBonusCodeResult
    {
        public int error { get; set; }
        public string description { get; set; }
    }

    public class CreateFRB
    {
        public List<GameList> gameList = new();
    }

    public class GameList
    {
        public string gameId { get; set; }
        public List<BetValues> betValues { get; set; }
    }

    public class BetValues
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal betPerLine { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public decimal totalBet { get; set; }
        public string currency { get; set; }
    }
}