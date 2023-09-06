using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class KMHeaderFilter: ActionFilterAttribute
    {
        private readonly IKMService _kmService;
        public KMHeaderFilter(IKMService kmService)
        {
            _kmService = kmService;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var req = context.HttpContext.Request;
            var clientId = req.Headers["X-QM-ClientId"];
            var clientSecret = req.Headers["X-QM-ClientSecret"];
            if (!_kmService.CheckCredentials(clientId, clientSecret))
            {
                context.Result = new JsonResult(new
                {
                    err = 30,
                    errdesc = "Invalid Credentials"
                });
            }
            base.OnActionExecuting(context);
        }
    }
}