using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class CRMController : ControllerBase
    {
        private readonly CRMBLL _cRMBll;

        public CRMController(IServiceProvider provider)
        {
            _cRMBll = provider.GetRequiredService<CRMBLL>();
        }
        public async Task<ApiResult> CRMEventSendAsync(CRMRequest request) => await _cRMBll.CRMEventSendAsync(request);
    }
}
