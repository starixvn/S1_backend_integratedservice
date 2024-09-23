namespace ThirdPartyWager.Service.BLL
{
    public interface ITpRefundBLL
    {
        public IEnumerable<dynamic> GetPendingList(int gameType);

        public Task<bool> CheckTicketStatus(dynamic pending, int gameType);
    }
}
