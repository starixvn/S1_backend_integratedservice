using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ITCGBLL
    {
        Task<TCGReturnObj> SeamlessWallet(string merchantCode, string @params, string sign);
        Task<TCGReturnObj> SeamlessWallet(string merchantCode, TCGRequest request);
    }
}