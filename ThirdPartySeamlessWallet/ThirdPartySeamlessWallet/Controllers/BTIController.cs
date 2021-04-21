using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BTIController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<CFController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public BTIController(IHostEnvironment environment, ILogger<CFController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            thisTp = Tp.BTI;
            gameType = (int)GameType.BTI_Sports;
            this.environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> ValidateToken([FromQuery] string auth_token)
        {
            try
            {
                // get token
                if (string.IsNullOrEmpty(auth_token))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", $"Query auth_token is null." },
                    });
                }
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(auth_token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                var authToken = sessionKeyArr[0]; /* 1 Time*/
                var sessionToken = sessionKeyArr[1];/* Long Time*/

                // check token
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", $"authToken invalid." },
                    });
                }

                // check tp status and sessionToken
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                if (tpCheck == null)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", "tpCheck fail." },
                    });
                }
                if (tpCheck.Locked)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-6" },
                        { "error_message", "Restricted customer." },
                    });
                }
                if (tpCheck.Token != sessionToken)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", "sessionToken expire." },
                    });
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "cust_id", tpCustInfoAuth.CustId.ToString()},
                    { "balance", tpCustBalance.Balance.ToString()},
                    { "cust_login", tpCustInfoAuth.TpUserName },
                    { "city", "" },
                    { "country", GetCountryCode(tpCustInfoAuth.TpCurrencyId)},
                    { "currency_code", tpCustBalance.TpCurrencyId},
                });
            }
            catch (NotSupportedException)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-6" },
                    { "error_message", "Restricted customer." },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount)
        {
            try
            {
                
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount, [FromQuery] string req_id)
        {
            try
            {
                using var requestBody = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
                var content = await requestBody.ReadToEndAsync();
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CancelReserve([FromQuery] string cust_id, [FromQuery] string reserve_id)
        {
            try
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CommitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id)
        {
            try
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount)
        {
            try
            {
                using var requestBody = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
                var content = await requestBody.ReadToEndAsync();
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreditCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount)
        {
            try
            {
                using var requestBody = new StreamReader(HttpContext.Request.Body, Encoding.UTF8, true, 1024, true);
                var content = await requestBody.ReadToEndAsync();
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                });
            }
            catch (Exception ex)
            {
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"{ex.Message} || {ex.StackTrace}" },
                });
            }
        }

        private ContentResult ReturnBTI(Dictionary<string, string> dic)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var item in dic)
            {
                sb.AppendLine($"{item.Key}={item.Value}");
            }
            return Content(sb.ToString());
        }

        private string GetCountryCode(string TpCurrencyId)
        {
            return TpCurrencyId switch
            {
                "VND" => "VN",
                _ => throw new NotSupportedException()
            };

        }
    }
}
