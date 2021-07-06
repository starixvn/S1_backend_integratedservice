using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class FireBaseController
    {
        private readonly FireBaseBLL fireBaseBLL;

        public FireBaseController(FireBaseBLL fireBaseBLL)
        {
            this.fireBaseBLL = fireBaseBLL;
        }

        public class FireBaseNotificationReq
        {
            public IEnumerable<FireBaseMessage> FireBaseMessages { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult<IEnumerable<FireBaseReturn>>> SendFireBaseNotificationAsync(FireBaseNotificationReq fireBaseNotificationReq)
        {
            var apiResult = new ApiResult<IEnumerable<FireBaseReturn>>();
            var fireBaseReturn = new List<FireBaseReturn>();
            if (fireBaseNotificationReq.FireBaseMessages.Any())
            {
                foreach (var fireBaseMessageItem in fireBaseNotificationReq.FireBaseMessages)
                {
                    var response = await fireBaseBLL.SendMessageAsync(fireBaseMessageItem);
                    var item = fireBaseReturn.Find(x => x.FireBaseId == fireBaseMessageItem.FireBaseId);
                    if (item == null)
                        fireBaseReturn.Add(new FireBaseReturn { FireBaseId = fireBaseMessageItem.FireBaseId, Status = (response.ErrorCode == ErrorCode.Success?1:2), Success = (response.ErrorCode == ErrorCode.Success ?1:0), Fail = (response.ErrorCode == ErrorCode.Success ? 0 : 1) });
                    else
                    {
                        if (response.ErrorCode == ErrorCode.Success)
                        {
                            item.Status = 1;
                            item.Success++;
                        }
                        else
                        {
                            item.Fail++;

                        }

                    }

                    apiResult.ErrorCode = 0;
                    apiResult.Data = fireBaseReturn;
                }
            }
            return apiResult;
        }
        public class FireBaseNotificationOneReq
        {
            public int CustId { get; set; }
            public string SiteName { get; set; }
            public string Token { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
            public int Status { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult> SendFireBaseNotificationOneAsync(FireBaseNotificationOneReq fireBaseNotificationOneReq)
        {
            var apiResult = new ApiResult();
            var item = new FireBaseMessage ();
            item.CustId = fireBaseNotificationOneReq.CustId;
            item.SiteName = fireBaseNotificationOneReq.SiteName;
            item.Token = fireBaseNotificationOneReq.Token;
            item.Title = fireBaseNotificationOneReq.Title;
            item.Body = fireBaseNotificationOneReq.Body;
            item.Status = fireBaseNotificationOneReq.Status;


            //if (fireBaseBLL.SendMessage(item, out returnString))
            apiResult = fireBaseBLL.SendMessage(item);
           // apiResult = await fireBaseBLL.SendMessageAsync(item);

            return apiResult;
        }


    }
}
