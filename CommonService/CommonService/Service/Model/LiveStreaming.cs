namespace CommonService.Service.Model
{
    public class LiveStreamingConfig
    {
        public string ApiURL { get; set; }
        public string Action { get; set; }
        public string Apiuser { get; set; }
        public string Key { get; set; }
        public string Ip { get; set; }
    }
    public class LiveStreamingLinkReq
    {
        public int Ch { set; get; }
        public string SiteName { set; get; }
        public string Uid { set; get; }
    }
    public class LiveStreamingLinkResult
    {
        public string H5LINKROW { set; get; }
        public string Status { set; get; }
    }

}
