using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Renci.SshNet.Common;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
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
        private readonly ILogger<SBOBLL> _logger;

        private Dictionary<SBOProductType, GameType> _sboGameTypeMap = new Dictionary<SBOProductType, GameType>()
        {
            { SBOProductType.SportsBook, GameType.SBO_Sports }
        };

        public SBOBLL(ThirdPartyBLL thirdPartyBll, TpCustInfoBLL tpCustInfoBll, ILogger<SBOBLL> logger, BettingService bettingService, SettledService settledService)
        {
            ThirdParty = Tp.SBO;
            _thirdPartyBLL = thirdPartyBll;
            _tpCustInfoBLL = tpCustInfoBll;
            _logger = logger;
            _bettingService = bettingService;
            _settledService = settledService;
        }

        public SBOBalanceResponse GetBalance(SBOBalanceRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)ThirdParty, request.Username);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new SBOException(SBOErrorCode.MemberNotExist, $"{request.Username} is locked");
            }
            
            var gameType = _sboGameTypeMap[request.ProductType];
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, request.Username, (int)gameType);
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
            throw new SBOException(SBOErrorCode.NotEnoughBalance,
                $"{ThirdParty} {gameType}: {request.Username} get balance failed");
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

            var result = new SBOResult();
            if (request.ProductType == SBOProductType.SportsBook)
            {
                var placeBetDto = new PlaceBetSportDto_SBO()
                {
                    CompanyKey = request.CompanyKey,
                    Username = request.Username,
                    Amount = request.Amount * tpCurrency.ExchangeRate,
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
                };
                result = await _bettingService.PlaceBetSport_SBO(tpCustInfo, placeBetDto);
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                -1 => throw new SBOException(SBOErrorCode.BetWithSameRefNoExists),
                0 => throw new SBOException(SBOErrorCode.NotEnoughBalance),
                1 => new SBODeductResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount / tpCurrency.ExchangeRate,
                    BetAmount = request.Amount,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                _ => throw new SBOException(SBOErrorCode.InternalError)
            };
        }

        public async Task<SBOSettleResponse> Settle(SBOSettleRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.Username);
            request.WinLoss *= tpCurrency.ExchangeRate;
            
            var result = new SBOResult();
            if (request.ProductType == SBOProductType.SportsBook)
            {
                var payoutDto = new PayoutSportDto_SBO()
                {
                    CompanyKey = request.CompanyKey,
                    Username = request.Username,
                    ProductType = (int)request.ProductType,
                    WinLoss = request.WinLoss * tpCurrency.ExchangeRate,
                    TransferCode = request.TransferCode,
                    CommissionStake = request.CommissionStake,
                    GameResult = request.GameResult,
                    ResultType = (int)request.ResultType,
                    ResultTime = request.ResultTime,
                    Gpid = request.Gpid,
                };
                result = await _settledService.PayoutSports_SBO(tpCustInfo, payoutDto);
            }
            else
            {
                throw new SBOException(SBOErrorCode.InternalError);
            }

            return result.Result switch
            {
                1 => new SBOSettleResponse()
                {
                    AccountName = request.Username,
                    Balance = result.AfterAmount / tpCurrency.ExchangeRate,
                    ErrorCode = (int)SBOErrorCode.NoError,
                    ErrorMessage = SBOErrorCode.NoError.GetEnumDescription()
                },
                -1 => throw new SBOException(SBOErrorCode.BetWithSameRefNoExists),
                0 => throw new SBOException(SBOErrorCode.BetAlreadySettled),
                _ => throw new SBOException(SBOErrorCode.InternalError)
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