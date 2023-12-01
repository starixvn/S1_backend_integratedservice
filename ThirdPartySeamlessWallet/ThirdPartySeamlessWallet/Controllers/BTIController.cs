using System.Text;

using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

using Serilog.Context;

using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Model;

using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BTIController : ControllerBase
    {
        private readonly ILogger<BTIController> logger;
        private readonly IHostEnvironment _environment;
        private static BTIBLL _bTIBLL { get; set; }

        public BTIController(IServiceProvider provider, ILogger<BTIController> logger)
        {
            this.logger = logger;
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _bTIBLL = new BTIBLL(provider);
        }

        [EnableCors("BTIPolicy")]
        [HttpGet]
        public async Task<BTIStatusObj> Status([FromQuery] string token)
        {
            return _bTIBLL.Staus(token);
        }

        [EnableCors("BTIPolicy")]
        [HttpGet]
        public async Task<BTIRefreshObj> Refresh([FromQuery] string token)
        {
            return _bTIBLL.Refresh(token);
        }

        [HttpGet]
        public async Task<IActionResult> ValidateToken([FromQuery] string auth_token)
        {
            try
            {
                if (string.IsNullOrEmpty(auth_token))
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-3" },
                        { "error_message", $"Fail:Query auth_token is null." },
                    });
                }

                return Content(_bTIBLL.ValidateToken(auth_token));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"Fail:{ex.Message}" },
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Reserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount)
        {
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id) || amount < 0)
                {
                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", "-10" },
                        { "error_message", $"Fail:Wrong Request." },
                        { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                        { "balance", "0" }
                    });
                }

                return Content(await _bTIBLL.ReserveAsync(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    amount = amount,
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);
                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", "-1" },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] decimal amount, [FromQuery] string req_id, [FromQuery] string purchase_id)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);

                return Content(await _bTIBLL.DebitReserveAsync(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    amount = amount,
                    req_id = req_id,
                    purchase_id = purchase_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CancelReserve([FromQuery] string cust_id, [FromQuery] string reserve_id)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id))
                {
                    if (!_environment.IsDevelopment()) //PRD
                        errorCode = "-8";

                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", errorCode },
                        { "error_message", $"Fail:Wrong Request." },
                        { "balance", "0" }
                    });
                }

                return Content(await _bTIBLL.CancelReserveAsync(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "balance", "0" }
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> CommitReserve([FromQuery] string cust_id, [FromQuery] string reserve_id, [FromQuery] string purchase_id)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                if (string.IsNullOrEmpty(cust_id) || string.IsNullOrEmpty(reserve_id))
                {
                    if (!_environment.IsDevelopment()) //PRD
                        errorCode = "-8";

                    return ReturnBTI(new Dictionary<string, string>
                    {
                        { "error_code", errorCode },
                        { "error_message", $"Fail:Wrong Request." },
                        { "balance", "0" }
                    });
                }

                return Content(await _bTIBLL.CommitReserve(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    purchase_id = purchase_id,
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "balance", "0" }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DebitCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount, [FromQuery] string purchase_id)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);

                return Content(await _bTIBLL.Credit(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    amount = amount * -1,
                    req_id = req_id,
                    purchase_id = purchase_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreditCustomer([FromQuery] string cust_id, [FromQuery] string req_id, [FromQuery] decimal amount, [FromQuery] string purchase_id)
        {
            var errorCode = "0"; //DEV MUST ALWAYS SET error_code=0 For BTI Test Case
            try
            {
                using var requestBody = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true);

                return Content(await _bTIBLL.Credit(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    amount = amount,
                    req_id = req_id,
                    purchase_id = purchase_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
            }
            catch (Exception ex)
            {
                LogExceptionError(GetCurrentMethod(), ex);

                if (!_environment.IsDevelopment()) //PRD
                    errorCode = "-9";

                return ReturnBTI(new Dictionary<string, string>
                {
                    { "error_code", errorCode },
                    { "error_message", $"Fail:{ex.Message}" },
                    { "trx_id", $"{DateTime.UtcNow.ToFileTimeUtc()}"},
                    { "balance", "0" }
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

        private void LogExceptionError(string Method, Exception ex)
        {
            using (LogContext.PushProperty("Exception.HttpMethod", Request.Method))
            using (LogContext.PushProperty("Exception.Method", Method))
            using (LogContext.PushProperty("Exception.Url", Request.GetDisplayUrl()))
            using (LogContext.PushProperty("Exception.Message", ex.Message))
            {
                logger.LogError(ex, ex.Message);
            }
        }
    }
}
