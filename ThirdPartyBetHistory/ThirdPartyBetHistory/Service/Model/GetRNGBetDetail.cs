namespace ThirdPartyBetHistory.Service.Model
{
    public class GetRNGBetDetail
    {
        public int error_code { get; set; }
        public string message { get; set; }
        public GetRNGBetDetailData Data { get; set; }
    }
    public class GetRNGBetDetailData
    {
        public long last_version_key { get; set; }
        public IEnumerable<RNGBetDetail> RNGBetDetails { get; set; }
    }

    public class RNGBetDetail
    {
        public long trans_id { get; set; }
        public string vendor_member_id { get; set; }
        public DateTime transaction_time { get; set; }
        public decimal stake { get; set; }
        public decimal bet_amount { get; set; }
        public decimal winlost_amount { get; set; }
        public decimal after_amount { get; set; }
        public string ticket_status { get; set; }
        public int bet_type { get; set; }
        public int currency { get; set; }
        public long ref_no { get; set; }
        public int version_key { get; set; }
    }
}
