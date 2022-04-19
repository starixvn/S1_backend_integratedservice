using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AllbetController : ControllerBase
    {
        private readonly Tools tools;
        private readonly Log2File log2File;
        private readonly ILogger<AllbetController> _logger;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;
        private readonly int gameType = 0;
        private static IAllbetBll _AllbetBll { get; set; }
        

        public AllbetController(IServiceProvider provider)
        {
            tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<AllbetController>>();
            log2File = provider.GetRequiredService<Log2File>();
            thisTp = Tp.Allbet;
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            gameType = (int)GameType.Allbet_LiveCasino;
            _AllbetBll = new AllbetBll(provider);
        }
        public class Resp
        {
            public string error_code { get; set; }
            public string message { get; set; }
            public decimal balance { get; set; }
        }
        public class TransferReq : BettingDataLC_Allbet
        {

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
                _logger.LogInformation($"authorization={authorization}, date={date}, signString={signString}, signature={signature}");
                
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
                _logger.LogInformation($"authorization={authorization}, accept={accept}, md5={md5}, contentType={contentType}, date={date}, signString={signString}, signature={signature}");

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