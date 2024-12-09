namespace ThirdPartyCore.Model
{
    public class GameCasinoOption
    {
        public int TpId { get; set; }
        public string GameId { get; set; }
        public string AppId { get; set; }
        public string Name { get; set; }
    }

    public class GameNameInfo
    {
        public int TpId { get; set; }

        public string GameId { get; set; }

        public string BetType { get; set; }

        public string Name { get; set; }

        public string Lang { get; set; }
    }
}
