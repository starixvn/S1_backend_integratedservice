using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class PNGBLL : IPNGBLL, IThirdPartyBLL
    {
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBll;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly AuthBLL _authBll;
        private readonly ConfigBLL _configBll;
        private readonly ILogger<PNGBLL> _logger;
        private readonly Dictionary<string, string> defaultConfig;

        public PNGBLL(IServiceProvider provider)
        {
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBll = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _authBll = provider.GetRequiredService<AuthBLL>();
            _configBll = provider.GetRequiredService<ConfigBLL>();
            _logger = provider.GetRequiredService<ILogger<PNGBLL>>();

            ThirdParty = Tp.PNG;
            GameType = GameType.PNG_RNG;
            defaultConfig = _configBll.GetConfigValue<Dictionary<string, string>>(ThirdParty.ToString());
        }

        public PNG_AuthenticateResponse Authenticate(PNG_AuthenticateRequest authenticateRequest)
        {
            // 檢查 AuthToken
            if (!_authBll.Authenticate(authenticateRequest.AuthToken, out TpCustInfoAuth tpCustInfoAuth))
            {
                return new PNG_AuthenticateResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.WrongUserNamePassword,
                    StatusMessage = "WrongUserNamePassword",
                };
            }

            var checkResult = CheckTpUserWithAccessToken(tpCustInfoAuth.TpUserName, authenticateRequest.AccessToken, true);
            if (!checkResult.IsVaild)
                return new PNG_AuthenticateResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfoAuth.TpUserName, (int)GameType);
            var tpCountryCode = _tpCustInfoBll.GetTpCurrencyCodeByCurrencyId((int)ThirdParty, tpCustInfoAuth.TpCurrencyId);
            var userCurrency = tpCountryCode.TpCurrencyId;

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
            var checkResult = CheckTpUserWithAccessToken(balanceRequest.TpUserName, balanceRequest.AccessToken, true);
            if (!checkResult.IsVaild)
                return new PNG_BalanceResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

            // check user exist
            var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, balanceRequest.TpUserName);
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
            if (tpCustInfo.TpCurrencyId != balanceRequest.Currency)
            {
                return new PNG_BalanceResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Internal,
                    StatusMessage = $"{balanceRequest.TpUserName} Currency wrong",
                    Real = default
                };
            }

            var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)ThirdParty, balanceRequest.TpUserName, (int)GameType);
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
                var checkResult = CheckTpUserWithAccessToken(reserveRequest.TpUserName, reserveRequest.AccessToken, isTpCheck: true);
                if (!checkResult.IsVaild)
                    return new PNG_ReserveResponse(checkResult.response.StatusCode, checkResult.response.StatusMessage);

                var betTransData = _bettingService.GetBettingDataRng_PNG(reserveRequest.RoundId);
                ValidateBetTrans(BettransType.Bet, betTransData, reserveRequest.TransactionId);

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, reserveRequest.TpUserName);
                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = reserveRequest.TpUserName,
                    ProductId = reserveRequest.ProductId,
                    TransactionId = reserveRequest.TransactionId,
                    Amount = reserveRequest.Real *= tpCurrency.ExchangeRate,
                    CurrencyIsoCode = reserveRequest.Currency,
                    GameId = reserveRequest.GameId,
                    GameSessionId = reserveRequest.GameSessionId,
                    RoundId = reserveRequest.RoundId,
                    Channel = reserveRequest.Channel,
                    FreeGameExternalId = reserveRequest.FreeGameExternalId,
                    ActualAmount = reserveRequest.ActualAmount *= tpCurrency.ExchangeRate,
                    JackpotWin = GetJackpotsWinAmount(reserveRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    JackpotLoss = GetJackpotsLossAmount(reserveRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    TicketType = (int)BettransType.Bet,
                };


                var reserveResult = await _bettingService.PlaceBetRNG_PNG(tpCustInfo, bettingData);

                return new PNG_ReserveResponse
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                    StatusMessage = "Reserve Success",
                    ExternalTransactionId = reserveResult.ExternalTransactionId,
                    AfterAmount = reserveResult.AfterAmount *= tpCurrency.ExchangeRate,
                };
            }
            catch (PNGException ex)
            {
                _logger.LogError(ex, $"Reserve TpUserName:{reserveRequest.TpUserName} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new PNG_ReserveResponse((int)ex.StatsuCode, ex.ErrorMessage);
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
                ValidateBetTrans(BettransType.CancelBet, betTransData, cancelReserveRequest.TransactionId);

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, cancelReserveRequest.TpUserName);
                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = cancelReserveRequest.TpUserName,
                    ProductId = cancelReserveRequest.ProductId,
                    TransactionId = cancelReserveRequest.TransactionId,
                    Amount = cancelReserveRequest.Real *= tpCurrency.ExchangeRate,
                    CurrencyIsoCode = cancelReserveRequest.Currency,
                    GameId = cancelReserveRequest.GameId,
                    GameSessionId = cancelReserveRequest.GameSessionId ?? string.Empty,
                    RoundId = cancelReserveRequest.RoundId,
                    Channel = cancelReserveRequest.Channel,
                    FreeGameExternalId = cancelReserveRequest.FreeGameExternalId,
                    ActualAmount = cancelReserveRequest.ActualAmount *= tpCurrency.ExchangeRate,
                    JackpotWin = GetJackpotsWinAmount(cancelReserveRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    JackpotLoss = GetJackpotsLossAmount(cancelReserveRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    TicketType = (int)BettransType.CancelBet
                };


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
                return new PNG_CancelReserveResponse((int)ex.StatsuCode, ex.ErrorMessage);
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

                ValidateBetTrans(pngTicketType, betTransData, releaseRequest.TransactionId);

                var tpCustInfo = _tpCustInfoBll.GetTpCustInfoWithCache((int)ThirdParty, releaseRequest.TpUserName);
                var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);

                bettingData = new BettingDataRNG_PNG()
                {
                    TpUserName = releaseRequest.TpUserName,
                    ProductId = releaseRequest.ProductId,
                    TransactionId = releaseRequest.TransactionId,
                    Amount = releaseRequest.Real *= tpCurrency.ExchangeRate,
                    CurrencyIsoCode = releaseRequest.Currency,
                    GameId = releaseRequest.GameId,
                    GameSessionId = releaseRequest.GameSessionId ?? string.Empty,
                    RoundId = releaseRequest.RoundId,
                    Channel = releaseRequest.Channel,
                    FreeGameExternalId = releaseRequest.FreeGameExternalId,
                    ActualAmount = decimal.Zero,
                    JackpotWin = GetJackpotsWinAmount(releaseRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    JackpotLoss = GetJackpotsLossAmount(releaseRequest.PngJackpots) * tpCurrency.ExchangeRate,
                    TicketType = (int)pngTicketType
                };

                var finicalTransType = GetPayoutTransType(betTransData);
                var payOutResult = await _settledService.PayoutRNG_PNG(tpCustInfo, bettingData, finicalTransType);

                var response = new PNG_ReleaseResponse()
                {
                    StatusCode = (int)PNG_StatsuCodeEnum.Ok,
                    StatusMessage = "Release Success",
                    AfterAmount = payOutResult.AfterAmount,
                    ExternalTransactionId = payOutResult.ExternalTransactionId
                };

                if (response.AfterAmount != 0) return response;

                var tpCustBalance = _tpCustInfoBll.GetBalanceByHalfSecCache((int)ThirdParty, releaseRequest.TpUserName, (int)GameType);
                response.AfterAmount = tpCustBalance.Balance;

                return response;
            }
            catch (PNGException ex)
            {
                _logger.LogError(ex, $"Release TpUserName:{releaseRequest.TpUserName} bettingData:{JsonConvert.SerializeObject(bettingData)}");
                return new PNG_ReleaseResponse((int)ex.StatsuCode, ex.ErrorMessage);
            }
        }

        #region private method

        private bool ValidToken(string inputAccessToken)
        {
            var pngConfig = new PNGConfig(defaultConfig);
            if (!string.IsNullOrEmpty(pngConfig.AccessToken)) return pngConfig.AccessToken == inputAccessToken;

            _logger.LogError("thirdPartySetting not found");
            return false;
        }

        private (bool IsVaild, PNG_BaseResponse response) CheckTpUserWithAccessToken(string tpUserName, string accessToken, bool isTpCheck = false)
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

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, tpUserName);

            if (isTpCheck) // 三方 Test Case 會驗證 Reserve & Authenticate & Balance 是否 在線 or 鎖定
            {
                if (tpCheck == null)
                {
                    return (false, new PNG_BaseResponse()
                    {
                        StatusCode = (int)PNG_StatsuCodeEnum.SessionExpired,
                        StatusMessage = "SessionExpired"
                    });
                }

                if (tpCheck.Locked)
                {
                    return (false, new PNG_BaseResponse()
                    {
                        StatusCode = (int)PNG_StatsuCodeEnum.AccountLocked,
                        StatusMessage = "User Account Locked"
                    });
                }
            }

            return (true, null);
        }

        private decimal GetJackpotsLossAmount(PNGJackpots pngJackpots)
        {
            if (pngJackpots == null || !pngJackpots.PNGJackpot.Any()) return 0;
            return pngJackpots.PNGJackpot.Sum(r => r.Loss);
        }

        public decimal GetJackpotsWinAmount(PNGJackpots pngJackpots)
        {
            if (pngJackpots == null || !pngJackpots.PNGJackpot.Any()) return 0;
            return pngJackpots.PNGJackpot.Sum(r => r.Win);
        }

        public void ValidateBetTrans(BettransType ticketType, IEnumerable<BettingDataRNG_PNG> bettingData, string transactionId)
        {
            switch (ticketType)
            {
                case BettransType.Bet:

                    // 檢查 BetTrans 是否有相同 transactionId 的投注
                    if (bettingData.Any(r => r.TransactionId == transactionId && r.TicketType == (int)BettransType.Bet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "TransactionId Bet Is Duplicated");

                    // 檢查 BetTrans 已存在 CancelBet，不能投注成功
                    if (bettingData.Any(r => r.TransactionId == transactionId && r.TicketType == (int)BettransType.CancelBet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "TransactionId CancelBet Already Exist");

                    break;

                case BettransType.CancelBet:

                    // 檢查 BetTrans 是否有相同 transactionId 是否Cancel 過，測試案例要回 OK
                    if (bettingData.Any(r => r.TransactionId == transactionId && r.TicketType == (int)BettransType.CancelBet))
                        throw new PNGException(PNG_StatsuCodeEnum.Ok, "TransactionId Duplicated Cancel");

                    break;
                case BettransType.Payout:

                    // 檢查 BetTrans 是否有相同 transactionId 的取消投注紀錄
                    if (bettingData.Any(r => r.TransactionId == transactionId && r.TicketType == (int)BettransType.CancelBet))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "TransactionId Already Cancel");

                    //  檢查 BetTrans 是否有相同 transactionId 的派彩過紀錄
                    if (bettingData.Any(r => r.TransactionId == transactionId && r.TicketType == (int)BettransType.Payout))
                        throw new PNGException(PNG_StatsuCodeEnum.Internal, "TransactionId Duplicated Release");

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
                    throw new PNGException(PNG_StatsuCodeEnum.Internal, "PNG TicketType NotImplemented");
            }
        }

        private TransType GetPayoutTransType(IEnumerable<BettingDataRNG_PNG> bettingData)
        {
            // 如果有下注紀錄，表示是一般派彩，如果沒有下注紀錄直接派彩，表示是免費旋轉贏錢
            return bettingData.Any(r => r.TicketType == (int)BettransType.Bet)
                ? TransType.Payout
                : TransType.Prize;
        }

        #endregion
    }
}
