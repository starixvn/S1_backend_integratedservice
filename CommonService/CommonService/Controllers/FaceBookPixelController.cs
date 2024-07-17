using CommonService.Service.BLL;
using CommonService.Service.Model;
using Microsoft.AspNetCore.Mvc;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class FaceBookPixelController : ControllerBase
    {
        private readonly FaceBookPixelBLL _faceBookPixelBll;

        public FaceBookPixelController(IServiceProvider provider)
        {
            _faceBookPixelBll = provider.GetRequiredService<FaceBookPixelBLL>();
        }
        
        public async Task<ApiResult> SendAsync(PixelRequest request) => await _faceBookPixelBll.SendAsync(request);
    }
}