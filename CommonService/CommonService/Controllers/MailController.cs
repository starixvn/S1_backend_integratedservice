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
        private readonly SendGridBLL sendGridBLL;

        public MailController(CustInfoBLL custInfoBLL, SendXBLL sendXBLL, SendGridBLL sendGridBLL)
        {
            this.custInfoBLL = custInfoBLL;
            this.sendXBLL = sendXBLL;
            this.sendGridBLL = sendGridBLL;
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

        public class Req1
        {
            public int custId { get; set; }
            public string subject { get; set; }
            public string content { get; set; }
        }

        [HttpPost]
        public async Task<ApiResult> SendMailAsync(Req1 req)
        {
            ApiResult apiResult = new ApiResult();
            var custInfo = custInfoBLL.GetCustInfoProfile(req.custId);

            if (custInfo != null)
            {
                var resultResp = await sendGridBLL.SendMailAsync(custInfo, req.subject, req.content);

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