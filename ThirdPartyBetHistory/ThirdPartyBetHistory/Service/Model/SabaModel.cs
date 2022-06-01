using System;

namespace ThirdPartyBetHistory.Service.Model
{
    public class UnSettle
    {
        public string TransId { get; set; }
        public int GameType { get; set; }
        public DateTime SettledTime { get; set; }
    }
}