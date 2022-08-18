using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class Kiss918BLL : IKiss918BLL
    {
        public async Task<AuthenticateResponse_Kiss918> Authenticate(AuthenticateRequest_Kiss918 request)
        {
            return new AuthenticateResponse_Kiss918() { balance = 1000, playerID = 149186, error = Kiss918ErrorCode.Success.ToString() };
        }

        public async Task<GetBalanceResponse_Kiss918> GetBalance(GetBalanceRequest_Kiss918 request)
        {
            return new GetBalanceResponse_Kiss918() { balance = 1000, error = Kiss918ErrorCode.Success.ToString() };
        }
    }
}
