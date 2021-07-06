using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL.MG
{
    public interface IApiSWBLL
    {
        Task<bool> MgUpdateBalance(MGUpdatebalanceRequest request);
    }
}