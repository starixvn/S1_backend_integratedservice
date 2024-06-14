using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.DAL.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{

    public class SBOBLL : ISBOBLL
    {
        public Tp ThirdParty { get; set; }
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ISBOService _sboService;
        private readonly ILogger<SBOBLL> _logger;
        private readonly Dictionary<int, SBOConfig> _siteConfigDic;
        private readonly string _tpName = Tp.SBO.ToString();

        public Dictionary<int, string> SBOStatusMap = new()
        {
            { 0, "running" },
            { 1, "settled " },
            { 2, "void " }
        };

        private readonly Dictionary<SBOProductType, GameType> _sboGameTypeMap = new()
        {
            { SBOProductType.SportsBook, GameType.SBO_Sports },
            { SBOProductType.VirtualSports, GameType.SBO_VirtualSports }
        };

        public SBOBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.SBO;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<SBOBLL>>();
            _sboService = provider.GetService<ISBOService>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<SBOConfig>(_tpName);
        }

        public SBOBalanceResponse GetBalance(SBOBalanceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username)) throw new SBOException(SBOErrorCode.UsernameEmpty);

            // 因CompanyKey都是同一組共用，只取第一筆即可，如要使用其他欄位請先確認是否可共用
            var thirdPartySetting = _siteConfigDic.FirstOrDefault().Value;
            if (thirdPartySetting == null)
            {
                _logger.LogError("siteConfig not found");
                throw new SBOException(SBOErrorCode.CompanyKeyError);
            }
            var companyKey = thirdPartySetting.CompanyKey;
            if (companyKey != request.CompanyKey) throw new SBOException(SBOErrorCode.CompanyKeyError);

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.Username);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist, $"{request.Username} is locked");
            }

            var gameType = request.ProductType == SBOProductType.LiveCoin ? 0 : _sboGameTypeMap[request.ProductType];
            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.Username, (int)gameType);
            if (tpCustBalance != null)
            {
                return new SBOBalanceResponse()
                {
                    AccountName = request.Username,
                    Balance = tpCustBalance.Balance,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                };
            }
            throw new SBOException(SBOErrorCode.InternalError, $"{gameType}: {request.Username} get balance failed");
        }

        public async Task<SBODeductResponse> Deduct(SBODeductRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.Username);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist);
            }

            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            var betStatus = await _sboService.GetSportsBetStatus(request.TransferCode);
            if (betStatus != null) throw new SBOException(SBOErrorCode.BetWithSameRefNoExists);

            SBOResult result;
            if (request.ProductType == SBOProductType.SportsBook || request.ProductType == SBOProductType.VirtualSports)
            {
                var placeBetDto = new PlaceBetSportDto_SBO()
                {
                    CompanyKey = request.CompanyKey,
                    Username = request.Username,
                    Amount = request.Amount.ConvertToMain(tpCurrency.ExchangeRate),
                    TransferCode = request.TransferCode,
                    TransactionId = request.TransactionId,
                    BetTime = request.BetTime,
                    ProductType = (int)request.ProductType,
                    GameId = request.GameId,
                    GamePeriodId = request.GamePeriodId,
                    GameRoundId = request.GameRoundId,
                    GameTypeName = request.GameTypeName,
                    OrderDetail = request.OrderDetail,
                    PlayerIp = request.PlayerIp,
                    Gpid = request.Gpid,
                    TpGameType = _sboGameTypeMap[request.ProductType]
                };
                result = await _bettingService.PlaceBetSport_SBO(tpCustInfo, placeBetDto);
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                1 => new SBODeductResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate),
                    BetAmount = request.Amount,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                -1 => throw new SBOException(SBOErrorCode.BetWithSameRefNoExists),
                -2 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public async Task<SBOSettleResponse> Settle(SBOSettleRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            var result = new SBOResult();
            if (request.ProductType == SBOProductType.SportsBook || request.ProductType == SBOProductType.VirtualSports)
            {
                var betStatus = await _sboService.GetSportsBetStatus(request.TransferCode);
                switch (betStatus)
                {
                    case null:
                        throw new SBOException(SBOErrorCode.BetNotExists);
                    case { Status: 1 }:
                        throw new SBOException(SBOErrorCode.BetAlreadySettled);
                    case { Status: 2 }:
                        throw new SBOException(SBOErrorCode.BetAlreadyCanceled);
                }
                var payoutDto = new PayoutSportDto_SBO()
                {
                    Username = request.Username,
                    WinLoss = request.WinLoss.ConvertToMain(tpCurrency.ExchangeRate),
                    TransferCode = request.TransferCode,
                    CommissionStake = request.CommissionStake,
                    GameResult = request.GameResult,
                    ResultType = (int)request.ResultType,
                    ResultTime = request.ResultTime,
                    Gpid = request.Gpid,
                    TpGameType = _sboGameTypeMap[request.ProductType]
                };
                result = await _settledService.PayoutSports_SBO(tpCustInfo, payoutDto);
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            if (result.Result == 1 && result.AfterAmount == 0)
            {
                var gameType = _sboGameTypeMap[request.ProductType];
                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.Username, (int)gameType);
                return new SBOSettleResponse()
                {
                    AccountName = request.Username,
                    Balance = tpCustBalance.Balance,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                };
            }

            return result.Result switch
            {
                1 => new SBOSettleResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate),
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                -1 => throw new SBOException(SBOErrorCode.BetAlreadySettled),
                -2 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                -3 => throw new SBOException(SBOErrorCode.InternalError),
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public async Task<SBOCancelResponse> Cancel(SBOCancelRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            var result = new SBOResult();
            if (request.ProductType == SBOProductType.SportsBook || request.ProductType == SBOProductType.VirtualSports)
            {
                var betStatus = await _sboService.GetSportsBetStatus(request.TransferCode);
                switch (betStatus)
                {
                    case null:
                        throw new SBOException(SBOErrorCode.BetNotExists);
                    case { Status: 2 }:
                        throw new SBOException(SBOErrorCode.BetAlreadyCanceled);
                    default:
                        {
                            var cancelDto = new CancelSportDto_SBO()
                            {
                                Username = request.Username,
                                TransferCode = request.TransferCode,
                                Amount = -1 * betStatus.WinLoss + betStatus.Stake,
                                ResultTime = betStatus.ResultTime,
                                Status = betStatus.Status,
                                TpGameType = _sboGameTypeMap[request.ProductType]
                            };
                            result = await _settledService.CancelBetSports_SBO(tpCustInfo, cancelDto);
                            break;
                        }
                }
            }
            else if (request.ProductType == SBOProductType.LiveCoin)
            {
                var liveCoinData = await _sboService.GetLiveCoinTransaction(request.TransferCode);
                switch (liveCoinData)
                {
                    case null:
                        throw new SBOException(SBOErrorCode.BetNotExists);
                    case { Status: 2 }:
                        throw new SBOException(SBOErrorCode.BetAlreadyCanceled);
                    default:
                        {
                            var cancelDto = new CancelSportDto_SBO()
                            {
                                TransferCode = request.TransferCode,
                                Amount = liveCoinData.Amount
                            };
                            result = await _settledService.CancelBetLiveCoin_SBO(tpCustInfo, cancelDto);
                            break;
                        }
                }
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                1 => new SBOCancelResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate),
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                -1 => throw new SBOException(SBOErrorCode.BetAlreadyCanceled),
                -2 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                -3 => throw new SBOException(SBOErrorCode.InternalError),
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public async Task<SBORollbackResponse> Rollback(SBORollbackRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            var result = new SBOResult();
            if (request.ProductType == SBOProductType.SportsBook || request.ProductType == SBOProductType.VirtualSports)
            {
                var betStatus = await _sboService.GetSportsBetStatus(request.TransferCode);
                switch (betStatus)
                {
                    case null:
                        throw new SBOException(SBOErrorCode.BetNotExists);
                    default:
                        {
                            var rollbackDto = new RollbackSportDto_SBO()
                            {
                                Username = request.Username,
                                TransferCode = request.TransferCode,
                                Amount = CaculateRollbackAmount(betStatus),
                                ResultTime = betStatus.ResultTime,
                                Status = betStatus.Status,
                                TpGameType = _sboGameTypeMap[request.ProductType]
                            };
                            result = await _settledService.RollbackSport_SBO(tpCustInfo, rollbackDto);
                            break;
                        }
                }
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                1 => new SBORollbackResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount.ConvertToTp(tpCurrency.ExchangeRate),
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                -1 => throw new SBOException(SBOErrorCode.BetAlreadyRollback),
                -2 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                -3 => throw new SBOException(SBOErrorCode.InternalError),
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public async Task<SBOGetBetStatusResponse> GetBetStatus(SBOGetBetStatusRequest request)
        {
            if (string.IsNullOrEmpty(request.TransferCode))
            {
                throw new SBOException(SBOErrorCode.BetNotExists);
            }
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            if (request.ProductType == SBOProductType.SportsBook || request.ProductType == SBOProductType.VirtualSports)
            {
                var data = await _sboService.GetSportsBetStatus(request.TransferCode);
                if (data == null) throw new SBOException(SBOErrorCode.BetNotExists);

                var betStatus = new SBOGetBetStatusResponse()
                {
                    TransferCode = request.TransferCode,
                    TransactionId = request.TransactionId,
                    Stake = data.Stake / tpCurrency.ExchangeRate,
                    WinLoss = data.WinLoss / tpCurrency.ExchangeRate,
                    Status = SBOStatusMap[data.Status],
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                };
                return betStatus;
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }
        }

        public async Task<SBOLiveCoinTransactionResponse> LiveCoinTransaction(SBOLiveCoinTransactionRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.Username);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist);
            }

            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);

            var liveCoinData = await _sboService.GetLiveCoinTransaction(request.TransferCode);
            if (liveCoinData != null) throw new SBOException(SBOErrorCode.BetWithSameRefNoExists);

            var result = new SBOResult();
            if (request.ProductType == SBOProductType.LiveCoin)
            {
                var liveCoinDto = new LiveCoinTransactionDto_SBO()
                {
                    CompanyKey = request.CompanyKey,
                    Username = request.Username,
                    Amount = request.Amount * tpCurrency.ExchangeRate,
                    TransferCode = request.TransferCode,
                    TransactionId = request.TransactionId,
                    TransactionTime = request.TransactionTime,
                    ProductType = (int)request.ProductType,
                    Selection = request.Selection
                };
                result = await _bettingService.TransferLiveCoin_SBO(tpCustInfo, liveCoinDto);
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                -1 => throw new SBOException(SBOErrorCode.BetWithSameRefNoExists),
                0 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                1 => new SBOLiveCoinTransactionResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount / tpCurrency.ExchangeRate,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public decimal CaculateRollbackAmount(SBOGetBetStatusData betData)
        {
            return betData.Status switch
            {
                1 => betData.WinLoss,
                2 => betData.Stake,
                _ => throw new SBOException(SBOErrorCode.BetAlreadyRollback)
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                _logger.LogError($"tpCustInfo is null, tpUserName:{tpUserName}");
                throw new SBOException(SBOErrorCode.MemberNotExist);
            }
            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                _logger.LogError($"tpCurrency is null, {JsonConvert.SerializeObject(tpCustInfo)}");
                throw new SBOException(SBOErrorCode.InternalError);
            }
            return (tpCustInfo, tpCurrency);
        }
    }
}