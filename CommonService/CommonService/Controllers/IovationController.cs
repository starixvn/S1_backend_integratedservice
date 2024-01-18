using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class IovationController : ControllerBase
    {
        private readonly IovationBLL _iovationBll;
        private readonly ILogger<IovationController> _logger;

        public IovationController(IServiceProvider provider)
        {
            _iovationBll = provider.GetRequiredService<IovationBLL>();
            _logger = provider.GetRequiredService<ILogger<IovationController>>();
        }

        [HttpPost]
        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            try
            {
                return await _iovationBll.IovationChecksAsync(iovationChecksRequest);
            }
            catch (Exception e)
            {
                _logger.LogInformation($"IovationChecks Exception :{e.Message}");
                throw;
            }
        }
    }
}
