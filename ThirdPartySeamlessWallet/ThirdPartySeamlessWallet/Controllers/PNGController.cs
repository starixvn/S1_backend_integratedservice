using Microsoft.AspNetCore.Mvc;
using System.Text;
using ThirdPartySeamlessWallet.Extensions;
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

        /// <summary>
        /// 驗證
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/authenticate")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> Authenticate()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var authenticateRequest = _tools.XMLDeserialize<PNG_AuthenticateRequest>(body);
            var authenticateResponse = _pngBll.Authenticate(authenticateRequest);
            var xmlString = XmlHelper.SerializeToXmlString(authenticateResponse);

            return Content(xmlString, "application/xml");
        }


        /// <summary>
        /// 餘額
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/balance")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> GetBalance()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var balanceRequest = _tools.XMLDeserialize<PNG_BalanceRequest>(body);
            var balanceResponse = _pngBll.Balance(balanceRequest);
            var xmlString = XmlHelper.SerializeToXmlString(balanceResponse);

            return Content(xmlString, "application/xml");
        }
    }
}
