using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
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
        Task<NextspinCommonResponse> Handle(TransferRequest request);
    }

    public class NextspinHandler : INextspinHandler
    {
        public const Tp ThirdParty = Tp.Nextspin;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<NextspinHandler> _logger;

        public NextspinHandler(ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingService bettingService, SettledService settledService, ILogger<NextspinHandler> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBLL = tpCustInfoBLL;
            _bettingService = bettingService;
            _settledService = settledService;
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
        public async Task<NextspinCommonResponse> Handle(TransferRequest request)
        {    
            if(request.Type == NextspinTransferType.PlaceBet)
                return await PlaceBet(request);
            else if(request.Type == NextspinTransferType.Payout)
                return await Payout(request);
            throw new NotImplementedException();
        }
        private async Task<NextspinCommonResponse> PlaceBet(TransferRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.AcctId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new NextspinException(NextspinErrorCode.AcctLocked);
            }
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
            if (tpCustInfo == null)
            {
                _logger.LogError($"tpCustInfo is null, tpUserName:{request.AcctId}");
                throw new NextspinException(NextspinErrorCode.AcctLocked);
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                _logger.LogError($"tpCurrency is null, {JsonConvert.SerializeObject(tpCurrency)}");
                throw new NextspinException(NextspinErrorCode.AcctLocked);
            }
            var bettingData = new BettingDataRNG_Nextspin()
            {
                TransferId = request.TransferId,
                Amount = request.Amount.ConvertToMain(tpCurrency.ExchangeRate),
                TicketId = request.TicketId,
            };
            var result = await _bettingService.PlaceBetRNG_Nextspin(tpCustInfo, bettingData);
            if (result.Result > 0)
            {
                return new TransferResponse()
                {
                    TransferId = request.TransferId,
                    MerchantTxId = result.SQ,
                    AcctId = tpCustInfo.TpUserName,
                    Balance = result.AfterAmount,
                    MerchantCode = request.MerchantCode,
                    Msg = NextspinErrorCode.Success.GetEnumDescription(),
                    Code = (int)NextspinErrorCode.Success,
                    SerialNo = GenerateSerialNo()
                };
            }
            throw new NextspinException(NextspinErrorCode.InsufficientBalance);
        }

        private async Task<NextspinCommonResponse> Payout(TransferRequest request) 
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
            if (tpCustInfo == null)
            {
                _logger.LogError($"tpCustInfo is null, tpUserName:{request.AcctId}");
                throw new NextspinException(NextspinErrorCode.AcctLocked);
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                _logger.LogError($"tpCurrency is null, {JsonConvert.SerializeObject(tpCurrency)}");
                throw new NextspinException(NextspinErrorCode.AcctLocked);
            }
            var bettingData = new BettingDataRNG_Nextspin()
            {
                TransferId = request.TransferId,
                Amount = request.Amount.ConvertToMain(tpCurrency.ExchangeRate),
                TicketId = request.TicketId,
            };
            var result = await _settledService.PayoutRNG_Nextspin(tpCustInfo, bettingData);
            var response = new TransferResponse()
            {
                TransferId = request.TransferId,
                MerchantTxId = result.SQ,
                AcctId = tpCustInfo.TpUserName,
                MerchantCode = request.MerchantCode,
                Msg = NextspinErrorCode.Success.GetEnumDescription(),
                Code = (int)NextspinErrorCode.Success,
                SerialNo = GenerateSerialNo()
            };
            if (result.AfterAmount == 0)
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.AcctId, (int)GameType.Nextspin_RNG);
                response.Balance = tpCustBalance.Balance;
                return response;
            }
            response.Balance = result.AfterAmount;
            return response;
        }

        public static string GenerateSerialNo() => Guid.NewGuid().ToString("N");
    }
}
