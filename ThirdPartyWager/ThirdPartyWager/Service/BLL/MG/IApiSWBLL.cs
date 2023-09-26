using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public interface IApiSWBLL
    {
        Task<bool> MgUpdateBalance(MGUpdatebalanceRequest request);
    }
}