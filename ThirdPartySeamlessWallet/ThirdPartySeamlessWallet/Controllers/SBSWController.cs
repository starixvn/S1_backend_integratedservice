using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

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
        //[GzipFilter]
        public async Task<dynamic> SeamlessWallet([FromRoute] string merchant, [FromRoute] string method)
        {
            try
            {
                if (!Enum.TryParse(method, ignoreCase: true, out SBSWTransType transType))
                {
                    return new ReturnObj(ReturnCode.InvalidParameters);
                }

                using var reader = new StreamReader(Request.Body);
                var bodyText = await reader.ReadToEndAsync();               
                return await _sbswBLL.SeamlessWallet(transType, merchant, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError);
            }
        }

    }
}
