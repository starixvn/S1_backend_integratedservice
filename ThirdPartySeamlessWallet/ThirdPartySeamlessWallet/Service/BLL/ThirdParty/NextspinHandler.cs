using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public interface INextspinHandler
    {
        void SetApiName(string apiName);
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
        public void SetApiName(string apiName)
        {
            LogContext.PushProperty("ApiName", apiName);
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
            ValidateRequest(request);
            if (request.Type == NextspinTransferType.PlaceBet)
                return await PlaceBet(request);
            else if(request.Type == NextspinTransferType.Payout)
                return await Payout(request);
            throw new NotImplementedException();
        }

        private void ValidateRequest(TransferRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AcctId)) throw new NextspinException(NextspinErrorCode.InvalidAcctId);
            if (request.Amount < 0) throw new NextspinException(NextspinErrorCode.AmountInvalid);
        }

        private async Task<NextspinCommonResponse> PlaceBet(TransferRequest request)
        {
            var bettingData = new BettingDataRNG_Nextspin();
            try
            {
                if (request.GameCode == "s5ForStar" && request.Amount == 100)
                {
                    Sleep();
                }
                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.AcctId);
                if (tpCheck == null || tpCheck.Locked)
                    throw new NextspinException(NextspinErrorCode.AcctLocked);

                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
                if (tpCustInfo == null)
                    throw new NextspinException(NextspinErrorCode.AcctNotFound);

                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
                if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
                    throw new NextspinException(NextspinErrorCode.CurrencyInvalid);

                var data = _bettingService.GetBettingDataRNG_Nextspin(request.TransferId);
                if (data != null)
                    throw new NextspinException(NextspinErrorCode.DuplicatedReferenceId);

                bettingData = new BettingDataRNG_Nextspin()
                {
                    acctId = request.AcctId,
                    transferId = request.TransferId,
                    currency = request.Currency,
                    amount = request.Amount.ConvertToMain(tpCurrency.ExchangeRate) * -1,
                    type = (int)request.Type,
                    gameCode = request.GameCode,
                    channel = request.Channel,
                    referenceId = request.TransferId,
                    ticketId = ""
                };
                var result = await _bettingService.PlaceBetRNG_Nextspin(tpCustInfo, bettingData);
                return new TransferResponse()
                {
                    TransferId = request.TransferId,
                    MerchantTxId = result.SQ,
                    AcctId = tpCustInfo.TpUserName,
                    Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate),
                    MerchantCode = request.MerchantCode,
                    Msg = NextspinErrorCode.Success.GetEnumDescription(),
                    Code = (int)NextspinErrorCode.Success,
                    SerialNo = GenerateSerialNo()
                };
            }
            catch (NextspinException ex)
            {
                _logger.LogError(ex, $"AcctId:{request.AcctId} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new NextspinCommonResponse() 
                { 
                    MerchantCode = request.MerchantCode,
                    Code = (int)ex.NextspinErrorCode, Msg = ex.ErrorMessage,
                    SerialNo = GenerateSerialNo() 
                };
            }
        }
        private async Task<NextspinCommonResponse> Payout(TransferRequest request) 
        {
            var bettingData = new BettingDataRNG_Nextspin();
            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
                if (tpCustInfo == null)
                    throw new NextspinException(NextspinErrorCode.AcctLocked);
                
                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
                if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
                    throw new NextspinException(NextspinErrorCode.AcctLocked);
                
                var data = _bettingService.GetBettingDataRNG_Nextspin(request.ReferenceId);
                if (data == null)
                    throw new NextspinException(NextspinErrorCode.RecordIdNotFound);

                bettingData = new BettingDataRNG_Nextspin()
                {
                    acctId = request.AcctId,
                    transferId = request.TransferId,
                    currency = request.Currency,
                    amount = request.Amount.ConvertToMain(tpCurrency.ExchangeRate),
                    type = (int)request.Type,
                    gameCode = request.GameCode,
                    referenceId = request.ReferenceId,
                    ticketId = request.TicketId,
                    channel = request.Channel
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
            catch (NextspinException ex)
            {
                _logger.LogError(ex, $"AcctId:{request.AcctId} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new NextspinCommonResponse()
                {
                    MerchantCode = request.MerchantCode,
                    Code = (int)ex.NextspinErrorCode,
                    Msg = ex.ErrorMessage,
                    SerialNo = GenerateSerialNo()
                };
            }
        }

        public static string GenerateSerialNo() => Guid.NewGuid().ToString();
        // 建立一個方法,讓程式sleep 60 秒
        private void Sleep() => Thread.Sleep(60000);
    }
}
