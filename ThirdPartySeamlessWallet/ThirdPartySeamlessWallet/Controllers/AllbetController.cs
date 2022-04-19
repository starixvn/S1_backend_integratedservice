using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AllbetController : ControllerBase
    {
        private readonly ILogger<AllbetController> _logger;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private static IAllbetBll _AllbetBll { get; set; }
        

        public AllbetController(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<AllbetController>>();
            thisTp = Tp.Allbet;
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _AllbetBll = new AllbetBll(provider);
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

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var signString = $"GET\n\n\n{date}\n/{method}/{username}";
                var signature = Sign(signString, thirdPartySetting.TpSetting["PartnerKey"]);
                
                if (authorization != $"AB {thirdPartySetting.TpSetting["OperatorId"]}:{signature}")
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidSignature);
                }

                return await _AllbetBll.SeamlessWallet(transType, username);
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
                    return new AllBetTransferResponse(AllbetResultCode.InvalidParameters, $"Not Support {method} method.");
                }

                Request.Headers.TryGetValue("Authorization", out var authorization);
                Request.Headers.TryGetValue("Accept", out var accept);
                Request.Headers.TryGetValue("Content-MD5", out var md5);
                Request.Headers.TryGetValue("Content-Type", out var contentType);
                Request.Headers.TryGetValue("Date", out var date);

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var signString = $"POST\n{md5}\n{contentType}\n{date}\n/Transfer";
                var signature = Sign(signString, thirdPartySetting.TpSetting["PartnerKey"]);

                if (authorization != $"AB {thirdPartySetting.TpSetting["OperatorId"]}:{signature}")
                {
                    return new AllBetTransferResponse(AllbetResultCode.InvalidSignature);
                }

                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var bodyText = await reader.ReadToEndAsync();
                return await _AllbetBll.SeamlessWallet(transType, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new AllBetTransferResponse(AllbetResultCode.SystemError);
            }
        }

        private string Sign(string encryptText, string encryptKey)
        {
            using var hmacsha1 = new HMACSHA1(Convert.FromBase64String(encryptKey));
            var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));

            return Convert.ToBase64String(hash);
        }
    }
}