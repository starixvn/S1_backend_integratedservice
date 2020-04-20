using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class MailController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly SendXBLL sendXBLL;

        public MailController(CustInfoBLL custInfoBLL, SendXBLL sendXBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.sendXBLL = sendXBLL;
        }
        public class Req
        {
            public int custId { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult> SendWelcomeMailAsync(Req req)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null)
            {
                var resultResp = await sendXBLL.IdentifyAsync(custInfo);

                apiResult.ErrorCode = resultResp.ErrorCode;
                apiResult.Message = resultResp.Message;
                apiResult.RawMessage = resultResp.RawMessage;

                //if (apiResult.ErrorCode == 0)
                //{
                //    resultResp = await sendXBLL.WelcomeMailAsync(custInfo);
                //    apiResult.ErrorCode = resultResp.ErrorCode;
                //    apiResult.Message = resultResp.Message;
                //    apiResult.RawMessage = resultResp.RawMessage;
                //}
            }

            return apiResult;
        }
    }
}