using System.Text;
using System.Web;

using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class TFGController : Controller
    {
        #region Fields

        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly Tp _thisTp;
        private readonly ILogger<TFGController> _logger;
        private readonly ITFGBLL _tfgbLL;
        private readonly Dictionary<int, TFGConfig> _siteConfig;
        #endregion

        #region Constructors
        public TFGController(IServiceProvider provider, ITFGBLL tfgbll)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<TFGController>>();
            _thisTp = Tp.TFG;
            _tfgbLL = tfgbll;
            _siteConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<TFGConfig>(Tp.TFG.ToString());
        }
        #endregion

        public class TFGConfig : TpSiteConfig
        {
            public string RetrieveUrl { get; set; }
            public Dictionary<string, CurrencyDataTFG> CurrencyData { get; set; }
        }
        public class CurrencyDataTFG
        {
            public string PartnerId { get; set; }
            public string PublicToken { get; set; }
            public string PrivateToken { get; set; }
        }

        #region Action
        [Route("[controller]/token/validate")]
        [HttpPost]
        public IActionResult Validate(ValidateReq req)
        {
            try
            {
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.token)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                if (!_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    return BadRequest();
                var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)_thisTp, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return BadRequest();
                ValidateResp result = new ValidateResp();
                result.loginName = tpUserName;
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Validate\r\n{ex.ToString()}");
                return BadRequest();
            }
        }

        [HttpGet("[controller]/wallet")]
        public IActionResult GetBalance([FromQuery] string loginName)
        {
            try
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, loginName, (int)GameType.TFG_Esports);
                TFGWallet result = new TFGWallet() { balance = tpCustBalance.Balance };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetBalance\r\n{ex.ToString()}");
                return BadRequest();
            }
        }

        [Route("[controller]/transfer")]
        [HttpPost]
        public async Task<IActionResult> TransferAsync(TFGTransferReq req)
        {
            try
            {
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.loginName);
                var config = _siteConfig[tpCustInfo.SiteId];
                var privateToken = config.CurrencyData[tpCustInfo.TpCurrencyId].PrivateToken;
                if (privateToken != authorization)
                {
                    _logger.LogError($"privateToken:{privateToken}\r\nauthorization{authorization}");
                    return BadRequest();
                }

                var result = await _tfgbLL.Transfer(req);

                if (result.Code == ResultCode.Success)
                {
                    _logger.LogInformation(ResultCode.Success.ToString());
                    return Ok();
                }
                else
                {
                    _logger.LogError(result.Message);
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Transfer\r\n{ex.ToString()}");
                return BadRequest();
            }
        }

        [Route("[controller]/rollback")]
        [HttpPost]
        public async Task<IActionResult> RollbackAsync(TFGRallBackReq req)
        {
            try
            {
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.loginName);
                var config = _siteConfig[tpCustInfo.SiteId];
                var privateToken = config.CurrencyData[tpCustInfo.TpCurrencyId].PrivateToken;
                if (privateToken != authorization)
                {
                    _logger.LogError($"privateToken:{privateToken}\r\nauthorization{authorization}");
                    return BadRequest();
                }

                var result = await _tfgbLL.Rollback(req);

                if (result.Code == ResultCode.Success)
                {
                    _logger.LogInformation(ResultCode.Success.ToString());
                    return Ok();
                }
                else
                {
                    _logger.LogError(result.Message);
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Rollback\r\n{ex.ToString()}");
                return BadRequest();
            }
        }

        #endregion
    }
}
