using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Org.BouncyCastle.Cms;
using System.Text;
using System.Web;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class TFGController : ControllerBase
    {
        #region Fields

        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Tp thisTp;
        //private readonly int gameType = 0;
        private readonly Tools tools;
        private readonly ILogger<TFGController> logger;
        private readonly IHostEnvironment environment;
        #endregion

        #region Constructors
        public TFGController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            tools = provider.GetRequiredService<Tools>();
            logger = provider.GetRequiredService<ILogger<TFGController>>();
            thisTp = Tp.TFG;
            environment = provider.GetRequiredService<IHostEnvironment>();
        }
        #endregion

        #region model
        public class ValidateReq
        {
            public string token { get; set; }
        }
        public class ValidateResp
        {
            public string loginName { get; set; }
        }
        #endregion

        #region
        [Route("[controller]/token/validate")]
        [HttpPost]
        public IActionResult Validate(ValidateReq req)
        {
            try
            {
                //logger.LogInformation($"token\r\n{req.token}");
                var sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(req.token)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0];
                string sessionToken = sessionKeyArr[1];
                string tpUserName = sessionKeyArr[2];
                //logger.LogInformation($"authToken\r\n{authToken}");
                //logger.LogInformation($"sessionToken\r\n{sessionToken}");
                //logger.LogInformation($"tpUserName\r\n{tpUserName}");
                if (!authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    return StatusCode(400);
                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, tpUserName);
                if (tpCheck == null || tpCheck.Token != sessionToken)
                    return StatusCode(400);
                ValidateResp result = new ValidateResp();
                result.loginName = tpUserName;
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError($"Validate\r\n{JsonConvert.SerializeObject(req)}\r\n{ex.ToString()}");
                return StatusCode(400);
            }
        }

        public class TFGWallet
        {
            public Decimal balance { get; set; }
        }
        [HttpGet("[controller]/wallet")]
        public IActionResult GetBalance([FromQuery] string loginName)
        {
            try
            {
                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, loginName, (int)GameType.TFG_Esports);
                TFGWallet result = new TFGWallet() { balance = tpCustBalance.Balance };
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError($"GetBalance\r\n{loginName}\r\n{ex.ToString()}");
                return StatusCode(400);
            }
        }

        public class TransferReq
        {
            public string loginName { get; set; }
            public decimal amount { get; set; }
            public bool placeBet { get; set; }
            public string ticketNum { get; set; }
            public string description { get; set; }
            public TicketDetail ticketDetail { get; set; }
        }
        public class TicketDetail
        {
            public string id { get; set; }
            public decimal odds { get; set; }
            public decimal amount { get; set; }
            public string result { get; set; }
            public string map_num { get; set; }
            public string currency { get; set; }
            public decimal earnings { get; set; }
            public int event_id { get; set; }
            public string handicap { get; set; }
            public bool is_combo { get; set; }
            public decimal euro_odds { get; set; }
            public int market_id { get; set; }
            public string event_name { get; set; }
            public decimal? malay_odds { get; set; }
            public string member_code { get; set; }
            public decimal member_odds { get; set; }
            public string ticket_type { get; set; }
            public DateTimeOffset date_created { get; set; }
            public int game_type_id { get; set; }
            public bool is_unsettled { get; set; }
            public string bet_selection { get; set; }
            public string bet_type_name { get; set; }
            public string market_option { get; set; }
            public string result_status { get; set; }
            public DateTimeOffset event_datetime { get; set; }
            public string game_type_name { get; set; }
            public string request_source { get; set; }
            public string competition_name { get; set; }
            public string game_market_name { get; set; }
            public string member_odds_style { get; set; }
            public DateTimeOffset modified_datetime { get; set; }
            public string settlement_status { get; set; }
            public DateTimeOffset settlement_datetime { get; set; }
            //public TicketDetail tickets { get; set; }
        }

        [Route("[controller]/transfer")]
        [HttpPost]
        public IActionResult Transfer(TransferReq req)
        {
            try
            {
                logger.LogInformation($"Transfer\r\n{req}");
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {
                    logger.LogInformation($"privateToken\r\n{privateToken}");
                    logger.LogInformation($"authorization\r\n{authorization}");
                }
                return StatusCode(400);
            }
            catch (Exception ex)
            {
                logger.LogError($"Transfer\r\n{ex.ToString()}");
                return StatusCode(400);
            }

            //return Ok();
        }

        [Route("[controller]/rollback")]
        [HttpPost]
        public IActionResult Rollback(TransferReq req)
        {
            try
            {
                logger.LogInformation($"Rollback\r\n{req}");
                Request.Headers.TryGetValue("Authorization", out var authorization);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, req.loginName);
                var privateToken = thirdPartySetting.TpSetting[$"PrivateToken_{tpCustInfo.TpCurrencyId}"];
                if (privateToken != authorization)
                {

                }
            }
            catch (Exception ex)
            {
                logger.LogError($"Rollback\r\n{ex.ToString()}");
                return StatusCode(400);
            }

            return Ok();
        }
        #endregion
    }
}
