using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/wallet")]
    public class KMController : ControllerBase
    {
        private readonly ILogger<KMController> _logger;
        private readonly IKMService _kmService;

        public KMController(IServiceProvider provider, ILogger<KMController> logger)
        {
            _logger = logger;
            _kmService = provider.GetService<IKMService>();
        }
        
        [HttpGet]
        [Route("Ping")]
        public string Ping() => "Pong";

        /// <summary>
        /// 扣錢
        /// </summary>
        [HttpPost]
        [Route("debit")]
        public async Task<BaseResult> Debit(KMDebitRequest request)
        {
            try
            {
                return await _kmService.Debit(request);
            }
            catch (Exception ex)
            {
                return new ErrorResult()
                {
                    err = 900,
                    errdesc = "system error"
                };
            }
        }
        
        /// <summary>
        /// 加錢
        /// </summary>
        [HttpPost]
        [Route("credit")]
        public async Task<KMCreditResponse> Credit(KMCreditRequest request)
        {
            return new KMCreditResponse();
        }

        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("balance")]
        public async Task<BaseResult> Balance(KMBalanceRequest request)
        {
            try
            {
                return _kmService.GetBalance(request);
            }
            catch (KmException ex)
            {
                return new ErrorResult()
                {
                    err = (int)ex.KMErrorCode,
                    errdesc = ex.ErrorMessage
                };
            }
            catch (Exception e)
            {
                return new ErrorResult()
                {
                    err = 900,
                    errdesc = "system error"
                };
            }
        }
    }
}