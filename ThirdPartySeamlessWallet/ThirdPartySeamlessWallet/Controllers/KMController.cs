using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Extensions.ActionFilters;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [ServiceFilter(typeof(KMHeaderFilter))]
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
        [Route("[controller]/{sitename}/wallet/Ping")]
        [Route("[controller]/wallet/Ping")]
        public string Ping([FromRoute] string sitename = null) => "Pong";

        /// <summary>
        /// 扣錢
        /// </summary>
        [HttpPost]
        [Route("[controller]/{sitename}/wallet/debit")]
        [Route("[controller]/wallet/debit")]
        public async Task<BaseResult> Debit([FromRoute] string sitename = null)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<KMDebitRequest>(content);
                return await _kmService.Debit(request);
            }
            catch (KmException ke)
            {
                _logger.LogError(ke.ErrorMessage);
                return new ErrorResult()
                {
                    err = (int)ke.KMErrorCode,
                    errdesc = ke.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "km debit failed");
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
        [Route("[controller]/{sitename}/wallet/credit")]
        [Route("[controller]/wallet/credit")]
        public async Task<BaseResult> Credit([FromRoute] string sitename = null)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<KMCreditRequest>(content);

                return await _kmService.Credit(request);
            }
            catch (KmException ke)
            {
                _logger.LogError(ke.ErrorMessage);
                return new ErrorResult()
                {
                    err = (int)ke.KMErrorCode,
                    errdesc = ke.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "km credit failed");
                return new ErrorResult()
                {
                    err = 900,
                    errdesc = "system error"
                };
            }
        }

        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("[controller]/{sitename}/wallet/balance")]
        [Route("[controller]/wallet/balance")]
        public async Task<BaseResult> Balance([FromBody] KMBalanceRequest request, [FromRoute] string sitename = null)
        {
            try
            {
                return _kmService.GetBalance(request);
            }
            catch (KmException ke)
            {
                _logger.LogError(ke.ErrorMessage);
                return new ErrorResult()
                {
                    err = (int)ke.KMErrorCode,
                    errdesc = ke.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "km balance failed");
                return new ErrorResult()
                {
                    err = 900,
                    errdesc = "system error"
                };
            }
        }

        /// <summary>
        /// 奖励
        /// </summary>
        [HttpPost]
        [Route("[controller]/{sitename}/wallet/reward")]
        [Route("[controller]/wallet/reward")]
        public async Task<BaseResult> Reward([FromRoute] string sitename = null)
        {
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var content = await reader.ReadToEndAsync();
                var request = JsonConvert.DeserializeObject<KMRewardRequest>(content);

                return await _kmService.Reward(request);
            }
            catch (KmException ke)
            {
                _logger.LogError(ke.ErrorMessage);
                return new ErrorResult()
                {
                    err = (int)ke.KMErrorCode,
                    errdesc = ke.ErrorMessage
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "km reward failed");
                return new ErrorResult()
                {
                    err = 900,
                    errdesc = "system error"
                };
            }
        }
    }
}