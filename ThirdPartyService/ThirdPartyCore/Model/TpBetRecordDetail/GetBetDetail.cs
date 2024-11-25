namespace ThirdPartyCore.Model.TpBetRecordDetail
{
    public class GetBetDetail
    {
        public string TransId { get; set; }

        public int CustId { get; set; }

        public DateTime SettledTime { get; set; }

        public TpGameTypeOption TpGameTypeOption { get; set; }

        public GetBetDetail(GetBetDetailRequest request, TpGameTypeOption tpGameTypeOption)
        {
            TransId = request.TransId;
            CustId = request.CustId;
            SettledTime = request.SettledTime;
            TpGameTypeOption = tpGameTypeOption;
        }
    }
}
