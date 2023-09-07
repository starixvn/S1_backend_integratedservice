using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using static ThirdPartySeamlessWallet.Controllers.N2LiveController;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class PNGBLL : IPNGBLL
    {
        public Tp TpId => Tp.PNG;
        public GameType TpGameType => GameType.PNG_RNG;

        private readonly AuthBLL _authBll;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<PNGBLL> _logger;

        public PNGBLL(AuthBLL authBll, ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBll,
            BettingService bettingService,
            SettledService settledService,
            ILogger<PNGBLL> logger)
        {
            _authBll = authBll;
            _thirdPartyBLL = thirdPartyBLL;
            _tpCustInfoBll = tpCustInfoBll;
            _bettingService = bettingService;
            _settledService = settledService;
            _logger = logger;
        }

        public PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest)
        {
            // 檢查 AuthToken
            if (!_authBll.Authenticate(authenticateRequest.AuthToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.NoUser,
                    StatusMessage = "AuthToken Expired",
                };
            }

            var checkResult = CheckTpUserWithAccessToken(tpCustInfoAuth.TpUserName, authenticateRequest.AccessToken);
            if (!checkResult.IsVaild)
                return new PNG_AuthenticateResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)TpId, tpCustInfoAuth.TpUserName, (int)TpGameType);
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, tpCustInfoAuth.TpUserName);
            var tpCountryCode = _tpCustInfoBll.GetTpCurrencyCode((int)TpId, tpCustInfo.CurrencyId);
            var userCurrency = this.GetCurrencyIsoCode(tpCountryCode.TpCurrencyId);

            return new PNG_AuthenticateResponse()
            {
                TpUserName = tpCustInfoAuth.TpUserName,
                StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                StatusMessage = "Success",
                UserCurrency = userCurrency,
                Country = tpCountryCode.CountryCode,
                Birthdate = new DateTime(1970, 1, 1), // 給default
                Registration = new DateTime(1970, 1, 1), // 給default
                Real = tpCustBalance.Balance
            };
        }

        public PNG_BalanceResponse Balance(PNG_BalanceRequest balanceRequest)
        {
            // check playNGo accessToken
            if (!ValidToken(balanceRequest.AccessToken))
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Internal,
                    StatusMessage = "AccessToken not equal",
                    Real = default
                };
            }

            // check user exist
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, balanceRequest.TpUserName);
            if (tpCustInfo == null)
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Internal,
                    StatusMessage = "tpCustInfo not found user not exist",
                    Real = default
                };
            }

            // check player currency 是否正確
            var userCurrency = GetCurrencyIsoCode(tpCustInfo.TpCurrencyId);
            if (userCurrency != balanceRequest.Currency)
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Internal,
                    StatusMessage = $"{balanceRequest.TpUserName} Currency wrong",
                    Real = default
                };
            }

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)TpId, balanceRequest.TpUserName, (int)TpGameType);
            return new PNG_BalanceResponse
            {
                StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                StatusMessage = "Success",
                Real = tpCustBalance.Balance
            };
        }

        public async Task<PNG_ReserveResponse> Reserve(PNG_ReserveRequest reserveRequest)
        {
            var bettingData = new BettingDataRNG_PNG();
            try
            {
                var checkResult = CheckTpUserWithAccessToken(reserveRequest.TpUserName, reserveRequest.AccessToken);
                if (!checkResult.IsVaild)
                    return new PNG_ReserveResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

                var betTransData = _bettingService.GetBettingDataRng_PNG(reserveRequest.RoundId);
                ValidateBetTrans(BettransType.Bet, betTransData);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = reserveRequest.TpUserName,
                    ProductId = reserveRequest.ProductId,
                    TransactionId = reserveRequest.TransactionId,
                    Amount = reserveRequest.Real,
                    CurrencyIsoCode = reserveRequest.Currency,
                    GameId = reserveRequest.GameId,
                    GameSessionId = reserveRequest.GameSessionId,
                    RoundId = reserveRequest.RoundId,
                    Channel = reserveRequest.Channel,
                    FreeGameExternalId = reserveRequest.FreeGameExternalId,
                    ActualAmount = reserveRequest.ActualAmount,
                    JackpotWin = GetJackpotsWinAmount(reserveRequest.PngJackpots),
                    JackpotLoss = GetJackpotsLossAmount(reserveRequest.PngJackpots),
                    TicketType = (int)BettransType.Bet,
                };

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, reserveRequest.TpUserName);
                var reserveResult = await _bettingService.PlaceBetRNG_PNG(tpCustInfo, bettingData);

                return new PNG_ReserveResponse
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                    StatusMessage = "Reserve Success",
                    ExternalTransactionId = reserveResult.ExternalTransactionId,
                    AfterAmount = reserveResult.AfterAmount
                };
            }
            catch (PNGException ex)
            {
                _logger.LogError(ex, $"Reserve TpUserName:{reserveRequest.TpUserName} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new PNG_ReserveResponse((int)PNG_StatsuCodeEnum.Internal, ex.ErrorMessage);
            }
        }

        public async Task<PNG_CancelReserveResponse> CancelReserve(PNG_CancelReserveRequest cancelReserveRequest)
        {
            var bettingData = new BettingDataRNG_PNG();
            try
            {
                var checkResult = CheckTpUserWithAccessToken(cancelReserveRequest.TpUserName, cancelReserveRequest.AccessToken);
                if (!checkResult.IsVaild)
                    return new PNG_CancelReserveResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

                // check BetTrans
                var betTransData = _bettingService.GetBettingDataRng_PNG(cancelReserveRequest.RoundId);
                ValidateBetTrans(BettransType.CancelBet, betTransData);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = cancelReserveRequest.TpUserName,
                    ProductId = cancelReserveRequest.ProductId,
                    TransactionId = cancelReserveRequest.TransactionId,
                    Amount = cancelReserveRequest.Real,
                    CurrencyIsoCode = cancelReserveRequest.Currency,
                    GameId = cancelReserveRequest.GameId,
                    GameSessionId = cancelReserveRequest.GameSessionId ?? string.Empty,
                    RoundId = cancelReserveRequest.RoundId,
                    Channel = cancelReserveRequest.Channel,
                    FreeGameExternalId = cancelReserveRequest.FreeGameExternalId,
                    ActualAmount = cancelReserveRequest.ActualAmount,
                    JackpotWin = GetJackpotsWinAmount(cancelReserveRequest.PngJackpots),
                    JackpotLoss = GetJackpotsLossAmount(cancelReserveRequest.PngJackpots),
                    TicketType = (int)BettransType.CancelBet
                };

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, cancelReserveRequest.TpUserName);
                var cancelBetResult = await _settledService.CancelBetRNG_PNG(tpCustInfo, bettingData);

                return new PNG_CancelReserveResponse
                {
                    ExternalTransactionId = cancelBetResult.ExternalTransactionId,
                    StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                    StatusMessage = "CancelReserve Success"
                };
            }
            catch (PNGException ex)
            {
                _logger.LogError(ex, $"CancelReserve TpUserName:{cancelReserveRequest.TpUserName} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new PNG_CancelReserveResponse((int)PNG_StatsuCodeEnum.Internal, ex.ErrorMessage);
            }
        }

        public async Task<PNG_ReleaseResponse> Release(PNG_ReleaseRequest releaseRequest)
        {
            var bettingData = new BettingDataRNG_PNG();
            try
            {
                var checkResult = CheckTpUserWithAccessToken(releaseRequest.TpUserName, releaseRequest.AccessToken);
                if (!checkResult.IsVaild)
                    return new PNG_ReleaseResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

                // check BetTrans
                var betTransData = _bettingService.GetBettingDataRng_PNG(releaseRequest.RoundId);

                // check ticketType
                var pngTicketType = releaseRequest.RoundId == 0
                    ? BettransType.EndRound
                    : BettransType.Payout;

                ValidateBetTrans(pngTicketType, betTransData);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = releaseRequest.TpUserName,
                    ProductId = releaseRequest.ProductId,
                    TransactionId = releaseRequest.TransactionId,
                    Amount = releaseRequest.Real,
                    CurrencyIsoCode = releaseRequest.Currency,
                    GameId = releaseRequest.GameId,
                    GameSessionId = releaseRequest.GameSessionId ?? string.Empty,
                    RoundId = releaseRequest.RoundId,
                    Channel = releaseRequest.Channel,
                    FreeGameExternalId = releaseRequest.FreeGameExternalId,
                    ActualAmount = decimal.Zero,
                    JackpotWin = GetJackpotsWinAmount(releaseRequest.PngJackpots),
                    JackpotLoss = GetJackpotsLossAmount(releaseRequest.PngJackpots),
                    TicketType = (int)pngTicketType
                };

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)TpId, releaseRequest.TpUserName);
                var payOutResult = await _settledService.PayoutRNG_PNG(tpCustInfo, bettingData);

                var response = new PNG_ReleaseResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                    StatusMessage = "Release Success",
                    AfterAmount = payOutResult.AfterAmount,
                    ExternalTransactionId = payOutResult.ExternalTransactionId
                };

                if (response.AfterAmount != 0) return response;

                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)TpId, releaseRequest.TpUserName, (int)TpGameType);
                response.AfterAmount = tpCustBalance.Balance;

                return response;
            }
            catch (PNGException ex)
            {
                _logger.LogError(ex, $"Release TpUserName:{releaseRequest.TpUserName} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new PNG_ReleaseResponse((int)PNG_StatsuCodeEnum.Internal, ex.ErrorMessage);
            }
        }


        #region private method

        private string GetCurrencyIsoCode(string tpCurrencyId)
        {
            var tpCurrencyOfIsoCode = _thirdPartyBLL.GeTpCurrencyOfIsoCode((int)TpId);

            if (tpCurrencyOfIsoCode.ContainsKey(tpCurrencyId)) return tpCurrencyOfIsoCode[tpCurrencyId];

            _logger.LogError("tpCurrencyOfIsoCode not found");
            return string.Empty;
        }

        private bool ValidToken(string inputAccessToken)
        {
            var tpListSetting = _thirdPartyBLL.GetThirdPartySetting((int)TpId);
            var accessToken = tpListSetting.TpSetting.ContainsKey("AccessToken")
                ? tpListSetting.TpSetting["AccessToken"]
                : string.Empty;

            if (!string.IsNullOrEmpty(accessToken)) return accessToken == inputAccessToken;

            _logger.LogError("thirdPartySetting not found");
            return false;
        }

        private (bool IsVaild, PNG_BaseResponse response) CheckTpUserWithAccessToken(string tpUserName, string accessToken)
        {
            // 檢查 PlayNGo AccessToken
            if (!ValidToken(accessToken))
            {
                return (false, new PNG_BaseResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Internal,
                    StatusMessage = "AccessToken Invalid"
                });
            }

            var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)TpId, tpUserName);
            if (tpCheck == null || tpCheck.Locked)
            {
                return (false, new PNG_BaseResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.AccountLocked,
                    StatusMessage = "User Account Locked"
                });
            }

            return (true, null);
        }

        private decimal GetJackpotsLossAmount(PNGJackpots pngJackpots)
        {
            if (pngJackpots == null) return 0;

            return !pngJackpots.PNGJackpot.Any()
                ? 0
                : pngJackpots.PNGJackpot.Sum(r => r.Loss);
        }

        public decimal GetJackpotsWinAmount(PNGJackpots pngJackpots)
        {
            if (pngJackpots == null) return 0;

            return !pngJackpots.PNGJackpot.Any()
                ? 0
                : pngJackpots.PNGJackpot.Sum(r => r.Win);
        }

        public void ValidateBetTrans(BettransType ticketType, IEnumerable<BettingDataRNG_PNG> bettingData)
        {
            switch (ticketType)
            {
                case BettransType.Bet:
                    // 檢查 BetTrans 是否有投注紀錄了，避免重複投注
                    if (bettingData.Any(r => r.TicketType == (int)BettransType.Bet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "RoundId Bet Is Duplicated");
                    break;

                case BettransType.CancelBet:

                    // 檢查 BetTrans 是否有這筆投注紀錄
                    if (!bettingData.Any(r => r.TicketType == (int)BettransType.Bet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "RoundId Is NotFound");

                    // 檢查 BetTrans 是否已經有取消紀錄
                    if (bettingData.Any(r => r.TicketType == (int)BettransType.CancelBet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "RoundId Duplicated Cancel");

                    break;
                case BettransType.Payout:

                    // 檢查 BetTrans 是否已存在取消投注紀錄
                    if (bettingData.Any(r => r.TicketType == (int)BettransType.CancelBet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "RoundId Already Cancel");

                    // 檢查 BetTrans 是否已經有派彩過的紀錄
                    if (bettingData.Any(r => r.TicketType == (int)BettransType.Payout))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "RoundId Duplicated Release");

                    break;
                case BettransType.EndRound:
                    // RoundID = 0，無法檢查邏輯
                    break;
                case BettransType.Reserve:
                case BettransType.Tip:
                case BettransType.Bonus:
                case BettransType.Jackpot:
                case BettransType.ConfirmReserve:
                default:
                    throw new PNGException(PNG_StatsuCodeEnum.Internal, "PNG RoundId TicketType NotImplemented");
            }
        }

        #endregion
    }
}
