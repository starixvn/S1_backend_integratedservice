using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BTIController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<BTIController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public BTIController(IHostEnvironment environment, ILogger<BTIController> logger, AuthBLL authBLL, ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            thisTp = Tp.BTI;
            gameType = (int)GameType.BTI_Sports;
            this.environment = environment;
            LogContext.PushProperty("TP", Tp.BTI.ToString());
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
                if (tpCustInfoAuth.TpCurrencyId == null)
                {
                    throw new NotSupportedException();
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
                    { "cust_id", tpCustInfoAuth.TpUserName},
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
                LogExceptionError(GetCurrentMethod(), ex);          
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Reserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount)
        {
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id) || amount <= 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-10" },
                        { "error_message", $"Wrong Request." },
                    });
                }

                var request = new BettingDataBTIReserve()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    amount = amount,
                };
                var result = bettingBLL.Reserve_BTI(request, out decimal tpAffterAmount, out long trx_id);

                if (result < 1)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-4" },
                        { "error_message", "InsufficientFunds" },
                    });
                }

                if (tpAffterAmount < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-4" },
                        { "error_message", "InsufficientFunds" },
                    });
                }

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", tpAffterAmount.ToString() },
                    { "trx_id", trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount, [FromQuery] string req_id)
        {
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
                var content = await requestBody.ReadToEndAsync();
                var request = new BettingDataBTIReserve()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    amount = amount,
                    req_id = req_id,
                    requestBody = content
                };
                _ = bettingBLL.DebitReserve_BTI(request, out decimal tpAffterAmount, out long trx_id);
                
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "trx_id", trx_id.ToString()},
                    { "balance", tpAffterAmount.ToString() },                   
                });
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CancelReserve([FromQuery] string cust_id, [FromQuery] string reserve_id)
        {
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-10" },
                        { "error_message", $"Wrong Request." },
                    });
                }

                var request = new BettingDataBTIReserve()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                };
                var result = settledBLL.CancelReserve_BTI(request, out decimal tpAffterAmount);

                if (result < 1)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "Reserve was not found." },
                    });
                }

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", tpAffterAmount.ToString() },
                });
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CommitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id)
        {
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-10" },
                        { "error_message", $"Wrong Request." },
                    });
                }

                var request = new BettingDataBTIReserve()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                };
                var result = settledBLL.CommitReserve_BTI(request, out decimal tpAffterAmount, out long trx_id);

                if (result < 1)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "0" },
                        { "error_message", "Reserve was not found." },
                    });
                }

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "0" },
                    { "error_message", "OK" },
                    { "balance", tpAffterAmount.ToString() },
                    { "trx_id", trx_id.ToString()},
                });
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount)
        {
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
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
                    { "error_message", ex.Message },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreditCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount)
        {
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);
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
                    { "error_message", ex.Message },
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

        private void LogExceptionError(string Method, Exception ex)
        {
            LogContext.PushProperty("Exception.HttpMethod", Request.Method);
            LogContext.PushProperty("Exception.Method", Method);
            LogContext.PushProperty("Exception.Url", Request.GetDisplayUrl());
            LogContext.PushProperty("Exception.Message", ex.Message);
            logger.LogError(ex.StackTrace);
        }
    }
}
