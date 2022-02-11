using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBSWBLL
    {
        Task<ReturnObj> SeamlessWallet(string merchant, string method, string bodyText);
    }
}