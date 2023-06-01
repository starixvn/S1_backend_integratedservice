using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public interface INextspinHandler
    {
        NextspinCommonResponse Handle(GetBalanceRequest request);
    }

    public class NextspinHandler : INextspinHandler
    {
        public const Tp ThirdParty = Tp.Nextspin;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<NextspinHandler> _logger;

        public NextspinHandler(ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, ILogger<NextspinHandler> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBLL = tpCustInfoBLL;
            _logger = logger;
        }


        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        public NextspinCommonResponse Handle(GetBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AcctId)) throw new NextspinException(NextspinErrorCode.InvalidAcctId);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.AcctId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist, $"{request.AcctId} is locked");
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.AcctId, (int)GameType.Nextspin_RNG);
            if (tpCustBalance != null)
            {
                return new GetBalanceResponse()
                {
                    AcctInfo = new AcctInfo()
                    {
                        UserName = request.AcctId,
                        Currency = tpCustBalance.TpCurrencyId,
                        AcctId = request.AcctId,
                        Balance = tpCustBalance.Balance
                    },
                    MerchantCode = request.MerchantCode,
                    Msg = NextspinErrorCode.Success.GetEnumDescription(),
                    Code = (int)NextspinErrorCode.Success,
                    SerialNo = Guid.NewGuid().ToString(),
                };
            }
            return new NextspinCommonResponse();
        }
    }
}
