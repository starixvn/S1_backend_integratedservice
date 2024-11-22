using Microsoft.AspNetCore.Mvc;

using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
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
        [Route("[controller]")]
        [Route("[controller]/{sitename}")]
        public async Task<dynamic> SeamlessWallet([FromForm] string merchant_code, [FromForm] string @params, [FromForm] string sign, [FromRoute] string sitename = "")
        {
            try
            {
                return await _tcgBLL.SeamlessWallet(merchant_code, @params, sign, sitename);
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

        [HttpPost]
        [Route("[controller]/[action]/{sitename}/{merchant_code}")]
        public async Task<dynamic> Internal([FromRoute] string merchant_code, [FromBody] TCGRequest request, [FromRoute] string sitename = "")
        {
            try
            {
                return await _tcgBLL.SeamlessWallet(merchant_code, request, sitename);
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
