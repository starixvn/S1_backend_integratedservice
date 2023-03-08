using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class TFGController : ControllerBase
    {
        #region Fields

        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        //private readonly int gameType = 0;
        private readonly Tools tools;
        private readonly ILogger<TFGController> logger;
        private readonly IHostEnvironment environment;
        #endregion

        #region Constructors
        public TFGController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<TFGController>>();
            thisTp = Tp.TFG;
            environment = provider.GetRequiredService<IHostEnvironment>();
        }
        #endregion

        #region model
        public class ValidateReq
        {
            public string token { get; set; }
        }
        public class ValidateResp 
        {
            public string loginName { get; set; }
        }
        #endregion

        #region
        [Route("[controller]/token/validate")]
        [HttpPost]
        public IActionResult Validate(ValidateReq req)
        {
            try
            {
                logger.LogInformation($"token\r\n{req.token}");
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.token)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                logger.LogInformation($"authToken\r\n{authToken}");
                logger.LogInformation($"sessionToken\r\n{sessionToken}");
                logger.LogInformation($"tpUserName\r\n{tpUserName}");
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    return StatusCode(400);
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return StatusCode(400);
                ValidateResp result = new ValidateResp();
                result.loginName = tpUserName;
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError($"Validate\r\n{JsonConvert.SerializeObject(req)}\r\n{ex.ToString()}");
                return StatusCode(400);
            }           
        }
        #endregion
    }
}
