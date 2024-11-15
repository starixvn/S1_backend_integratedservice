using System.Web;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.KY;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class KYController : ControllerBase
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
        #endregion

        private readonly ILogger<KYController> _logger;
        private readonly KYBLL _kyBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp _thisTp;
        private readonly Dictionary<string, KYConfig> _siteConfig;

        public KYController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<KYController>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _kyBLL = provider.GetRequiredService<KYBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _thisTp = Tp.KY;
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<KYConfig>((Tp.KY).ToString());
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
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Agent;
                    _log2File.Error($"Agent Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                if (string.IsNullOrEmpty(getReq.param))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var param = DecryptParam(getReq.param, getReq.agent);

                if (string.IsNullOrEmpty(param))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Decryption;
                    _log2File.Error($"Decryption Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                var query = HttpUtility.ParseQueryString(param);
                var json = JsonConvert.SerializeObject(query.AllKeys.ToDictionary(y => y, y => query[y]));
                _log2File.Info($"{json}", logName);

                var req = JsonConvert.DeserializeObject<CommonP>(json);
                getApiResponse.s = req.s;

                if (!Enum.IsDefined(typeof(SubTypeOfKY), req.s))
                {
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Subtype;
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
                    getApiResponse.d.code = (int)ErrorCodeOfKY.Md5;
                    _log2File.Error($"CheckKey Error {JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                switch (req.s)
                {
                    case (int)SubTypeOfKY.RequestBet:
                        return _kyBLL.PlaceBet(getReq, json);
                    case (int)SubTypeOfKY.ReturnBalance:
                        return _kyBLL.Payout(getReq, json);
                    case (int)SubTypeOfKY.CancelBet:
                        return _kyBLL.CancelBet(getReq, json);
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

        private string DecryptParam(string getReq, string agent)
        {
            var param = getReq;

            try
            {
                var deskey = _siteConfig
                    .Select(x => x.Value)
                    .Where(y => y.CurrencyData != null)
                    .SelectMany(y => y.CurrencyData.Values)
                    .FirstOrDefault(x => x.Agent == agent)?.Deskey;

                if (deskey == null)
                {
                    throw new Exception("Deskey not found for the specified agent.");
                }

                param = _tools.AesDecrypt(HttpUtility.UrlEncode(getReq), deskey);

                _logger.LogInformation(param);
            }
            catch (Exception)
            {
                _logger.LogError($"DescryptParam Error \r\nkey={getReq}");
            }

            return param;
        }

        private bool CheckKey(string key, long timestamp, string agent)
        {
            var md5key = _siteConfig
                .Select(x => x.Value)
                .Where(y => y.CurrencyData != null)
                .SelectMany(y => y.CurrencyData.Values)
                .FirstOrDefault(x => x.Agent == agent)?.Md5key;

            if (md5key == null)
            {
                throw new Exception("MD5key not found for the specified agent.");
            }

            var encryptKey = _tools.ToMD5($"{agent}{timestamp}{md5key}").ToLower();
            if (key.ToLower() == encryptKey)
            {
                return true;
            }

            _logger.LogError($"ChecKey Error\r\nkey{key}\r\nencryptKey={encryptKey}");
            return false;
        }
    }
}
