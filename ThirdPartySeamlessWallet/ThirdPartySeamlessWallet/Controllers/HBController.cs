using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/api")]
    public class HBController : Controller
    {
        private readonly IHBBLL _hbbll;

        public HBController(IHBBLL hbbll)
        {
            _hbbll = hbbll;
        }
        [HttpGet]
        [Route("Test")]
        public string Ping() => "ok";
        
        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("auth")]
        public async Task<HBPlayerDetailResponse> GetBalance()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<HBPlayerDetailRequest>(content);
            return _hbbll.Auth(request);
        }
    }
}