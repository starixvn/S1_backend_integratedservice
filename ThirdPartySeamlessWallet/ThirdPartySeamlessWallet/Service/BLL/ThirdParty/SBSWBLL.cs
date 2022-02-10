using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class SBSWBLL : ISBSWBLL, IThirdPartyBLL
    {
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingService bettingService;
        private readonly SettledService settledService;
        private readonly ILogger<SBSWBLL> _logger;
        public readonly ThirdPartySetting thirdPartySetting;
        private readonly Dictionary<string, Func<SBSWRequest, Task<SBSWReturnObj>>> _requestHandler;


        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }

        public SBSWBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.SBSW;
            LogContext.PushProperty("TP", ThirdParty.ToString());
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingService = provider.GetRequiredService<BettingService>();
            settledService = provider.GetRequiredService<SettledService>();
            thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)ThirdParty);
            _requestHandler = new Dictionary<string, Func<SBSWRequest, Task<SBSWReturnObj>>>()
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

        public async Task<SBSWReturnObj> SeamlessWallet(string merchant, string method, string bodyText)
        {
            try
            {
                var config = new SBSWConfig(thirdPartySetting.TpSetting, merchant);
                var request = JsonConvert.DeserializeObject<SBSWRequest>(bodyText);
                if (config.VendorId != request.key)
                {
                    throw new SBSWException(SBSWReturnCode.OperatorNotFound, $"Invalid key | key is:{request.key}");
                }

                return await _requestHandler[method.ToLower()].Invoke(request);
            }
            catch (SBSWException ex)
            {
                return new SBSWReturnObj(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new SBSWReturnObj(SBSWReturnCode.UnknownError);
            }
        }

        private async Task<SBSWReturnObj> GetBalance(SBSWRequest request)
        {
            var (tpCustInfo, _) = GetTPInfo((string)request.message.userId);
            var gameType = (int)GameType.SBSW_Sports;//SBSW使用專用的單一錢包Bonus類別，所以直接帶固定gameType
            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)ThirdParty, tpCustInfo.TpUserName, gameType);
            return new SBSWBalanceObj(SBSWReturnCode.Success)
            {
                balance = tpCustBalance.Balance,
                balanceTs = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(-4, 0, 0)).ToString("yyyy-MM-ddTHH:mm:ss.fffzz:00"),
                userId = tpCustInfo.TpUserName,
            };
        }

        private async Task<SBSWReturnObj> PlaceBet(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> ConfirmBet(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> CancelBet(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> Settle(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> Resettle(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }
        
        private async Task<SBSWReturnObj> Unsettle(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }
        
        private async Task<SBSWReturnObj> PlaceBetParlay(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> ConfirmBetParlay(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> PlaceBet3rd(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> ConfirmBet3rd(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private async Task<SBSWReturnObj> HealthCheck(SBSWRequest request)
        {
            return new SBSWReturnObj(SBSWReturnCode.Success);
        }

        private (TpCustInfo, TpCurrency) GetTPInfo(string tpUserName)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                throw new SBSWException(SBSWReturnCode.UserNotFound, $"Customer:{tpUserName} not found.");
            }
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)ThirdParty, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate < 0)
            {
                throw new SBSWException(SBSWReturnCode.UnSupportedCurrency, $"CurrencyId:{tpCustInfo.CurrencyId} not supported.");
            }
            return (tpCustInfo, tpCurrency);
        }

    }
}
