namespace ThirdParty.PNG
{
    internal class PNGConfig
    {
        public string RootUrl { get; set; }

        public string Password { get; set; }

        public string UserName { get; set; }

        public string AccessToken { get; set; }

        public string GameLaunchUrl { get; set; }

        public string GameHistoryMobile { get; set; }

        public string GameHistoryDesktop { get; set; }

        public int Pid { get; set; }

        public string GameId { get; set; }

        public PNGConfig(Dictionary<string, string> inputs)
        {
            RootUrl = inputs.ContainsKey("RootUrl") && !string.IsNullOrEmpty(inputs["RootUrl"])
                ? inputs["RootUrl"]
                : throw new Exception("PlayNGo TpList not found RootUrl Setting");

            Password = inputs.ContainsKey("Password") && !string.IsNullOrEmpty(inputs["Password"])
                ? inputs["Password"]
                : throw new Exception("PlayNGo TpList not found Password Setting");

            UserName = inputs.ContainsKey("UserName") && !string.IsNullOrEmpty(inputs["UserName"])
                ? inputs["UserName"]
                : throw new Exception("PlayNGo TpList not found UserName Setting");

            AccessToken = inputs.ContainsKey("AccessToken") && !string.IsNullOrEmpty(inputs["AccessToken"])
                ? inputs["AccessToken"]
                : throw new Exception("PlayNGo TpList not found AccessToken Setting");

            GameLaunchUrl = inputs.ContainsKey("GameLaunchUrl") && !string.IsNullOrEmpty(inputs["GameLaunchUrl"])
                ? inputs["GameLaunchUrl"]
                : throw new Exception("PlayNGo TpList not found GameLaunchUrl Setting");

            GameHistoryMobile = inputs.ContainsKey("GameHistoryMobile") && !string.IsNullOrEmpty(inputs["GameHistoryMobile"])
                ? inputs["GameHistoryMobile"]
                : throw new Exception("PlayNGo TpList not found GameHistoryMobile Setting");

            GameHistoryDesktop = inputs.ContainsKey("GameHistoryDesktop") && !string.IsNullOrEmpty(inputs["GameHistoryDesktop"])
                ? inputs["GameHistoryDesktop"]
                : throw new Exception("PlayNGo TpList not found GameHistoryDesktop Setting");

            Pid = inputs.ContainsKey("Pid") && !string.IsNullOrEmpty(inputs["Pid"])
                ? int.Parse(inputs["Pid"]) 
                : throw new Exception("PlayNGo TpList not found Pid Setting");

            GameId = inputs.ContainsKey("gameId") && !string.IsNullOrEmpty(inputs["gameId"])
                ? inputs["gameId"]
                : throw new Exception("Api Param inputs[gameId] not found");
        }
    }
}
