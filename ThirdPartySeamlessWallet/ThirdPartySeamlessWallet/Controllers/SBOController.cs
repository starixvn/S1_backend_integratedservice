using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace ThirdPartySeamlessWallet.Controllers
{
    public class SBOBalanceResponse
    {
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public int ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
    }

    [ApiController]
    [Route("[controller]/api")]
    public class SBOController : ControllerBase
    {
        /// <summary>
        /// 取得餘額
        /// </summary>
        [HttpPost]
        [Route("GetBalance")]
        public SBOBalanceResponse GetBalance()
        {
            return new SBOBalanceResponse()
            {
                AccountName = "DNova88_250032",
                Balance = 500.0000m,
                ErrorCode = 0,
                ErrorMessage = "No Error"
            };
        }
    }
}