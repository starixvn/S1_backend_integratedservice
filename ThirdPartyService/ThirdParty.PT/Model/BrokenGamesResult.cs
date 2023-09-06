using System;

namespace ThirdParty.PT
{
    public partial class TpHelper
    {
        public class BrokenGamesResult
        {
            public string PLAYERNAME { get; set; }
            public DateTime GAMEDATE { get; set; }
            public string CLIENTTYPE { get; set; }
            public string PLATFORM { get; set; }
            public string GAMENAME { get; set; }
            public string SHORTNAME { get; set; }
            public decimal BET { get; set; }
            public string REMOTEIP { get; set; }
            public string BROKENGAMETYPE { get; set; }
            public string BROKENGAMECODE { get; set; }
            public string STATUS { get; set; }
            public string FINISHEDGAMECODE { get; set; }
            public DateTime FINISHEDGAMEDATE { get; set; }

        }
    }
}