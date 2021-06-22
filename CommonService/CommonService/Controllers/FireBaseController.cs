using CommonService.Service.BLL;
using CommonService.Service.Model;
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
        public async Task<ApiResult<IEnumerable<FireBaseMessage>>> SendFireBaseNotificationAsync(FireBaseNotificationReq fireBaseNotificationReq)
        {
            var apiResult = new ApiResult<IEnumerable<FireBaseMessage>>();
            




            return apiResult;
        }
        public class FireBaseNotificationOneReq
        {
            public FireBaseMessage FireBaseMessage { get; set; }
        }
        [HttpPost]
        public async Task<ApiResult> SendFireBaseNotificationOneAsync(FireBaseNotificationOneReq fireBaseNotificationOneReq)
        {
            var apiResult = new ApiResult();
            String returnString;
            if (fireBaseBLL.SendMessage(fireBaseNotificationOneReq.FireBaseMessage ,out returnString))
            {
                apiResult.ErrorCode = 0;
            }

            apiResult.Message = returnString;

            return apiResult;
        }


    }
}
