using CommonService.Service.BLL;
using CommonService.Service.Model;

using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class IovationController : ControllerBase
    {
        private readonly IovationBLL _iovationBLL;

        public IovationController(IServiceProvider provider)
        {
            _iovationBLL = provider.GetRequiredService<IovationBLL>();
        }

        [HttpPost]
        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            return await _iovationBLL.IovationChecksAsync(iovationChecksRequest);
        }
    }
}
