using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Extensions.ActionFilters;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ServiceFilter(typeof(MGHeaderFilter))]
    [ServiceFilter(typeof(MGExceptionFilter))]
    public class MGController : ControllerBase
    {
        private readonly IMGBLL _mgbll;
        
        public MGController(IServiceProvider provider)
        {
            _mgbll = provider.GetService<IMGBLL>();
        }

        [HttpGet]
        [Route("ping")]
        public string Ping() => "pong";

        /// <summary>
        /// 登入
        /// </summary>
        [HttpPost]
        [Route("login")]
        public MGLoginResponse Login(MGLoginRequest request)
        {
            return _mgbll.Login(request);
        }
        
        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("getbalance")]
        public MGGetBalanceResponse GetBalance(MGGetBalanceRequest request)
        {
            return _mgbll.GetBalance(request);
        }
        
        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("updatebalance")]
        public async Task<MGUpdatebalanceResponse> UpdateBalance(MGUpdatebalanceRequest request)
        {
            return await _mgbll.UpdateBalance(request);
        }
        
        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("rollback")]
        public async Task<MGRollbackResponse> Rollback(MGRollbackeRequest request)
        {
            return await _mgbll.Rollback(request);
        } 
    }
}