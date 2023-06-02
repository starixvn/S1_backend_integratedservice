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
        NextspinCommonResponse Handle(AuthorizeRequest request);
        NextspinCommonResponse Handle(GetBalanceRequest request);
        NextspinCommonResponse Handle(TransferRequest request);
    }

    public class NextspinHandler : INextspinHandler
    {
        public const Tp ThirdParty = Tp.Nextspin;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<NextspinHandler> _logger;

        public NextspinHandler(ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, ILogger<NextspinHandler> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBLL = tpCustInfoBLL;
            _logger = logger;
        }
        public NextspinCommonResponse Handle(AuthorizeRequest request) 
        {
            if (string.IsNullOrWhiteSpace(request.AcctId)) throw new NextspinException(NextspinErrorCode.InvalidAcctId);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.AcctId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new NextspinException(NextspinErrorCode.AcctLocked, $"{request.AcctId} is locked");
            }
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
            if (tpCustInfo == null) throw new NextspinException(NextspinErrorCode.AcctNotFound);

            return new GetBalanceResponse()
            {
                AcctInfo = new AcctInfo()
                {
                    UserName = tpCustInfo.TpUserName,
                    Currency = tpCustInfo.TpCurrencyId,
                    AcctId = tpCustInfo.TpUserName,
                    Balance = 0
                },
                MerchantCode = request.MerchantCode,
                Msg = NextspinErrorCode.Success.GetEnumDescription(),
                Code = (int)NextspinErrorCode.Success,
                SerialNo = GenerateSerialNo(),
            };
        }
        public NextspinCommonResponse Handle(GetBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AcctId)) throw new NextspinException(NextspinErrorCode.InvalidAcctId);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.AcctId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new NextspinException(NextspinErrorCode.AcctLocked, $"{request.AcctId} is locked");
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
            if (tpCustInfo == null) throw new NextspinException(NextspinErrorCode.AcctNotFound);

            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.AcctId, (int)GameType.Nextspin_RNG);
            if (tpCustBalance == null) throw new NextspinException(NextspinErrorCode.AcctNotFound);

            return new GetBalanceResponse()
            {
                AcctInfo = new AcctInfo()
                {
                    UserName = tpCustInfo.TpUserName,
                    Currency = tpCustBalance.TpCurrencyId,
                    AcctId = tpCustInfo.TpUserName,
                    Balance = tpCustBalance.Balance
                },
                MerchantCode = request.MerchantCode,
                Msg = NextspinErrorCode.Success.GetEnumDescription(),
                Code = (int)NextspinErrorCode.Success,
                SerialNo = GenerateSerialNo(),
            };
        }
        public NextspinCommonResponse Handle(TransferRequest request)
        {
            throw new NotImplementedException();
        }

        public static string GenerateSerialNo() => Guid.NewGuid().ToString();
    }
}
