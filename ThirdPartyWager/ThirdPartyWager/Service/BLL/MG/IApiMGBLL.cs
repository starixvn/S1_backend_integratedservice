using System.Collections.Generic;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.MG
{
    public interface IApiMGBLL
    {
        Dictionary<string, string> MgConfig { get; set; }
        Task<List<MGFailedTransactions>> GetFailedTransactionsAsync(string token, string currencyId);
        Task UpdateFailedTransactionToResolvedState(string token, string txnId, string currencyId);
        Task<string> GetToken(string tpCurrencyId);
    }
}