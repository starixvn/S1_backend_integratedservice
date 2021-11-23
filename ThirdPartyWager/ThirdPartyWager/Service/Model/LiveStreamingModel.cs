using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class 
        GetLiveStreaming
    {
        public IEnumerable<LiveStreamingJson> Match { get; set; }

    }
    public class LiveStreamingJson
    {
        public string MatchID { get; set; }
        public string Channel { get; set; }
        public string TimeStart { get; set; }
        public string UTCTimeStart { get; set; }
        public string TimeStop { get; set; }
        public string UTCTimeStop { get; set; }
        public string Name { get; set; }
        public string Home { get; set; }
        public string Away { get; set; }
        public string HomeCH { get; set; }
        public string AwayCH { get; set; }
        public string HomeTH { get; set; }
        public string AwayTH { get; set; }
        public string Type { get; set; }
        public string League { get; set; }
        public string LeagueCH { get; set; }
        public string LeagueTH { get; set; }
        public int NowPlaying { get; set; }
        public string IsLive { get; set; }
        public string State { get; set; }
        public string bid { get; set; }
        public string HomeScore { get; set; }
        public string AwayScore { get; set; }
    }

}
