using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IHBBLL
    {
        playerdetailresponse PlayerDetail(string token, bool gamelaunch);
        bool Auth(string request);
        Task<dynamic> FundTransfer(dynamic request);
        bool QueryRequest(dynamic request);
    }
}