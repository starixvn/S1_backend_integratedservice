namespace ThirdPartyCore.Model.TpBetRecordDetail
{
    public class GetBetDetailRequest
    {
        public int GameType { get; set; }
        public string TransId { get; set; }
        public int CustId { get; set; }
        public DateTime SettledTime { get; set; }
    }
}
