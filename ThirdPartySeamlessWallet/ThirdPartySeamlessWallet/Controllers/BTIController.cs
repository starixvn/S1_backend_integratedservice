using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.ThirdParty;
using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class BTIController : ControllerBase
    {
        private readonly ILogger<BTIController> logger;
        private static BTIService btiService { get; set; }

        public BTIController(IServiceProvider provider, ILogger<BTIController> logger)
        {
            this.logger = logger; 
            btiService = new BTIService(provider);
            LogContext.PushProperty("TP", btiService.ThirdParty);
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
                        { "error_message", $"Query auth_token is null." },
                    });
                }

                return Content(btiService.ValidateToken(auth_token));
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

                return Content(await btiService.ReserveAsync(new BTIRequestObj()
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

                return Content(await btiService.DebitReserveAsync(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                    amount = amount,
                    req_id = req_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
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
                
                return Content(await btiService.CancelReserveAsync(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                }));
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

                return Content(await btiService.CommitReserve(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    reserve_id = reserve_id,
                }));
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

                return Content(await btiService.Credit(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    amount = amount * -1,
                    req_id = req_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
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
                
                return Content(await btiService.Credit(new BTIRequestObj()
                {
                    cust_id = cust_id,
                    amount = amount,
                    req_id = req_id,
                    requestBody = await requestBody.ReadToEndAsync()
                }));
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

        private void LogExceptionError(string Method, Exception ex)
        {
            using (LogContext.PushProperty("Exception.HttpMethod", Request.Method))
            using (LogContext.PushProperty("Exception.Method", Method))
            using (LogContext.PushProperty("Exception.Url", Request.GetDisplayUrl()))
            using (LogContext.PushProperty("Exception.Message", ex.Message))
            {
                logger.LogError(ex.StackTrace);
            }
        }
    }
}
