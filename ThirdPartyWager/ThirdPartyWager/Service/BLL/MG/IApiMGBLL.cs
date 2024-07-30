using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public interface IApiMGBLL
    {
        Task<List<MGFailedTransactions>> GetFailedTransactionsAsync(string token, string currencyId, int siteId);

        Task UpdateFailedTransactionToResolvedState(string token, string txnId, string currencyId, int siteId);

        Task<string> GetToken(string tpCurrencyId, int siteId);

        Task<IEnumerable<MGPlayerGameAnalysis>> GetPlayerGameAnalysisAsync(string token, string currencyId, int siteId, DateTime date);

        Task<IEnumerable<MGFortuneRewards>> GetFortuneRewardsAsync(string token, string currencyId, int siteId, DateTime date);
    }
}