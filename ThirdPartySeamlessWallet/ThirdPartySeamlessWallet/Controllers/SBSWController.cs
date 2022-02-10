using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Extensions.ActionFilters;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/{merchant}")]
    public class SBSWController : ControllerBase
    {
        private readonly ILogger<SBSWController> _logger;

        private static ISBSWBLL _sbswBLL { get; set; }

        public SBSWController(IServiceProvider provider, ILogger<SBSWController> logger)
        {
            _logger = logger;
            _sbswBLL = new SBSWBLL(provider);
        }

       
        [HttpPost]
        [Route("{method}")]
        [GzipFilter]
        public async Task<dynamic> SeamlessWallet([FromRoute] string merchant, [FromRoute] string method)
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var bodyText = await reader.ReadToEndAsync();
                return await _sbswBLL.SeamlessWallet(merchant, method, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new SBSWReturnObj(SBSWReturnCode.UnknownError);
            }
        }

    }
}
