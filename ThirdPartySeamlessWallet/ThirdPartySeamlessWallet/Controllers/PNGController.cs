using Microsoft.AspNetCore.Mvc;
using System.Text;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class PNGController : ControllerBase
    {
        private readonly Tools _tools;
        private readonly ILogger<NextspinController> _logger;

        public PNGController(Tools tools, ILogger<NextspinController> logger)
        {
            _tools = tools;
            _logger = logger;
        }

        [Route("[controller]/Auth")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> Action()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var authenticateRequest = _tools.XMLDeserialize<PNG_AuthenticateRequest>(body);


            await Task.Delay(1);
            return "";
        }
    }
}
