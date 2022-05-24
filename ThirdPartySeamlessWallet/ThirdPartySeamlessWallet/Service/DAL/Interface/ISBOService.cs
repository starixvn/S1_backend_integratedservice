using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL.Interface
{
    public interface ISBOService
    {
        Task<SBOGetBetStatusData> GetBetStatus(string transferCode);
    }
}