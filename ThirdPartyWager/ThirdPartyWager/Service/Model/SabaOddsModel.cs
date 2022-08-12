using System.Collections.Generic;

namespace ThirdPartyWager.Service.Model
{
    public class GetSabaOddsJson
    {
        public string outrights { get; set; }
        public GetMatches Matches { get; set; }
    }
    public class GetMatches
    {
        public string lastupdate { get; set; }
        public string timestamp { get; set; }
        public string oddstype { get; set; }
        public List<GetMatch> match { get; set; }
    }

    public class GetMatch
    {
        public int id { get; set; }
        public string ge { get; set; }
        public string gt { get; set; }
        public string gs { get; set; }
        public string gb { get; set; }
        public string ga { get; set; }
        public string gk { get; set; }
        public string gv { get; set; }
        public string gj { get; set; }
        public string he { get; set; }
        public string ht { get; set; }
        public string hs { get; set; }
        public string hb { get; set; }
        public string ha { get; set; }
        public string hk { get; set; }
        public string hv { get; set; }
        public string hj { get; set; }
        public string ae { get; set; }
        public string at { get; set; }
        public string As { get; set; }
        public string ab { get; set; }
        public string aa { get; set; }
        public string ak { get; set; }
        public string av { get; set; }
        public string aj { get; set; }
        public string mt { get; set; }
        public string sport { get; set; }
        public string running { get; set; }
        public GetBookie bookie { get; set; }
    }
    public class GetBookie
    {
        public string name { get; set; }
        public string simp { get; set; }
        public string trad { get; set; }
        public List<GetOdd> odd { get; set; }
    }
    public class GetOdd
    {
        public string t { get; set; }
        public string h { get; set; }
        public string o { get; set; }
        public string u { get; set; }
        public string time { get; set; }
        public string timestamp { get; set; }
    }
    public class UpdateOdd
    {
        public int _MatchId { get; set; }
        public string _Handicap { get; set; }
        public string _HomeOdds { get; set; }
        public string _AwayOdds { get; set; }
    }
}
