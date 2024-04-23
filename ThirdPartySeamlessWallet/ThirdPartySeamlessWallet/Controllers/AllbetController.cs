using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
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
    public class AllbetController : ControllerBase
    {
        private readonly ILogger<AllbetController> _logger;
        private static IAllbetBll _AllbetBll { get; set; }
        private readonly AllBetConfig _siteAllBetConfig;


        public AllbetController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<AllbetController>>();
            _AllbetBll = new AllbetBll(provider);
            _siteAllBetConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<AllBetConfig>(Tp.Allbet.ToString());
        }

        [HttpGet]
        [Route("[controller]/{method}/{username}")]
        public async Task<dynamic> GetBalanceAsync([FromRoute] string method, [FromRoute] string username)
        {
            try
            {
                if (!Enum.TryParse(method, true, out AllbetType transType))
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, $"Not Support {method} method.");
                }

                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Date", out var date);
                
                var signString = $"GET\n\n\n{date}\n/{method}/{username}";
                var signature = GetSign(signString, _siteAllBetConfig.PartnerKey);

                CheckSign(authorization, signature);

                return await _AllbetBll.SeamlessWallet(transType, username);
            }
            catch (AllbetException ex)
            {
                return new AllBetTransferResponse(ex.ReturnCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        [HttpPost]
        [Route("[controller]/{method}")]
        public async Task<dynamic> TransferAsync([FromRoute] string method)
        {
            try
            {
                if (!Enum.TryParse(method, true, out AllbetType transType))
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters,
                        $"Not Support {method} method.");
                }

                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Content-MD5", out var md5);
                Request.Headers.TryGetValue("Content-Type", out var contentType);
                Request.Headers.TryGetValue("Date", out var date);
                Request.Headers.TryGetValue("Internal", out var @internal);
                
                var signString = $"POST\n{md5}\n{contentType}\n{date}\n/{transType.ToString()}";
                var signature = GetSign(signString, _siteAllBetConfig.PartnerKey);

                if (@internal.ToString() != "Wager")
                {
                    CheckSign(authorization,signature);
                }

                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var bodyText = await reader.ReadToEndAsync();
                return await _AllbetBll.SeamlessWallet(transType, bodyText);
            }
            catch (AllbetException ex)
            {
                return new AllBetTransferResponse(ex.ReturnCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private void CheckSign(StringValues authorization, string signature)
        {
            var auth = authorization.ToString();
            var operatorId = "";
            if (auth.Length > 0)
            {
                operatorId = auth.Substring(3, auth.IndexOf(":", StringComparison.Ordinal) - 3);
            }

            if (operatorId != _siteAllBetConfig.OperatorId)
            {
                throw new AllbetException(AllbetResultCode.InvalidOperatorId);
            }

            if (authorization != $"AB {_siteAllBetConfig.OperatorId}:{signature}")
            {
                throw new AllbetException(AllbetResultCode.InvalidSignature);
            }
        }

        private string GetSign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Convert.FromBase64String(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));

            return Convert.ToBase64String(hash);
        }


        #region Model

        private class AllBetConfig
        {
            public string ApiUrl { get; set; }
            public string Suffix { get; set; }
            public string AllbetKey { get; set; }
            public string OperatorId { get; set; }
            public string PartnerKey { get; set; }
            public List<CurrencyAgent> CurrencyAgents { get; set; }
        }
        
        public class CurrencyAgent
        {
            public string CurrencyId { get; set; }
            public string Agent { get; set; }
        }

        #endregion
    }
}