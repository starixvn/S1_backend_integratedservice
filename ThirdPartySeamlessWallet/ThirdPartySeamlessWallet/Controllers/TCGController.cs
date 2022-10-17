using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TCGController : ControllerBase
    {
        private readonly ILogger<TCGController> _logger;

        private static ITCGBLL _tcgBLL { get; set; }

        public TCGController(IServiceProvider provider, ILogger<TCGController> logger)
        {
            _logger = logger;
            _tcgBLL = new TCGBLL(provider);
        }

       
        [HttpPost]
        public async Task<dynamic> SeamlessWallet([FromForm] string merchant_code, [FromForm] string @params, [FromForm] string sign)
        {
            try
            {
                return await _tcgBLL.SeamlessWallet(merchant_code, @params, sign);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGReturnObj()
                {
                    status = TCGReturnCode.UnknownSystemError.ToString(),
                    error_desc= "Unknown system error",
                };         
            }
        }

        [HttpPost]
        [Route("{merchant_code}")]
        public async Task<dynamic> Internal([FromRoute] string merchant_code)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var bodyText = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<TCGRequest>(bodyText);
                return await _tcgBLL.SeamlessWallet(merchant_code, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new TCGReturnObj()
                {
                    status = TCGReturnCode.UnknownSystemError.ToString(),
                    error_desc = "Unknown system error",
                };
            }
        }

    }
}
