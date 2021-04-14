namespace ThirdPartyWager.Service.Model
{
    public class LatestHighlightVideoJson
    {
        public bool fVN { get; set; }
        public string date { get; set; }
        public string m3u8 { get; set; }
        public string poster { get; set; }
        public string share { get; set; }
        public Langlist title { get; set; }
        public int? LeagueId { get; set; }
        public int? HomeId { get; set; }
        public int? AwayId { get; set; }
    }

    public class Langlist
    {
        public string cs { get; set; }
        public string en { get; set; }
    }
    public class LatestHighlightVideoData
    {
        public int SiteId { get; set; }
        public string LangId { get; set; }
        public string Date { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string Time { get; set; }
        public string Operator { get; set; }
    }

}
