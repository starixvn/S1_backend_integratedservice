using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class MGBLL : IMGBLL, IThirdPartyBLL
    {
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<MGBLL> _logger;
        private readonly Dictionary<MGTxnType, Func<MGUpdatebalanceRequest, Task<MGUpdatebalanceResponse>>> _processRequestHandler;

        public MGBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.MG;
            GameType = GameType.MG_RNG;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<MGBLL>>();
            _processRequestHandler =
                new Dictionary<MGTxnType, Func<MGUpdatebalanceRequest, Task<MGUpdatebalanceResponse>>>()
                {
                    {MGTxnType.DEBIT, Debit},
                    {MGTxnType.CREDIT, Credit},
                    {MGTxnType.ROLLBACK, Rollback},
                };
        }

        public MGLoginResponse Login(MGLoginRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.playerId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new MGException(MGErrorCode.PlayerIsLocked, $"{request.playerId} is locked");
            }

            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.playerId);
            var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)ThirdParty);

            var tpCustBalance =
                _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, request.playerId, gameType);
            if (tpCustBalance != null)
            {
                return new MGLoginResponse()
                {
                    balance = tpCustBalance.Balance,
                    currency = tpCustBalance.TpCurrencyId
                };
            }
            throw new MGException(MGErrorCode.BalanceFailed,
                $"{ThirdParty} {gameType}: {request.playerId} get balance failed");
        }

        public MGGetBalanceResponse GetBalance(MGGetBalanceRequest request)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, request.playerId);
            var gameType = _tpCustInfoBLL.GetCustGotoGameType(tpCustInfo.CustId, (int)ThirdParty);

            var tpCustBalance =
                _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, request.playerId, gameType);
            if (tpCustBalance != null)
            {
                return new MGGetBalanceResponse()
                {
                    balance = tpCustBalance.Balance,
                    currency = tpCustBalance.TpCurrencyId
                };
            }
            throw new MGException(MGErrorCode.BalanceFailed,
                $"{ThirdParty} {gameType}: {request.playerId} get balance failed");
        }

        public async Task<MGUpdatebalanceResponse> UpdateBalance(MGUpdatebalanceRequest request)
        {
            var trnType = (MGTxnType)Enum.Parse(typeof(MGTxnType), request.txnType);
            return await _processRequestHandler[trnType].Invoke(request);
        }

        private async Task<MGUpdatebalanceResponse> Debit(MGUpdatebalanceRequest request)
        {
            // 檢查玩家
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.playerId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new MGException(MGErrorCode.PlayerIsLocked, $"{request.playerId} is locked");
            }

            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            if (tpCurrency.ExchangeRate > 0)
                request.amount *= tpCurrency.ExchangeRate;

            var bettingData = new BettingDataRNG_MG()
            {
                txnType = request.txnType,
                txnEventType = request.txnEventType,
                playerId = request.playerId,
                amount = request.amount,
                currency = request.currency,
                txnId = request.txnId,
                creationTime = request.creationTime,
                creationTimeMs = ConvertCreationTimeMs(request.creationTimeMs),
                contentCode = request.contentCode,
                betId = request.betId,
                roundId = request.roundId,
                deviceType = request.deviceType,
                platformType = request.platformType,
                completed = request.completed,
            };

            MGPlaceBetResult placeBetResult;

            var gameType = GetGameType(request.channel);
            if (gameType == (int)GameType.MG_RNG)
            {
                placeBetResult = await _bettingService.PlaceBetRNG_MG(tpCustInfo, bettingData, gameType);
            }
            else if (gameType == (int)GameType.MG_LiveCasino)
            {
                placeBetResult = _bettingService.PlaceBetLC_MG(tpCustInfo, bettingData, gameType);
            }
            else
            {
                throw new MGException(MGErrorCode.GameTypeError, $"gameType error:{gameType}");
            }

            // 餘額不足
            if (placeBetResult.AfterAmount < 0)
            {
                throw new MGException(MGErrorCode.InsufficientBalance,
                    $"betting data: {JsonConvert.SerializeObject(bettingData)} and afterAmount:{placeBetResult.AfterAmount}");
            }
            // DB 錯誤
            if (placeBetResult.Result <= 0)
            {
                throw new MGException(MGErrorCode.SystemError);
            }

            if (placeBetResult.AfterAmount == 0)
            {
                placeBetResult.AfterAmount = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)gameType).Balance;
            }
            else
            {
                if (tpCurrency.ExchangeRate > 0)
                    placeBetResult.AfterAmount /= tpCurrency.ExchangeRate;
            }

            return new MGUpdatebalanceResponse()
            {
                extTxnId = placeBetResult.ExtTxnId.ToString(),
                currency = request.currency,
                balance = placeBetResult.AfterAmount
            };
        }

        private async Task<MGUpdatebalanceResponse> Credit(MGUpdatebalanceRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            if (tpCurrency.ExchangeRate > 0)
                request.amount *= tpCurrency.ExchangeRate;

            var bettingData = new BettingDataRNG_MG()
            {
                txnType = request.txnType.ToString(),
                txnEventType = request.txnEventType,
                playerId = request.playerId,
                amount = request.amount,
                currency = request.currency,
                txnId = request.txnId,
                creationTime = request.creationTime,
                creationTimeMs = ConvertCreationTimeMs(request.creationTimeMs),
                contentCode = request.contentCode,
                betId = request.betId,
                roundId = request.roundId,
                deviceType = request.deviceType,
                platformType = request.platformType,
                completed = request.completed,
                transNum = request.transNum
            };

            MGPayoutResult payoutResult;

            var gameType = GetGameType(request.channel);
            if (gameType == (int)GameType.MG_RNG)
            {
                payoutResult = await _settledService.PayoutRNG_MG(tpCustInfo, bettingData, gameType);
            }
            else if (gameType == (int)GameType.MG_LiveCasino)
            {
                payoutResult = _settledService.PayoutLC_MG(tpCustInfo, bettingData, gameType);
            }
            else
            {
                throw new MGException(MGErrorCode.GameTypeError, $"gameType error:{gameType}");
            }

            // 餘額不足
            if (payoutResult.AfterAmount < 0)
            {
                throw new MGException(MGErrorCode.InsufficientBalance,
                    $"betting data: {JsonConvert.SerializeObject(bettingData)} and afterAmount:{payoutResult.AfterAmount}");
            }
            // DB 錯誤
            if (payoutResult.Result <= 0)
            {
                throw new MGException(MGErrorCode.SystemError);
            }

            if (payoutResult.AfterAmount == 0)
            {
                payoutResult.AfterAmount = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)gameType).Balance;
            }
            else
            {
                if (tpCurrency.ExchangeRate > 0)
                    payoutResult.AfterAmount /= tpCurrency.ExchangeRate;
            }

            return new MGUpdatebalanceResponse()
            {
                extTxnId = payoutResult.ExtTxnId.ToString(),
                currency = request.currency,
                balance = payoutResult.AfterAmount
            };
        }

        public async Task<MGRollbackResponse> Rollback(MGRollbackeRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            if (tpCurrency.ExchangeRate > 0)
                request.amount *= tpCurrency.ExchangeRate;

            var cancelData = new CancelDataRNG_MG()
            {
                betId = request.betId,
                txnId = request.txnId,
                playerId = request.playerId,
                amount = request.amount,
                creationTime = DateTime.UtcNow.Ticks,
                creationTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            };

            MGCancelResult cancelResult;

            // check bettrans of gametype by table
            var tpGameType = _settledService.GetTpGameType_MG(request.txnId, cancelData.creationTimeMs);
            if (tpGameType == 0)
            {
                throw new MGException(MGErrorCode.GameTypeError, $"gameType error:{tpGameType}, betId:{request.betId}");
            }

            var gameType = tpGameType;
            if (gameType == (int)GameType.MG_RNG)
            {
                cancelResult = await _settledService.CancelBetRNG_MG(tpCustInfo, cancelData, gameType);
            }
            else if (gameType == (int)GameType.MG_LiveCasino)
            {
                cancelResult = await _settledService.CancelBetLC_MG(tpCustInfo, cancelData, gameType);
            }
            else
            {
                throw new MGException(MGErrorCode.GameTypeError, $"gameType error:{gameType}");
            }

            // DB 錯誤
            if (cancelResult.Result <= 0)
            {
                throw new MGException();
            }

            if (cancelResult.AfterAmount == 0)
            {
                cancelResult.AfterAmount = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)GameType).Balance;
            }
            else
            {
                if (tpCurrency.ExchangeRate > 0)
                    cancelResult.AfterAmount /= tpCurrency.ExchangeRate;
            }

            return new MGRollbackResponse()
            {
                currency = tpCurrency.TpCurrencyId,
                balance = cancelResult.AfterAmount,
                extTxnId = cancelResult.ExtTxnId.ToString()
            };
        }

        private async Task<MGUpdatebalanceResponse> Rollback(MGUpdatebalanceRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            if (tpCurrency.ExchangeRate > 0)
                request.amount *= tpCurrency.ExchangeRate;

            var cancelData = new CancelDataRNG_MG()
            {
                betId = request.betId,
                txnId = request.txnId,
                playerId = request.playerId,
                amount = request.amount,
                creationTime = request.creationTime,
                creationTimeMs = ConvertCreationTimeMs(request.creationTimeMs),
            };

            MGCancelResult cancelResult;

            var gameType = GetGameType(request.channel);
            if (gameType == (int)GameType.MG_RNG)
            {
                cancelResult = await _settledService.CancelBetRNG_MG(tpCustInfo, cancelData, gameType);
            }
            else if (gameType == (int)GameType.MG_LiveCasino)
            {
                cancelResult = await _settledService.CancelBetLC_MG(tpCustInfo, cancelData, gameType);
            }
            else
            {
                throw new MGException(MGErrorCode.GameTypeError, $"gameType error:{gameType}");
            }

            // DB 錯誤
            if (cancelResult.Result <= 0)
            {
                throw new MGException();
            }

            if (cancelResult.AfterAmount == 0)
            {
                cancelResult.AfterAmount = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)gameType).Balance;
            }
            else
            {
                if (tpCurrency.ExchangeRate > 0)
                    cancelResult.AfterAmount /= tpCurrency.ExchangeRate;
            }

            return new MGUpdatebalanceResponse()
            {
                currency = tpCurrency.TpCurrencyId,
                balance = cancelResult.AfterAmount,
                extTxnId = cancelResult.ExtTxnId.ToString()
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new MGException(MGErrorCode.SystemError, $"tpCustInfo is null, tpUserName:{tpUserName}");
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                throw new MGException(MGErrorCode.SystemError, $"tpCurrency is null, {JsonConvert.SerializeObject(tpCustInfo)}");
            }

            return (tpCustInfo, tpCurrency);
        }

        private static int GetGameType(string channel = null)
        {
            int gameType;
            if (!string.IsNullOrEmpty(channel) && string.Equals(channel, "LIVEDEALER", StringComparison.OrdinalIgnoreCase))
            {
                gameType = (int)GameType.MG_LiveCasino;
            }
            else
            {
                gameType = (int)GameType.MG_RNG;
            }
            return gameType;
        }

        private static long ConvertCreationTimeMs(long creationTimeMs)
        {
            var leng = creationTimeMs.ToString().Length;
            if (leng == 13)
            {
                return creationTimeMs;
            }
            else if (leng == 10)
            {
                return creationTimeMs * 1000;
            }
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}