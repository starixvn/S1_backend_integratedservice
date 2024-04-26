using System.Text;
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
        public HBPlayerDetailResponse Auth(HBPlayerDetailRequest request)
        {
            return new HBPlayerDetailResponse()
            {
                playerdetailresponse = _hbbll.PlayerDetail(request.playerdetailrequest.token)
            };
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
            if ((bool)request.fundtransferrequest.isretry == false)
            {
                var token = (string)request.fundtransferrequest.token;
                if (_hbbll.Auth(token) == false)
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
            }

            return new HBFundTransferResponse()
            {
                fundtransferresponse = await _hbbll.FundTransfer(request.fundtransferrequest)
            };
        }

        /// <summary>
        /// 交易
        /// </summary>
        [HttpPost]
        [Route("query")]
        public async Task<HBFundTransferResponse> QueryRequest()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<dynamic>(content);

            return new HBFundTransferResponse()
            {
                fundtransferresponse = new
                {
                    status = new
                    {
                        success = _hbbll.QueryRequest(request.queryrequest)
                    }
                }
            };
        }
    }
}