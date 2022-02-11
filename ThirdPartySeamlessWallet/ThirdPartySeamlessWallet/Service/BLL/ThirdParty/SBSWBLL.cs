using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class SBSWBLL : ISBSWBLL, IThirdPartyBLL
    {
        private readonly MySqlClient _mainDbClient;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;
        private readonly ILogger<SBSWBLL> _logger;
        public readonly ThirdPartySetting _thirdPartySetting;
        private readonly Dictionary<string, Func<BaseReq, Task<ReturnObj>>> _requestHandler;


        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public SBSWBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.SBSW;
            LogContext.PushProperty("TP", ThirdParty.ToString());
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
            _thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);
            _requestHandler = new Dictionary<string, Func<BaseReq, Task<ReturnObj>>>()
            {
                { "getbalance", GetBalance},
                { "placebet", PlaceBet},
                { "confirmbet", ConfirmBet},
                { "cancelbet", CancelBet},
                { "settle", Settle},
                { "resettle", Resettle},
                { "unsettle", Unsettle},
                { "placebetparlay", PlaceBetParlay},
                { "confirmbetparlay", ConfirmBetParlay},
                { "placebet3rd", PlaceBet3rd},
                { "confirmbet3rd", ConfirmBet3rd},
                { "healthcheck", HealthCheck},
            };
            _logger = provider.GetRequiredService<ILogger<SBSWBLL>>();
        }

        public async Task<ReturnObj> SeamlessWallet(string merchant, string method, string bodyText)
        {
            try
            {
                var config = new Config(_thirdPartySetting.TpSetting, merchant);
                _logger.LogInformation($"{method}:{bodyText}");
                var request = JsonConvert.DeserializeObject<BaseReq>(bodyText);
                if (config.VendorId != request.key)
                {
                    throw new SBSWException(ReturnCode.InvalidVendorId, $"Invalid key | key is:{request.key}");
                }

                return await _requestHandler[method.ToLower()].Invoke(request);
            }
            catch (SBSWException ex)
            {
                return new ReturnObj(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError);
            }
        }

        private async Task<ReturnObj> GetBalance(BaseReq request)
        {
            var (tpCustInfo, _) = GetTPInfo((string)request.message.userId);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return new BalanceObj(ReturnCode.Success)
            {
                balance = tpCustBalance.Balance,
                balanceTs = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-ddTHH:mm:ss.fffzz:00"),
                userId = tpCustInfo.TpUserName,
            };
        }

        private async Task<ReturnObj> PlaceBet(BaseReq request)
        {
            try
            {
                var placeBet = JsonConvert.DeserializeObject<PlaceBetReq>((string)JsonConvert.SerializeObject(request.message));
                return new PlaceBetObj(ReturnCode.Success)
                {
                    licenseeTxId = Guid.NewGuid().ToString(),
                    refId = placeBet.refId,
                };
                var (tpCustInfo, tpCurrency) = GetTPInfo((string)request.message.userId);
                placeBet.betAmount *= tpCurrency.ExchangeRate;
                placeBet.actualAmount *= tpCurrency.ExchangeRate;
                placeBet.creditAmount *= tpCurrency.ExchangeRate;
                placeBet.debitAmount *= tpCurrency.ExchangeRate;

                TpUserCheck(tpCustInfo.TpUserName);
                var check = _mainDbClient.Query<string>("Service_Bettrans_SBSW_Check", parameters: placeBet);
                if (check.Any())
                {
                    throw new SBSWException(ReturnCode.TransactionDuplicated);
                }

                var (gameType, tpCategory) = GetGameType(placeBet.sportType);

                using var conn = _mainDbClient.GetMySqlConnection();
                conn.Open();
                var trans = conn.BeginTransaction();
                var result = await _mainDbClient.QueryFirstAsync<long>("Service_Bettrans_SBSW_Ins", trans, placeBet);

                if (result > 0 && placeBet.actualAmount != 0)
                {
                    var bonusId = 0;
                    result = _bettingService.PlaceBetV2(
                        tpCustInfo,
                        Math.Abs(placeBet.actualAmount) * -1,
                        (int)ThirdParty,
                        placeBet.refId,
                        (int)gameType,
                        placeBet.refId,
                        null,
                        tpCategory,
                        out var afterAmount,
                        ref bonusId,
                        conn,
                        trans);
                }

                if (result > 0)
                {
                    trans.Commit();
                }
                return new PlaceBetObj(ReturnCode.Success)
                {
                    licenseeTxId = Guid.NewGuid().ToString(),
                    refId = placeBet.refId,
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError,ex.Message);
            }      
        }

        private async Task<ReturnObj> ConfirmBet(BaseReq request)
        {
            var (tpCustInfo, _) = GetTPInfo((string)request.message.userId);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return new ConfirmBetObj(ReturnCode.Success)
            {
                balance = tpCustBalance.Balance
            };
        }

        private async Task<ReturnObj> CancelBet(BaseReq request)
        {
            var (tpCustInfo, _) = GetTPInfo((string)request.message.userId);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return new ConfirmBetObj(ReturnCode.Success)
            {
                balance = tpCustBalance.Balance
            };
        }

        private async Task<ReturnObj> Settle(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> Resettle(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }
        
        private async Task<ReturnObj> Unsettle(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }
        
        private async Task<ReturnObj> PlaceBetParlay(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> ConfirmBetParlay(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> PlaceBet3rd(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> ConfirmBet3rd(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private async Task<ReturnObj> HealthCheck(BaseReq request)
        {
            return new ReturnObj(ReturnCode.Success);
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new SBSWException(ReturnCode.UserNotFound, $"Customer:{tpUserName} not found.");
            }

            var tpCurrency = _thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new SBSWException(ReturnCode.UnSupportedCurrency, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }

        private void TpUserCheck(string tpUserName)
        {
            var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
            if (tpCheck == null)
            {
                throw new SBSWException(ReturnCode.InvalidToken, $"{tpUserName} is logout.");
            }
            if (tpCheck.Locked)
            {
                throw new SBSWException(ReturnCode.UserLocked, $"{tpUserName} is locked.");
            }
        }

        private (GameType gameType, TpCategory tpCategory) GetGameType(int sportType)
        {
            if (sportType > 0 && sportType < 100)
            {
                return (GameType.SBSW_Sports, TpCategory.Sports);
            }

            switch (sportType)
            {
                //Number Game
                case 161:
                case 164:
                case 168:
                    return (GameType.SBSW_NumberGame, TpCategory.Lottery);
                case 180:
                case 181:
                case 182:
                case 183:
                case 184:
                case 185:
                case 186:
                    return (GameType.SBSW_VirtualSports, TpCategory.VirtualSports);
                case 190:
                case 191:
                case 192:
                case 193:
                case 194:
                case 195:
                case 196:
                case 197:
                case 199:
                    return (GameType.SBSW_VirtualSports2, TpCategory.VirtualSports);
                case 202:
                    return (GameType.SBSW_Keno, TpCategory.Lottery);
                case 220:
                    return (GameType.SBSW_Lottery, TpCategory.Lottery);
                case 222:
                    return (GameType.SBSW_TableGame, TpCategory.Lottery);
                default:
                    throw new SBSWException(ReturnCode.InvalidParameters, $"sportType:{sportType} not supported.");
            }    
        }

    }
}
