using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog.Context;
using ThirdPartySeamlessWallet.Extensions;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NextspinController : ControllerBase
    {
        private readonly INextspinBLL _nextspinHandler;
        private readonly ILogger<NextspinController> _logger;
        public NextspinController(INextspinBLL nextspinHandler, ILogger<NextspinController> logger)
        {
            _nextspinHandler = nextspinHandler;
            _logger = logger;
        }


        [HttpPost]
        public async Task<NextspinCommonResponse> Action([FromHeader] NextspinHeader header)
        {
            LogContext.PushProperty("NextspinApi", header.API);
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();
                switch (header.API)
                {
                    case "authorize":
                        var authorize = JsonConvert.DeserializeObject<AuthorizeRequest>(body);
                        return _nextspinHandler.Handle(authorize);
                    case "getBalance":
                        var getBalance = JsonConvert.DeserializeObject<GetBalanceRequest>(body);
                        return _nextspinHandler.Handle(getBalance);
                    case "transfer":
                        var transfer = JsonConvert.DeserializeObject<TransferRequest>(body);
                        return await _nextspinHandler.Handle(transfer);
                    default:
                        var nextspin = JsonConvert.DeserializeObject<NextspinCommonRequest>(body);
                        throw new NextspinException(NextspinErrorCode.InvalidRequest);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "system error");
                return new NextspinCommonResponse() { Code = (int)NextspinErrorCode.SystemError, Msg = NextspinErrorCode.SystemError.GetEnumDescription(), SerialNo = NextspinBLL.GenerateSerialNo() };
            }
        }
    }
}