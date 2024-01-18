using Microsoft.AspNetCore.Mvc;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model;
using Org.BouncyCastle.Crypto.Agreement.JPake;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class GAController : ControllerBase
    {
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly Tp _thisTp;
        private readonly ILogger<TFGController> _logger;
        public GAController(IServiceProvider provider)
        {
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _logger = provider.GetRequiredService<ILogger<TFGController>>();
            _thisTp = Tp.GA;
        }
        public class GetBalanceReq
        {
            public string member_ref_id { get; set; }
        }
        public class GetBalanceResp
        {
            public decimal balance { get; set; }
        }
        [HttpGet]
        [Route("[controller]/balance")]
        public IActionResult GetBalance(GetBalanceReq req)
        {
            try
            {
                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)_thisTp, req.member_ref_id, (int)GameType.GA_CockFighting);
                GetBalanceResp result = new GetBalanceResp() { balance = tpCustBalance.Balance };
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GetBalance\r\n{ex.ToString()}");
                return BadRequest();
            }
        }
        public class TransferReq
        {
            public string amount { get; set; }
            public string id { get; set; }
            public string member_ref_id { get; set; }
            public ticket ticket { get; set; }
            public string type { get; set; }
        }
        public class ticket
        {
            public string account_id { get; set; }
            public string bet_amount { get; set; }
            public string arena_code { get; set; }
            public string ticket_amount { get; set; }
            public string ante_amount { get; set; }
            public DateTime created_date_time { get; set; }
            public string currency_code { get; set; }
            public string fight_number { get; set; }
            public string match_day_code { get; set; }
            public string member_ref_id { get; set; }
            public DateTime modified_date_time { get; set; }
            public string odds { get; set; }
            public string result { get; set; }
            public string selection { get; set; }
            public DateTime? settled_date_time { get; set; }
            public string sid { get; set; }
            public int status { get; set; }
            public string win_loss_amount { get; set; }
        }
        [HttpPost]
        [Route("[controller]/transfer")]
        public IActionResult Transfer(TransferReq req)
        {

        }
    }
}
