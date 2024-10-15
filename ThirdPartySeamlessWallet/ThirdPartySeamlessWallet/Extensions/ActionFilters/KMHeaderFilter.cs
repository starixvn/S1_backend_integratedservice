using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class KMHeaderFilter : ActionFilterAttribute
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
            if (context.RouteData.Values.TryGetValue("sitename", out var sitename))
            {
                if (sitename == null || sitename.ToString() == "invalid" || 
                    !_kmService.CheckCredentialsBySiteName(clientId, clientSecret, sitename.ToString()))
                {
                    context.Result = new JsonResult(new
                    {
                        err = 30,
                        errdesc = "Invalid Credentials"
                    });
                }
            }
            else
            {
                if (!_kmService.CheckCredentials(clientId, clientSecret))
                {
                    context.Result = new JsonResult(new
                    {
                        err = 30,
                        errdesc = "Invalid Credentials"
                    });
                }
            }

            base.OnActionExecuting(context);
        }
    }
}