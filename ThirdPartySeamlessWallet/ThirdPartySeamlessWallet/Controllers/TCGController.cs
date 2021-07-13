using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        public async Task<dynamic> SeamlessWallet([FromQuery] string merchant_code, [FromQuery] string @params, [FromQuery] string sign)
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

    }
}
