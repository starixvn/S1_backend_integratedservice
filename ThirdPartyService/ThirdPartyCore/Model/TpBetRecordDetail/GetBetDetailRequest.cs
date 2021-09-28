using System;
using System.Collections.Generic;
using System.Text;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore.Model
{
    public class GetBetDetailRequest
    {
        public int GameType { get; set; }
        public string TransId { get; set; }
        public int CustId { get; set; }
        public DateTime SettledTime { get; set; }
    }
}
