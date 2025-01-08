using BetDetailApiHost.Models.GetBetDetail;
using Refit;

namespace BetDetailApiHost.ApiClient
{
    public interface ISabaSportApi
    {
        [Post("/BatchBetDetails")]
        Task<object> BatchBetDetails([Body] List<BetDetail> request);
    }
}