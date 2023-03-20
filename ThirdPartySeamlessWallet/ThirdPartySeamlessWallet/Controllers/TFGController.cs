using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using System.Text;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
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
        private readonly Tp thisTp;
        private readonly Tools tools;
        private readonly ILogger<TFGController> logger;
        private readonly IHostEnvironment environment;
        private readonly ITFGBLL _tfgbLL;
        #endregion

        #region Constructors
        public TFGController(IServiceProvider provider, ITFGBLL tfgbll)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<TFGController>>();
            thisTp = Tp.TFG;
            environment = provider.GetRequiredService<IHostEnvironment>();
            _tfgbLL = tfgbll;
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
                //logger.LogInformation($"token\r\n{req.token}");
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.token)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                //logger.LogInformation($"authToken\r\n{authToken}");
                //logger.LogInformation($"sessionToken\r\n{sessionToken}");
                //logger.LogInformation($"tpUserName\r\n{tpUserName}");
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

        public class TFGWallet
        {
            public Decimal balance { get; set; }
        }
        [HttpGet("[controller]/wallet")]
        public IActionResult GetBalance([FromQuery] string loginName)
        {
            try
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, loginName, (int)GameType.TFG_Esports);
                TFGWallet result = new TFGWallet() { balance = tpCustBalance.Balance };
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError($"GetBalance\r\n{loginName}\r\n{ex.ToString()}");
                return StatusCode(400);
            }
        }

        

        [Route("[controller]/transfer")]
        [HttpPost]
        public async Task<IActionResult> TransferAsync(TFGTransferReq req)
        {
            try
            {
                logger.LogInformation($"Transfer\r\n{req}");
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {
                    logger.LogInformation($"privateToken\r\n{privateToken}");
                    logger.LogInformation($"authorization\r\n{authorization}");
                    return StatusCode(400);
                }

                var result = await _tfgbLL.Transfer(req);
                return Ok();
            }
            catch (Exception ex)
            {
                logger.LogError($"Transfer\r\n{ex.ToString()}");
                return StatusCode(400);
            }

            //return Ok();
        }

        [Route("[controller]/rollback")]
        [HttpPost]
        public IActionResult Rollback(TFGTransferReq req)
        {
            try
            {
                logger.LogInformation($"Rollback\r\n{req}");
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {

                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Rollback\r\n{ex.ToString()}");
                return StatusCode(400);
            }

            return Ok();
        }
        #endregion
    }
}
