using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class GAController : ControllerBase
    {
        private readonly GaBLL _gaBll;

        public GAController(IServiceProvider provider)
        {
            _gaBll = provider.GetRequiredService<GaBLL>();
        }
        
        public async Task<ApiResult> SendGaPurchaseAsync(PurchaseRequest request) => await _gaBll.GaEventSendAsync(request);
    }
}
