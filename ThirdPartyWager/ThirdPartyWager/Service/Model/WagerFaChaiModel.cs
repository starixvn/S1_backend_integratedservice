namespace ThirdPartyWager.Service.Model
{
    public class WagerFaChaiModel
    {
    }
    public class GetRecordListRequest
    {
        public string StartDate { get; set; }
        public string EndDate { get; set; }
    }
    public class RecordListData
    {
        public string Result { get; set; }
        public List<Record> Records { get; set; }
    }
    public class Record
    {
        public decimal bet { get; set; }
        public decimal validBet { get; set; }
        public decimal prize { get; set; }
        public decimal refund { get; set; }
        public decimal winlose { get; set; }
        public decimal commission { get; set; }
        public decimal before { get; set; }
        public decimal after { get; set; }
        public decimal jptax { get; set; }
        public decimal jppoints { get; set; }
        public string recordID { get; set; }
        public string account { get; set; }
        public int gameID { get; set; }
        public int gametype { get; set; }
        public int jpmode { get; set; }
        public DateTime bdate { get; set; }
        public bool isBuyFeature { get; set; }
    }
}
