using ThirdPartyCore.Model;

namespace ThirdParty.JILI
{
    internal class JILIConfig : TpSiteConfig
    {
        public string AgentId { get; set; }
        public string AgentKey { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string FunGameUrl { get; set; }
        public string AgentId_Stream { get; set; }
        public string Password_Stream { get; set; }
        public string Username_Stream { get; set; }
    }
}
