using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using System.Text.Json.Serialization;
using ThirdPartySeamlessWallet.Service.Model;
using Newtonsoft.Json;
using System.Text;
using System.Web;
using ThirdPartySeamlessWallet.Service.Model.SBSW;
using System.Drawing.Drawing2D;
using Pipelines.Sockets.Unofficial.Buffers;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BNGController : ControllerBase
    {
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly AuthBLL _authBLL;
        private readonly IBNGBLL _bNGBLL;
        private readonly Tools _tools;
        private readonly Dictionary<string, string> _defaultConfig;
        public Tp ThirdParty { get; set; }
        public int gameType { get; set; }

        #region Constructors
        public BNGController(IServiceProvider provider)
        {
            ThirdParty = Tp.BNG;
            gameType = (int)GameType.BNG_RNG;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _bNGBLL = provider.GetRequiredService<IBNGBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(ThirdParty.ToString());
        }
        #endregion


        [HttpPost]
        public IActionResult POST(BNGRequest req)
        {
            var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.token)));
            var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
            string authToken = sessionKeyArr[0];
            string sessionToken = sessionKeyArr[1];
            string tpUserName = sessionKeyArr[2];
            
            if (req.name == ActionName.login.ToString())
            {
                if (!_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    return Ok(new BNGResponse()
                    {
                        uid = req.uid,
                        error = new error()
                        {
                            code = ErrorCode.INVALID_TOKEN.ToString()
                        }
                    });

                var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return Ok(new BNGResponse()
                    {
                        uid = req.uid,
                        error = new error()
                        {
                            code = ErrorCode.INVALID_TOKEN.ToString()
                        }
                    });

                var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, tpUserName, gameType);
                return Ok(new BNGResponse()
                {
                    uid = req.uid,
                    player = new player()
                    {
                        id = tpUserName,
                        brand = tpCustInfoAuth.SiteId.ToString(),
                        currency = tpCustInfoAuth.TpCurrencyId.ToString(),
                        mode = "REAL",
                        is_test = tpCustInfoAuth.IsTest,
                    },
                    balance = new balance()
                    {
                        value = tpCustBalance.Balance.ToString(),
                        version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                    }
                });
            }

            if (req.name == ActionName.transaction.ToString())
            {
                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, tpUserName);
                var result = _bNGBLL.Transaction(tpCustInfo, req, out decimal afterAmount);

                if (result == (int)TransactionStatus.Success)
                {
                    return Ok(new BNGTransactionResponse()
                    {
                        uid = req.uid,
                        balance = new balance()
                        {
                            value = afterAmount.ToString(),
                            version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        }
                    });
                }
                else
                {
                    return StatusCode(500, "Internal Server Error. Something went Wrong!");
                }
            }

            return Ok();
        }
    }
}
