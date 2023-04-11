using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using Newtonsoft.Json;
using System.Text;
using System.Web;

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
        }
        #endregion

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
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {
                    _logger.LogError($"privateToken:{privateToken}\r\nauthorization{authorization}");
                    return BadRequest();
                }

                var retult = await _tfgbLL.Transfer(req);

                if(retult.Code == ResultCode.Success)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError(retult.Message);
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
                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting((int)_thisTp);
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)_thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {
                    _logger.LogError($"privateToken:{privateToken}\r\nauthorization{authorization}");
                    return BadRequest();
                }

                var retult = await _tfgbLL.Rollback(req);

                if (retult.Code == ResultCode.Success)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError(retult.Message);
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
