using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class V8Controller : ControllerBase
    {
        #region Response

        public class GetApiResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
        }

        public class GetBalanceResp
        {
            public int code { get; set; }
            public decimal money { get; set; }
        }

        #endregion

        private readonly ILogger<V8Controller> _logger;
        private readonly V8BLL _v8BLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly Dictionary<string, V8Config> _siteConfig;

        public V8Controller(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<V8Controller>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _v8BLL = provider.GetRequiredService<V8BLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.V8;
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<V8Config>((Tp.V8).ToString());
        }

        [HttpGet]
        [Route("[controller]/seamlesswallet")]
        public dynamic SeamlessWalletAsync([FromQuery] ApiReq getReq)
        {
            string logName = $"{_thisTp}";
            GetApiResp getApiResponse = new GetApiResp
            {
                s = 0,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };
            try
            {
                if (string.IsNullOrEmpty(getReq.agent))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Agent;
                    _log2File.Error($"Agent Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                if (string.IsNullOrEmpty(getReq.param))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var param = DecryptParam(getReq.param, getReq.agent);

                if (string.IsNullOrEmpty(param))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var query = HttpUtility.ParseQueryString(param);
                var json = JsonConvert.SerializeObject(query.AllKeys.ToDictionary(y => y, y => query[y]));
                _log2File.Info($"{json}", logName);
                var req = JsonConvert.DeserializeObject<CommonP>(json);
                getApiResponse.s = req.s;

                if (!Enum.IsDefined(typeof(SubTypeOfV8), req.s))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Subtype;
                    _log2File.Error($"Subtype Error {req.s}", logName);
                    return getApiResponse;
                }

                var verifyKey = false;
                if (!string.IsNullOrEmpty(getReq.key))
                {
                    verifyKey = CheckKey(getReq.key, getReq.timestamp, getReq.agent);
                }
                if (!verifyKey)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Md5;
                    _log2File.Error($"CheckKey Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                switch (req.s)
                {
                    case (int)SubTypeOfV8.GetBalance:
                        return _v8BLL.GetBalance(json);
                    case (int)SubTypeOfV8.RequestBet:
                        return _v8BLL.PlaceBet(getReq, json);
                    case (int)SubTypeOfV8.ReturnBalance:
                        return _v8BLL.Payout(getReq, json);
                    case (int)SubTypeOfV8.GetOrderStatus:
                        return _v8BLL.GetOrderStatus(getReq, json);
                    case (int)SubTypeOfV8.CancelBet:
                        return _v8BLL.CancelBet(getReq, json);
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/CancelBetByOrderIdOfPostman")]
        public dynamic CancelBetByOrderIdOfPostman([FromQuery] CancelBetOfPostmanReq getReq)
        {
            string logName = $"{_thisTp}_CancelBetByOrderIdOfPostman";
            GetApiResp getApiResponse = new()
            {
                s = (int)SubTypeOfV8.CancelBet,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            try
            {
                if (string.IsNullOrEmpty(getReq.currency))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Format;
                    _log2File.Error($"Currency Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                //getReq.agent = GetAgentByCurrency(getReq.currency);
                getReq.s = (int)SubTypeOfV8.CancelBet;
                getReq.key = EncryptKey(getReq.timestamp, getReq.agent);

                if (string.IsNullOrEmpty(getReq.agent))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Agent;
                    _log2File.Error($"Agent Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                if (string.IsNullOrEmpty(getReq.orderId))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Format;
                    _log2File.Error($"orderId Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var verifyKey = false;
                if (!string.IsNullOrEmpty(getReq.key))
                {
                    verifyKey = CheckKey(getReq.key, getReq.timestamp, getReq.agent);
                }
                if (!verifyKey)
                {
                    getApiResponse.d.code = (int)ErrorCodeOfV8.Md5;
                    _log2File.Error($"CheckKey Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var param = new OrderDataPK_V8
                {
                    s = getReq.s,
                    orderId = getReq.orderId,
                    timestamp = getReq.timestamp
                };

                var bettrans = _v8BLL.GetBettransByOrderId(param);
                if (bettrans == null)
                {
                    _log2File.Error($"bettrans Error", logName);
                    return getApiResponse;
                }

                bettrans.s = getReq.s;
                var req = new ApiReq
                {
                    agent = getReq.agent,
                    param = "",
                    timestamp = getReq.timestamp,
                    key = ""
                };

                return _v8BLL.CancelBet(req, JsonConvert.SerializeObject(bettrans));
            }
            catch (Exception ex)
            {
                _log2File.Error($"{ex.Message}", logName, ex);
            }

            _log2File.Error($"Unknow Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        private string DecryptParam(string getReq, string agent)
        {
            var param = getReq;

            try
            {
                var deskey = _siteConfig.Select(x => x.Value).SelectMany(y=> y.CurrencyData.Values).ToList().Find(x=>x.Agent == agent).Deskey;
                param = _tools.AesDecrypt(HttpUtility.UrlEncode(getReq), deskey);

                _logger.LogInformation(param);
            }
            catch (Exception)
            {
                _logger.LogError($"DecryptParam Error\r\nkey={getReq}");
            }

            return param;
        }

        private bool CheckKey(string key, long timestamp, string agent)
        {
            var md5key = _siteConfig.Select(x => x.Value).SelectMany(y => y.CurrencyData.Values).ToList().Find(x => x.Agent == agent).Md5key;

            var encryptKey = _tools.ToMD5($"{agent}{timestamp}{md5key}").ToLower();
            if (key.ToLower() == encryptKey)
            {
                return true;
            }

            _logger.LogError($"CheckKey Error\r\nkey={key}\r\nencryptKey={encryptKey}");
            return false;
        }

        private string EncryptKey(long timestamp, string agent)
        {
            var md5key = _siteConfig.Select(x => x.Value).SelectMany(y => y.CurrencyData.Values).ToList().Find(x => x.Agent == agent).Md5key;

            return _tools.ToMD5($"{agent}{timestamp}{md5key}").ToLower();
        }

        //private string GetAgentByCurrency(string currency)
        //{
        //    var currencyAgentData = _config.CurrencyData[currency.ToUpper()];
        //    var agent = currencyAgentData.Agent;

        //    return agent;
        //}

    }
}
