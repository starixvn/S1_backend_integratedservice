using CommonService.Service.BLL;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CommonService.Controllers
{
    [Route("Common/[controller]/[action]")]
    [ApiController]
    public class IovationController : ControllerBase
    {
        private readonly CustInfoBLL custInfoBLL;
        private readonly IovationBLL iovationBLL;
        public IovationController(IovationBLL iovationBLL, CustInfoBLL custInfoBLL)
        {
            this.iovationBLL = iovationBLL;
            this.custInfoBLL = custInfoBLL;
        }
        [HttpPost]
        public async Task<ApiResult<IovationCheckResult>> IovationChecksAsync(IovationChecksRequest iovationChecksRequest)
        {
            return await iovationBLL.IovationChecksAsync(iovationChecksRequest);
        }
    }
}
