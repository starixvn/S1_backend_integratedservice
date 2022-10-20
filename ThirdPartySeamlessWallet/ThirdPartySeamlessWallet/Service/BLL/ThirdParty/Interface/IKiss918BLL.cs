using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface
{
    public interface IKiss918BLL
    {
        Task<AuthenticateResponse_Kiss918> Authenticate(AuthenticateRequest_Kiss918 request);

        Task<GetBalanceResponse_Kiss918> GetBalance(int playerID);

        Task<OperationResponse_Kiss918> Bet(BetRequest_Kiss918 request);

        Task<OperationResponse_Kiss918> BetResult(BetResultRequest_Kiss918 request);

        Task<OperationResponse_Kiss918> Refund(RefundRequest_Kiss918 request);

        Task<OperationResponse_Kiss918> JackpotWin(JackpotWinRequest_Kiss918 request);
    }
}
