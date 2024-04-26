using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public interface INextspinBLL
    {
        NextspinCommonResponse Handle(AuthorizeRequest request);
        NextspinCommonResponse Handle(GetBalanceRequest request);
        Task<NextspinCommonResponse> Handle(TransferRequest request);
    }

    public class NextspinBLL : INextspinBLL
    {
        public const Tp ThirdParty = Tp.Nextspin;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<NextspinBLL> _logger;
        private Dictionary<NextspinTransferType, Func<TransferRequest, Task<NextspinCommonResponse>>> TransferHandler;

        public NextspinBLL(ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingService bettingService, SettledService settledService, ILogger<NextspinBLL> logger)
        {
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBLL = tpCustInfoBLL;
            _bettingService = bettingService;
            _settledService = settledService;
            _logger = logger;
            TransferHandler = new Dictionary<NextspinTransferType, Func<TransferRequest, Task<NextspinCommonResponse>>>()
            {
                { NextspinTransferType.PlaceBet, PlaceBet},
                { NextspinTransferType.CancelBet, CancelBet},
                { NextspinTransferType.Payout, Payout},
                { NextspinTransferType.JackpotPayout, Payout},
            };
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
            if (TransferHandler.ContainsKey(request.Type))
            {
                return await TransferHandler[request.Type].Invoke(request);
            }
            throw new NotImplementedException();
        }

        public async Task<NextspinCommonResponse> PlaceBet(TransferRequest request)
        {
            var bettingData = new BettingDataRNG_Nextspin();
            try
            {
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
                ValidateBettrans(NextspinTransferType.PlaceBet, data);

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
                    ticketId = "",
                    specialGameType = ""
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
                    Code = (int)ex.NextspinErrorCode,
                    Msg = ex.ErrorMessage,
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
                    throw new NextspinException(NextspinErrorCode.AcctNotFound);

                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
                if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
                    throw new NextspinException(NextspinErrorCode.CurrencyInvalid);

                var data = _bettingService.GetBettingDataRNG_Nextspin(request.ReferenceId);
                ValidateBettrans(NextspinTransferType.Payout, data);

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
                    channel = request.Channel,
                    specialGameType = (request.SpecialGame == null || string.IsNullOrEmpty(request.SpecialGame.Type)) ? "" : request.SpecialGame.Type,
                    count = request.SpecialGame == null ? 0 : request.SpecialGame.Count,
                    sequence = request.SpecialGame == null ? 0 : request.SpecialGame.Sequence
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
                response.Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate);
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
        public async Task<NextspinCommonResponse> CancelBet(TransferRequest request)
        {
            var bettingData = new BettingDataRNG_Nextspin();
            try
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.AcctId);
                if (tpCustInfo == null)
                    throw new NextspinException(NextspinErrorCode.AcctNotFound);

                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
                if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
                    throw new NextspinException(NextspinErrorCode.CurrencyInvalid);

                var data = _bettingService.GetBettingDataRNG_Nextspin(request.ReferenceId);
                ValidateBettrans(NextspinTransferType.CancelBet, data);

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
                    channel = request.Channel,
                    specialGameType = ""
                };
                var result = await _settledService.CancelBetRNG_Nextspin(tpCustInfo, bettingData);
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
                response.Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate);
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

        public void ValidateBettrans(NextspinTransferType type, IEnumerable<BettingDataRNG_Nextspin> bettrans)
        {
            if (type == NextspinTransferType.PlaceBet)
            {
                // 已存在下注注單
                if (bettrans.Any()) throw new NextspinException(NextspinErrorCode.DuplicatedReferenceId);
            }
            else
            {
                // 不存在下注注單
                if (!bettrans.Any()) throw new NextspinException(NextspinErrorCode.RecordIdNotFound);
                // 已經payout或取消
                if (bettrans.Max(x => x.TicketStatus) > 0) throw new NextspinException(NextspinErrorCode.DuplicatedReferenceId);
            }
        }
        private void ValidateRequest(TransferRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.AcctId)) throw new NextspinException(NextspinErrorCode.InvalidAcctId);
            if (request.Amount < 0) throw new NextspinException(NextspinErrorCode.AmountInvalid);
        }
        public static string GenerateSerialNo() => Guid.NewGuid().ToString();
    }
}
