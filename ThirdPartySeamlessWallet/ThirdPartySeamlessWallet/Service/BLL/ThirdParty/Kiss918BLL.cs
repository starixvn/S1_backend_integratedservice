using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class Kiss918BLL : IKiss918BLL
    {
        public async Task<AuthenticateResponse_Kiss918> Authenticate(AuthenticateRequest_Kiss918 request)
        {
            return new AuthenticateResponse_Kiss918() { balance = 1000, playerID = 149186, error = "0" };
        }

        public async Task<GetBalanceResponse_Kiss918> GetBalance(GetBalanceRequest_Kiss918 request)
        {
            return new GetBalanceResponse_Kiss918() { balance = 1000, error = "0" };
        }

        public async Task<OperationResponse_Kiss918> Bet(BetRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }

        public async Task<OperationResponse_Kiss918> BetResult(BetResultRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }

        public async Task<OperationResponse_Kiss918> Refund(RefundRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }

        public async Task<OperationResponse_Kiss918> FundRequest(FundRequestRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }

        public async Task<OperationResponse_Kiss918> FundReturn(FundReturnRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }

        public async Task<FundBetResultResponse_Kiss918> FundBetResult(FundBetResultRequest_Kiss918 request)
        {
            return new FundBetResultResponse_Kiss918() { transaction = "", error = "0" };
        }

        public async Task<OperationResponse_Kiss918> JackpotWin(JackpotWinRequest_Kiss918 request)
        {
            return new OperationResponse_Kiss918() { transaction = "", balance = 1000, error = "0" };
        }
    }
}
