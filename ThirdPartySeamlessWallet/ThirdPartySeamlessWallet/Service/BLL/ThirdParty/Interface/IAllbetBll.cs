using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IAllbetBll
    {
        Task<AllBetTransferResponse> SeamlessWallet(AllbetType transType, string bodyText);
    }
}