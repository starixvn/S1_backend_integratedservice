using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface ITCGBLL
    {
        Task<TCGReturnObj> SeamlessWallet(string merchantCode, string @params, string sign, string sitename);
        Task<TCGReturnObj> SeamlessWallet(string merchantCode, TCGRequest request, string sitename);
    }
}