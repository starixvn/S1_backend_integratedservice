using System;

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
}
