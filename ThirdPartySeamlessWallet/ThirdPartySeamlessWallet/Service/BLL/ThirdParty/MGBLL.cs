using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        private Dictionary<string, string> Config;
        private readonly Dictionary<MGTxnType, Func<MGUpdatebalanceRequest, Task<MGUpdatebalanceResponse>>> _processRequestHandler;
        public MGBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.MG;
            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingService = provider.GetService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _logger = provider.GetService<ILogger<MGBLL>>();
            // Config = _thirdPartyBLL.GetThirdPartySetting((int) ThirdParty).TpSetting;
            _processRequestHandler =
                new Dictionary<MGTxnType, Func<MGUpdatebalanceRequest, Task<MGUpdatebalanceResponse>>>()
                {
                    {MGTxnType.DEBIT, Debit},
                    {MGTxnType.CREDIT, Credit}
                };
        }

        public MGLoginResponse Login(MGLoginRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int) ThirdParty, request.playerId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new MGException();
            }

            var tpCustBalance =
                _tpCustInfoBLL.GetBalanceByHalfSecCache((int) ThirdParty, request.playerId, (int) GameType);
            if (tpCustBalance != null)
            {
                return new MGLoginResponse()
                {
                    balance = tpCustBalance.Balance,
                    currency = tpCustBalance.TpCurrencyId
                };
            }
            throw new MGException();
        }

        public MGGetBalanceResponse GetBalance(MGGetBalanceRequest request)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int) ThirdParty, request.playerId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new MGException();
            }

            var tpCustBalance =
                _tpCustInfoBLL.GetBalanceByHalfSecCache((int) ThirdParty, request.playerId, (int) GameType);
            if (tpCustBalance != null)
            {
                return new MGGetBalanceResponse()
                {
                    balance = tpCustBalance.Balance,
                    currency = tpCustBalance.TpCurrencyId
                };
            }
            _logger.LogError($"get balance fail:{JsonConvert.SerializeObject(tpCheck)}");
            throw new MGException();
        }

        public async Task<MGUpdatebalanceResponse> UpdateBalance(MGUpdatebalanceRequest request)
        {
            var trnType = (MGTxnType)Enum.Parse(typeof(MGTxnType), request.txnType);
            return await _processRequestHandler[trnType].Invoke(request);
        }

        public async Task<MGRollbackResponse> Rollback(MGRollbackeRequest request)
        {
            throw new System.NotImplementedException();
        }

        private async Task<MGUpdatebalanceResponse> Debit(MGUpdatebalanceRequest request)
        {
            // 檢查玩家
            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int) ThirdParty, request.playerId);
            if (tpCheck == null || tpCheck.Locked)
            {
                _logger.LogError($"tpcheck fail:{JsonConvert.SerializeObject(tpCheck)}");
                throw new MGException();
            }
            
            // GameType = _thirdPartyBLL.GetGameType_PP(request.contentCode);
            GameType = GameType.MG_RNG;
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            request.amount *= tpCurrency.ExchangeRate;

            var result = 0;
            var afterAmount = 0M;
            var bettingData = new BettingDataRNG_MG()
            {
                txnType = request.txnType.ToString(),
                txnEventType = request.txnEventType,
                playerId = request.playerId,
                amount = request.amount,
                currency = request.currency,
                txnId = request.txnId,
                creationTime = request.creationTime,
                contentCode = request.contentCode,
                betId = request.betId,
                roundId = request.roundId,
                deviceType = request.deviceType,
                platformType = request.platformType,
                completed = request.completed
            };
            if (GameType == GameType.MG_RNG)
            {
                (result, afterAmount) = await _bettingService.PlaceBetRNG_MG(tpCustInfo, bettingData, (int) GameType);
            }
            else if (GameType == GameType.MG_LiveCasino)
            {
                (result, afterAmount) = await _bettingService.PlaceBetLC_MG(tpCustInfo, bettingData, (int) GameType);
            }
            
            if(afterAmount <0 )
            {
                throw new MGException();
            }
            if (result <= 0)
            {
                throw new MGException();
            } 

            
            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)GameType);
            
            return new MGUpdatebalanceResponse()
            {
                currency = tpCurrency.TpCurrencyId,
                balance = tpCustBalance.Balance
            };
        }
        
        private async Task<MGUpdatebalanceResponse> Credit(MGUpdatebalanceRequest request)
        {
            var (tpCustInfo, tpCurrency) = GetTPInfo(request.playerId);
            request.amount *= tpCurrency.ExchangeRate;
            
            var result = 0;
            var afterAmount = 0M;
            var bettingData = new BettingDataRNG_MG()
            {
                txnType = request.txnType.ToString(),
                txnEventType = request.txnEventType,
                playerId = request.playerId,
                amount = request.amount,
                currency = request.currency,
                txnId = request.txnId,
                creationTime = request.creationTime,
                contentCode = request.contentCode,
                betId = request.betId,
                roundId = request.roundId,
                deviceType = request.deviceType,
                platformType = request.platformType,
                completed = request.completed
            };
            // GameType = _thirdPartyBLL.GetGameType_PP(request.contentCode);
            GameType = GameType.MG_RNG;
            if (GameType == GameType.MG_RNG)
            {
                (result, afterAmount) = await _settledService.PayoutRNG_MG(tpCustInfo, bettingData, (int)GameType);
            }
            else if(GameType == GameType.MG_LiveCasino)
            {
                (result, afterAmount) = await _settledService.PayoutLC_MG(tpCustInfo, bettingData, (int)GameType);
            }
            
            if(afterAmount <0 )
            {
                throw new MGException();
            }
            if (result <= 0)
            {
                throw new MGException();
            } 
            
            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)ThirdParty, request.playerId, (int)GameType);
            
            return new MGUpdatebalanceResponse()
            {
                currency = tpCurrency.TpCurrencyId,
                balance = tpCustBalance.Balance
            };
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int) ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                _logger.LogError($"tpCustInfo is null, tpUserName:{tpUserName}");
                throw new MGException();
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int) ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency == null || tpCurrency.ExchangeRate < 0)
            {
                _logger.LogError($"tpCurrency is null, {JsonConvert.SerializeObject(tpCustInfo)}");
                throw new MGException();
            }

            return (tpCustInfo, tpCurrency);
        }
    }
}