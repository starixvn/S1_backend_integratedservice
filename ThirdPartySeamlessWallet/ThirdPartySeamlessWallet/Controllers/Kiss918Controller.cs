using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;
using ThirdPartySeamlessWallet.Service.Model.Exceptions;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class Kiss918Controller : ControllerBase
    {
        #region Fields

        private readonly Tp thisTp;

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly IKiss918BLL _kiss918BLL;

        #endregion

        #region Constructors

        public Kiss918Controller(IServiceProvider provider)
        {
            thisTp = Tp.Kiss918;

            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _kiss918BLL = provider.GetService<IKiss918BLL>();
        }

        #endregion

        /// <summary>
        /// 測試API有通
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/ping")]
        public string PingTest() => "Ping Ok.";

        #region Actions

        /// <summary>
        /// Authenticate 玩家身分驗證
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/authenticate")]
        public async Task<AuthenticateResponse_Kiss918> AuthenticateAsync()
        {
            if (!CheckToken(out string message))
                return new AuthenticateResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AuthenticateRequest_Kiss918>(content);
            return await _kiss918BLL.Authenticate(request);
        }

        /// <summary>
        /// GetBalance 獲取餘額信息
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("[controller]/api/getbalance")]
        public async Task<GetBalanceResponse_Kiss918> GetBalanceAsync(int playerID)
        {
            if (!CheckToken(out string message))
                return new GetBalanceResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            return await _kiss918BLL.GetBalance(playerID);
        }

        /// <summary>
        /// Bet (BetRequest of Wallet API) 押注
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/bet")]
        public async Task<OperationResponse_Kiss918> BetAsync()
        {
            if (!CheckToken(out string message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<BetRequest_Kiss918>(content);
            return await _kiss918BLL.Bet(request);
        }

        /// <summary>
        /// BetResult 押注结果
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/betresult")]
        public async Task<OperationResponse_Kiss918> BetResultAsync()
        {
            if (!CheckToken(out string message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<BetResultRequest_Kiss918>(content);
            return await _kiss918BLL.BetResult(request);
        }

        /// <summary>
        /// Refund 退款
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/refund")]
        public async Task<OperationResponse_Kiss918> RefundAsync()
        {
            if (!CheckToken(out string message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<RefundRequest_Kiss918>(content);
            return await _kiss918BLL.Refund(request);
        }

        /// <summary>
        /// JackpotWin
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/jackpotwin")]
        public async Task<OperationResponse_Kiss918> JackpotWinAsync()
        {
            if (!CheckToken(out string message))
                return new OperationResponse_Kiss918(Kiss918ErrorCode.InvaildPlayerAuth, message);

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<JackpotWinRequest_Kiss918>(content);
            return await _kiss918BLL.JackpotWin(request);
        }

        #endregion

        #region Private Methods

        private bool CheckToken(out string message)
        {
            message = string.Empty;

            Request.Headers.TryGetValue("Token", out var sv);
            string token = sv.ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                message = "Token is missing.";
                return false;
            }

            var setting = JsonConvert.DeserializeObject<Dictionary<string, string>>(_thirdPartyBLL.GetThirdPartySetting((int)thisTp).Setting);
            string apiToken = setting.ContainsKey("ApiToken") && !string.IsNullOrEmpty(setting["ApiToken"])
                ? setting["ApiToken"]
                : throw new Kiss918Exception(Kiss918ErrorCode.ServiceNotAvailable, "ApiToken is Invaild.");
            if (apiToken != token)
            {
                message = "Token invaild.";
                return false;
            }
            return true;
        }

        #endregion
    }
}
