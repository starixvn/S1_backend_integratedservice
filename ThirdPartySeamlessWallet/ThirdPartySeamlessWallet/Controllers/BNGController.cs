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
    [Route("[controller]")]
    public class BNGController : ControllerBase
    {
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly AuthBLL _authBLL;
        private readonly IBNGBLL _bNGBLL;
        //private readonly Tools _tools;
        //private readonly Dictionary<string, string> _defaultConfig;
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
            //_tools = provider.GetRequiredService<Tools>();
            //_defaultConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<Dictionary<string, string>>(ThirdParty.ToString());
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

            return req.name switch
            {
                "login" => Authenticate(authToken, sessionToken, tpUserName, req),
                "transaction" => Transaction(tpUserName, req),
                "rollback" => Rollback(tpUserName, req),
                _ => GetBalance(tpUserName, req)
            };

            //if (req.name == ActionName.login.ToString())
            //{
            //    if (!_authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
            //        return Ok(new BNGResponse()
            //        {
            //            uid = req.uid,
            //            error = new error()
            //            {
            //                code = ErrorCode.INVALID_TOKEN.ToString()
            //            }
            //        });

            //    var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)ThirdParty, tpUserName);
            //    if (tpCheck == null || tpCheck.Token != sessionToken)
            //        return Ok(new BNGResponse()
            //        {
            //            uid = req.uid,
            //            error = new error()
            //            {
            //                code = ErrorCode.INVALID_TOKEN.ToString()
            //            }
            //        });

            //    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, tpUserName, gameType);
            //    return Ok(new BNGResponse()
            //    {
            //        uid = req.uid,
            //        player = new player()
            //        {
            //            id = tpUserName,
            //            brand = tpCustInfoAuth.SiteId.ToString(),
            //            currency = tpCustInfoAuth.TpCurrencyId.ToString(),
            //            mode = "REAL",
            //            //is_test = tpCustInfoAuth.IsTest,
            //            is_test = true,
            //        },
            //        balance = new balance()
            //        {
            //            value = tpCustBalance.Balance.ToString(),
            //            version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //        }
            //    });
            //}
            //else if (req.name == ActionName.transaction.ToString())
            //{
            //    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, tpUserName);
            //    var result = _bNGBLL.Transaction(tpCustInfo, req, out decimal afterAmount);

            //    if (result == (int)TransactionStatus.Success)
            //    {
            //        return Ok(new BNGTransactionResponse()
            //        {
            //            uid = req.uid,
            //            balance = new balance()
            //            {
            //                value = afterAmount.ToString(),
            //                version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //            }
            //        });
            //    }
            //    else
            //    {
            //        return StatusCode(500, "Internal Server Error. Something went Wrong!");
            //    }
            //}
            //else if (req.name == ActionName.rollback.ToString())
            //{
            //    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.FaChai, tpUserName);
            //    var result = _bNGBLL.Rollback(tpCustInfo, req, out decimal afterAmount);

            //    if (result == (int)TransactionStatus.Success)
            //    {
            //        return Ok(new BNGTransactionResponse()
            //        {
            //            uid = req.uid,
            //            balance = new balance()
            //            {
            //                value = afterAmount.ToString(),
            //                version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //            }
            //        });
            //    }
            //    else
            //    {
            //        return StatusCode(500, "Internal Server Error. Something went Wrong!");
            //    }
            //}
            //else
            //{
            //    var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.FaChai, tpUserName, gameType);

            //    return Ok(new BNGTransactionResponse()
            //    {
            //        uid = req.uid,
            //        balance = new balance()
            //        {
            //            value = tpCustBalance.Balance.ToString(),
            //            version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            //        }
            //    });
            //}
        }

        #region private methord
        private IActionResult Authenticate(string authToken, string sessionToken, string tpUserName, BNGRequest req)
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
            return Ok(new BNGAuthenticateResponse()
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
                },
                tag = ""
            });
        }
        private IActionResult Transaction(string tpUserName, BNGRequest req)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.BNG, tpUserName);
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
        private IActionResult Rollback(string tpUserName, BNGRequest req)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.BNG, tpUserName);
            var result = _bNGBLL.Rollback(tpCustInfo, req, out decimal afterAmount);

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
        private IActionResult GetBalance(string tpUserName, BNGRequest req)
        {
            var tpCustBalance = _tpCustInfoBLL.GetBalance((int)Tp.BNG, tpUserName, gameType);

            return Ok(new BNGTransactionResponse()
            {
                uid = req.uid,
                balance = new balance()
                {
                    value = tpCustBalance.Balance.ToString(),
                    version = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        }
        #endregion
    }
}
