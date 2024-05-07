using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public interface IApiMGBLL
    {
        Task<List<MGFailedTransactions>> GetFailedTransactionsAsync(string token, string currencyId);

        Task UpdateFailedTransactionToResolvedState(string token, string txnId, string currencyId);

        Task<string> GetToken(string tpCurrencyId);

        Task<IEnumerable<MGPlayerGameAnalysis>> GetPlayerGameAnalysisAsync(string token, string currencyId, DateTime date);

        Task<IEnumerable<MGFortuneRewards>> GetFortuneRewardsAsync(string token, string currencyId, DateTime date);
    }
}