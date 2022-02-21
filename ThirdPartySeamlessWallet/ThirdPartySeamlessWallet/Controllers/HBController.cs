using System.Dynamic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/api")]
    public class HBController : Controller
    {
        private readonly IHBBLL _hbbll;

        public HBController(IHBBLL hbbll)
        {
            _hbbll = hbbll;
        }
        [HttpGet]
        [Route("Test")]
        public string Ping() => "ok";
        
        /// <summary>
        /// 驗證授權
        /// </summary>
        [HttpPost]
        [Route("auth")]
        public async Task<HBPlayerDetailResponse> Auth(HBPlayerDetailRequest request)
        {
            return await _hbbll.Auth(request);
        }

        /// <summary>
        /// 交易
        /// </summary>
        [HttpPost]
        [Route("tx")]
        public async Task<HBFundTransferResponse> FundTransfer()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<dynamic>(content);
            var token = (string) request.fundtransferrequest.token;
            var response = await _hbbll.Auth(token);
            if (response.Status.success == false)
            {
                return new HBFundTransferResponse()
                {
                    fundtransferresponse = new
                    {
                        status = new
                        {
                            success = false,
                            autherror = true
                        }
                    }
                };
            }

            return new HBFundTransferResponse()
            {
                fundtransferresponse = await _hbbll.FundTransfer(request.fundtransferrequest)
            };
        }
    }
}