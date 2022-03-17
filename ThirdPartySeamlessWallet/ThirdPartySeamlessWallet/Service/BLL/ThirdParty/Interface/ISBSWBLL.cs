using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ISBSWBLL
    {
        Task<ReturnObj> SeamlessWallet(SBSWTransType transType, string merchant, string bodyText);
    }
}