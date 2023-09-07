using Microsoft.AspNetCore.Mvc;
using System.Text;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class PNGController : ControllerBase
    {
        private readonly IPNGBLL _pngBll;
        private readonly Tools _tools;
        private readonly ILogger<PNGController> _logger;

        public PNGController(IPNGBLL pngBll, Tools tools, ILogger<PNGController> logger)
        {
            _pngBll = pngBll;
            _tools = tools;
            _logger = logger;
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
        /// 取餘額
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

        /// <summary>
        /// 投注
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/reserve")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> Reserve()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var reserveRequest = _tools.XMLDeserialize<PNG_ReserveRequest>(body);
            var reserveResponse = await _pngBll.Reserve(reserveRequest);
            var xmlString = XmlHelper.SerializeToXmlString(reserveResponse);
            return Content(xmlString, "application/xml");

        }

        /// <summary>
        /// 取消投注
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/cancelReserve")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> CancelReserve()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var cancelReserveRequest = _tools.XMLDeserialize<PNG_CancelReserveRequest>(body);
            var cancelReserveResponse = await _pngBll.CancelReserve(cancelReserveRequest);
            var xmlString = XmlHelper.SerializeToXmlString(cancelReserveResponse);

            return Content(xmlString, "application/xml");
        }

        /// <summary>
        /// 派彩結果
        /// </summary>
        /// <returns></returns>
        [Route("[controller]/release")]
        [Produces("application/xml")]
        [HttpPost]
        public async Task<dynamic> Release()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();

            var releaseRequest = _tools.XMLDeserialize<PNG_ReleaseRequest>(body);
            var releaseResponse = await _pngBll.Release(releaseRequest);
            var xmlString = XmlHelper.SerializeToXmlString(releaseResponse);

            return Content(xmlString, "application/xml");
        }
    }
}
