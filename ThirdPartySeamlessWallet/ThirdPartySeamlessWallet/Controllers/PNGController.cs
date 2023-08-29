using Microsoft.AspNetCore.Mvc;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class PNGController : ControllerBase
    {
        private readonly Tools _tools;
        private readonly ILogger<PNGController> _logger;
        private readonly IPNGBLL _pngBll;

        public PNGController(Tools tools, ILogger<PNGController> logger, IPNGBLL pngBll)
        {
            _tools = tools;
            _logger = logger;
            _pngBll = pngBll;
        }

        [Route("[controller]/Auth")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> Action()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var authenticateRequest = _tools.XMLDeserialize<PNG_AuthenticateRequest>(body);
            var authenticateResponse = _pngBll.Authenticate(authenticateRequest);

            return authenticateResponse;
        }
    }
}
