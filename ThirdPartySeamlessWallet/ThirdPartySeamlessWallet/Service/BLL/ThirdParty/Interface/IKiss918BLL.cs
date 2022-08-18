using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IKiss918BLL
    {
        Task<AuthenticateResponse_Kiss918> Authenticate(AuthenticateRequest_Kiss918 request);

        Task<GetBalanceResponse_Kiss918> GetBalance(GetBalanceRequest_Kiss918 request);
    }
}
