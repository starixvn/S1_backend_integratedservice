namespace ThirdPartyWager.Service.Model.TpSiteConfig
{
    public class GPIConfig : TpSiteConfig
    {
        public string P2PUrl { get; set; }

        public string RslotUrl { get; set; }

        public string LotteryUrl { get; set; }

        public string OperatorId { get; set; }

        public string LiveCasinoUrl { get; set; }

        public string WagerMerchPwd { get; set; }

        public List<int> CommonSiteIds { get; set; }
    }
}
