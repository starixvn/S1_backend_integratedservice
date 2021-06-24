using System.Xml.Serialization;
using Newtonsoft.Json;

namespace ThirdPartySeamlessWallet.Service.Model
{
    public class CheckOnlineMemberInput
    {
        [JsonProperty("secret_key")]
        public string Secret_Key { get; set; }
        [JsonProperty("vendor_member_id")]
        public string Vendor_Member_Id { get; set; }
        [JsonProperty("op")]
        public string Op { get; set; }
    }
    
    public class CheckOnlineMemberOutput
    {
        public CheckOnlineMember CheckOnlineMember { get; set; }
    }

    [XmlRoot(ElementName = "member_online")]
    [JsonObject(Title = "CheckOnlineMember")]
    public class CheckOnlineMember
    {
        [XmlAttribute(AttributeName = "version")]
        public string Version { get; set; }
        [XmlElement(ElementName = "status_code")]
        public int StatusCode { get; set; }
        [XmlElement(ElementName = "message")]
        public string Message { get; set; }
    }
}