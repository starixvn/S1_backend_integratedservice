using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBSWBLL
    {
        Task<SBSWReturnObj> SeamlessWallet(string merchant, string method, string bodyText);
    }
}