using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.TpBetRecordExtend;

namespace ThirdParty.SPRIBE
{
    public class TpHelper : ThirdPartyHelperBase
    {

        #region SPRIBE Configs Setting

        Dictionary<TpGameType, int> _productWallet = new Dictionary<TpGameType, int>()
        {
            {TpGameType.SPRIBE_RNG, 101},
        };

        #endregion

        #region Response

        private class RegisterResponse
        {
            public int Code { get; set; }
            public string Message { get; set; } = "";
            public string Currency { get; set; } = "";
        }

        private class LaunchGameResponse
        {
            public int Code { get; set; }
            public string Message { get; set; } = "";
            public string GameUrl { get; set; } = "";
            public string SessionToken { get; set; } = "";
            public int TTL { get; set; }
        }

        #endregion

        private readonly IReadOnlyDictionary<string, string> _gameNameList;
        private const string ContentType = "application/json";
        private readonly WebProxy _proxy;

        public TpHelper(IServiceProvider provider)
        {
            _gameNameList = TpBetRecordProvider.GetGameCasinoList(53).ToDictionary(d => d.AppId, d => d.Name);
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            var configuration = provider.GetRequiredService<IConfiguration>();
            if (env.EnvironmentName == "Development")
            {
                var proxyHost = configuration.GetValue<string>("configProxyHost");
                var proxyPort = configuration.GetValue<int>("configProxyPort");
                _proxy = new WebProxy(proxyHost, proxyPort);
            }
        }

        public override ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var apiUrl = $"{inputs["ApiUrl"]}/Player/Register";

            tpCustInfo.TpUserName = tpCustInfo.TpUserName.Replace("_", "x").ToLower();
            var signUp = new
            {
                MerchantCode = inputs["Key"],
                PlayerId = tpCustInfo.TpUserName,
                Currency = tpCustInfo.TpCurrencyId,
                Password = tpCustInfo.TpPwd
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy);
            var result = FromJson<RegisterResponse>(json);

            if (result.Code == 0)
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Success,
                    Message = result.Message
                };
            }
            else
            {
                return new ApiResult()
                {
                    ErrorCode = ErrorCode.Fail,
                    Message = result.Message
                };
            }
        }

        public override ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs)
        {
            var apiUrl = $"{inputs["ApiUrl"]}/Game/NewLaunchGame";
            var productWallet = _productWallet[TpGameType.SPRIBE_RNG];
            var fun = inputs.ContainsKey("fun") && !string.IsNullOrEmpty(inputs["fun"]) ? inputs["fun"] : "0";

            var playerName = tpCustInfo.TpUserName;
            if (fun.Equals("1", StringComparison.OrdinalIgnoreCase))
            {
                apiUrl = $"{inputs["ApiUrl"]}/Game/LaunchFreeGame";
                playerName = null;
            }

            var signUp = new
            {
                MerchantCode = inputs["Key"],
                PlayerId = playerName,
                GameCode = inputs["gameId"],
                Language = TpLangId,
                IpAddress = inputs["ClientIP"],
                ProductWallet = productWallet,
                LobbyURL = inputs["homeUrl"]
            };

            var json = HttpPostWithProxy(apiUrl, ToJson(signUp), ContentType, _proxy, null);
            var result = FromJson<LaunchGameResponse>(json);

            return new ApiResult
            {
                ErrorCode = ErrorCode.Success,
                Message = result.GameUrl,
                RawMessage = result.Message
            };
        }

        public override ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            var oApiResult = new ApiResult<UserBalance>
            {
                ErrorCode = ErrorCode.Fail
            };
            return oApiResult;
        }

        public override TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID)
        {
            throw new NotImplementedException();
        }

        public override BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        public override TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException();
        }

        private string GetGameName(string gameCode)
        {
            if (!_gameNameList.TryGetValue(gameCode, out var name))
            {
                return string.Empty;
            }
            var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(name);
            return dic.TryGetValue("en", out var result) ? result : string.Empty;
        }

    }
}
