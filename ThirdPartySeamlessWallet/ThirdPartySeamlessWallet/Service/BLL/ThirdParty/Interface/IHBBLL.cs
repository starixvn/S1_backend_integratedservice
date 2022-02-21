using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IHBBLL
    {
        Task<HBPlayerDetailResponse> Auth(HBPlayerDetailRequest request);
        Task<playerdetailresponse> Auth(string request);
        Task<dynamic> FundTransfer(dynamic request);
    }
}