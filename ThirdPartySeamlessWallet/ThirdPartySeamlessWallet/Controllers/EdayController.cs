using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Produces("application/json")]
    public class EdayController : ControllerBase
    {
        private readonly ILogger<EdayController> _logger;
        private static IEdayBLL _EdayBLL;
        private readonly Tp _thisTp;
        private readonly Dictionary<int, EdayConfig> _siteConfigDic;
        private readonly EdayConfig _defaultConfig; // SiteConfig的SiteId=0
        private readonly Dictionary<string, EdayConfig> _siteNameConfigs;


        public EdayController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<EdayController>>();
            _EdayBLL = provider.GetRequiredService<IEdayBLL>();
            _thisTp = Tp.Eday; 
            // defaultConfig給原本NOVA、GOAL、BWING的商號使用
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<EdayConfig>(_thisTp.ToString());
            // 新增商號使用指定siteName
            _siteNameConfigs = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteName<EdayConfig>(_thisTp.ToString());

        }

        [HttpGet]
        [Route("[controller]/edApi/GetPlayerBalance/{username}")]
        [Route("[controller]/{siteName}/edApi/GetPlayerBalance/{username}")]
        public IActionResult GetBalanceAsync([FromRoute] string username, string siteName = "")
        {
            var baseResult = BaseResult();
            try
            {
                Request.Headers.TryGetValue("Authorization", out var authorization);
                
                // var parameters = new
                // {
                //     player = username,
                // };
                //
                // var json = JsonConvert.SerializeObject(parameters);
                // //var testtest = Base64Md5(json);
                //
                // var signString = $"GET\n\n\n/edApi/GetPlayerBalance/{username}";
                // if (!CheckSign(signString, authorization, siteName,json))
                // {
                //     baseResult = new EdayResponse<EdayErrorResponse>()
                //     {
                //         resultCode = (int)EdayResultCode.InvalidSignature,
                //         message = EdayResultCode.InvalidSignature.GetEnumDescription()
                //     };
                //     return Ok(baseResult);
                // }
                
                var getBalanceResult = _EdayBLL.GetBalance(username);
                return Ok(getBalanceResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}", ex);
            }
            return Ok(baseResult);
        }

        [HttpPost]
        [Route("[controller]/edApi/BetRequest")]
        [Route("[controller]/{siteName}/edApi/BetRequest")]
        public IActionResult BetRequest(EdayBetRequest request,string siteName = "")
        {
            var baseResult = BaseResult();
            try
            {
                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Content-MD5", out var md5);
                Request.Headers.TryGetValue("Content-Type", out var contentType);
                
                var signString = $"POST\n{md5}\n{contentType}\n/edApi/BetRequest";
                var parameters = JsonConvert.SerializeObject(request);
                if (!CheckSign(signString, authorization, siteName,parameters))
                {
                    baseResult = new EdayResponse<EdayErrorResponse>()
                    {
                        resultCode = (int)EdayResultCode.InvalidSignature,
                        message = EdayResultCode.InvalidSignature.GetEnumDescription()
                    };
                    return Ok(baseResult);
                }

                //下注以及取消下注都走同一支，用BetType判斷， 1 = Bet , 2 = Cancel
                switch (request.betType)
                {
                    //Bet
                    case 1:
                        var betRequestResult = _EdayBLL.BetRequest(request);
                        return Ok(betRequestResult);
                    //Cancel Bet
                    case 2:
                        var betRequestCancelResult = _EdayBLL.BetRequestCancel(request);
                        return Ok(betRequestCancelResult);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}", ex);
            }
            return Ok(baseResult);
        }
        
        [HttpPost]
        [Route("[controller]/edApi/RoundUpdate")]
        [Route("[controller]/{siteName}/edApi/RoundUpdate")]
        public IActionResult RoundUpdate(EdayRoundUpdateRequest request,string siteName = "")
        {
            var baseResult = BaseResult();
            try
            {
                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Content-MD5", out var md5);
                Request.Headers.TryGetValue("Content-Type", out var contentType);
                
                var signString = $"POST\n{md5}\n{contentType}\n/edApi/RoundUpdate";
                var parameters = JsonConvert.SerializeObject(request);
                if (!CheckSign(signString, authorization, siteName,parameters))
                {
                    baseResult = new EdayResponse<EdayErrorResponse>()
                    {
                        resultCode = (int)EdayResultCode.InvalidSignature,
                        message = EdayResultCode.InvalidSignature.GetEnumDescription()
                    };
                    return Ok(baseResult);
                }
                
                var roundUpdateRequestResult = _EdayBLL.RoundUpdate(request);
                
                return Ok(roundUpdateRequestResult);
            }
            catch (Exception ex)
            {
                _logger.LogError($"{ex.Message}", ex);
            }
            return Ok(baseResult);
        }
        
        #region Private
        
        private string GetSign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));
            
            return Convert.ToBase64String(hash);
        }
        
        private bool CheckSign(string signString, string authorization, string siteName, string parameters = "")
        {
            var md5 = "";
            if (!string.IsNullOrEmpty(parameters))
            {
                md5 = Base64Md5(parameters);
            }
            var config = GetSiteConfig(siteName);
            var signature = GetSign(signString, config.ApiKey);
            return authorization == $"ED {config.Operator.ToLower()}:{signature}";
        }
        
        private EdayConfig GetSiteConfig(string siteName)
        {
            if (siteName == "")
            {
                return _defaultConfig;
            }
            return _siteNameConfigs.TryGetValue(siteName, out var result)
                ? result
                : _defaultConfig;
        }
        
        private string Base64Md5(string data)
        {
            return Convert.ToBase64String(Md5(Encoding.ASCII.GetBytes(data)));
        }
        private byte[] Md5(byte[] data)
        {
            using var md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }
        
        private EdayResponse<EdayErrorResponse> BaseResult()
        {
            return new EdayResponse<EdayErrorResponse>()
            {
                resultCode = (int)EdayResultCode.ServerError,
                message = EdayResultCode.ServerError.GetEnumDescription(),
                data = null
            };
        }
        
        #endregion
        
    }
}