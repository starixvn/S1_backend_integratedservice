using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NextspinController : ControllerBase
    {
        private readonly INextspinHandler _nextspinHandler;
        public NextspinController(INextspinHandler nextspinHandler)
        {
            _nextspinHandler = nextspinHandler;
        }


        [HttpPost]
        public async Task<NextspinCommonResponse> Action()
        {
            Request.Headers.TryGetValue("API", out StringValues apiName);
            LogContext.PushProperty("Method", apiName);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest>(body);
            return _nextspinHandler.Handle(request);
        }
    }
}