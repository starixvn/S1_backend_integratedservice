using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Model
{
    public class ThirdPartyModel
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public int Status { set; get; }
    }

    public class ThirdPartyTimerModel
    {
        public int TpId { set; get; }
        public int Type { set; get; }
        public DateTime StartTime { set; get; }
    }

    public class TpWagerTimerReferenceModel
    {
        public int TpId { set; get; }
        public int Type { set; get; }
        public string Currency { set; get; }
        public string Reference { set; get; }
        public int GameType { set; get; }
        public DateTime StartTime { set; get; }
    }

    public class Setting
    {
        public string APIkey { get; set; }
        public string ApiUrl { get; set; }
        public string WalletUrl { get; set; }
        public string WagerUrl { get; set; }
        public string AgentAccount { get; set; }

    }

    public class TpGameCasino
    {
        public int TpId { set; get; }
        public int CategoryId { set; get; }
        public int TypeId { set; get; }
        public string GameId { set; get; }
    }

    public class TpListModel
    {
        public int SiteId { set; get; }
        public int TpId { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public int Status { set; get; }
        public long TicketKey { set; get; }
        public Dictionary<string, string> SettingDic
        {
            get
            {
                if (Setting != null)
                {
                    return JsonConvert.DeserializeObject<Dictionary<string, string>>(Setting);
                }
                return default;
            }
        }
    }
}
