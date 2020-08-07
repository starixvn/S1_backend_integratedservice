using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ThirdPartyWager.Service.Model
{
    public class ThirdPartyModel
    {
        public int Id { set; get; }
        public string TpName { set; get; }
        public string Setting { set; get; }
        public int Status { set; get; }
    }

    public class Setting
    {
        public string APIkey { get; set; }
        public string ApiUrl { get; set; }
        public string WalletUrl { get; set; }
        public string AgentAccount { get; set; }

    }
}
