using System;
using ThirdPartyCore.Model;

namespace ThirdParty.MP
{
    internal class MPConfig : TpSiteConfig
    {
        public string Agent { get; set; }
        public string Deskey { get; set; }
        public string Md5key { get; set; }
    }
}
