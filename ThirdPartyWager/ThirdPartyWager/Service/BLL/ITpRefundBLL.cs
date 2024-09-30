using ThirdPartyWager.Service.Library;

namespace ThirdPartyWager.Service.BLL
{
    public interface ITpRefundBLL
    {
        public IEnumerable<dynamic> GetPendingList(int gameType, TimeCast ticketTime);

        public Task<bool> CheckTicketStatus(dynamic pending, int gameType);

        TimeCast GetTicketTime(DateTime startTime, int lockPeriod, out bool exec);
    }
}
