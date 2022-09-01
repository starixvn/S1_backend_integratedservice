using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using System.Web;
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
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        //private readonly BettingBLL _bettingBLL;
        //private readonly SettledBLL _settledBLL;
        //private readonly Log2File _log2File;
        private readonly IKiss918BLL _kiss918BLL;

        #endregion

        #region Constructors

        public Kiss918Controller(IServiceProvider provider)
        {
            thisTp = Tp.Kiss918;

            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            //_bettingBLL = provider.GetService<BettingBLL>();
            //_settledBLL = provider.GetService<SettledBLL>();
            //_log2File = provider.GetService<Log2File>();

            _kiss918BLL = provider.GetService<IKiss918BLL>();
        }

        #endregion

        #region Request

        public class BalanceReq
        {
            public string username { get; set; }
            public string agent { get; set; }

        }

        #endregion

        #region Response

        public class BalanceResp
        {
            public BalanceResp()
            {
                status = new Status();
                data = new BalanceData();
            }

            public Status status { get; set; }
            public BalanceData data { get; set; }
        }

        public class Status
        {
            public int code { get; set; }
            public string message { get; set; }

        }

        public class BalanceData
        {
            public decimal balance { get; set; }
        }

        public class BetResp
        {
            public BetResp()
            {
                status = new Status();
                data = new BetData();
            }

            public Status status { get; set; }
            public BetData data { get; set; }

        }

        public class BetData
        {
            public BetData()
            {
                wallet = new Wallet();
                balance = new Balance();
            }
            public string username { get; set; }
            public Wallet wallet { get; set; }
            public Balance balance { get; set; }
            public string refId { get; set; }

        }

        public class Wallet
        {
            public decimal balance { get; set; }
            public string lastUpdate { get; set; }
        }

        public class Balance
        {
            public decimal before { get; set; }
            public decimal after { get; set; }

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

        ///// <summary>
        ///// FundRequest 余额要求
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/api/fundrequest")]
        //public async Task<OperationResponse_Kiss918> FundRequestAsync()
        //{
        //    if (!CheckToken(out string message))
        //        return new OperationResponse_Kiss918() { error = "100", description = message };

        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<FundRequestRequest_Kiss918>(content);
        //    return await _kiss918BLL.FundRequest(request);
        //}

        ///// <summary>
        ///// FundReturn 余额退还
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/api/fundreturn")]
        //public async Task<OperationResponse_Kiss918> FundReturnAsync()
        //{
        //    if (!CheckToken(out string message))
        //        return new OperationResponse_Kiss918() { error = "100", description = message };

        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<FundReturnRequest_Kiss918>(content);
        //    return await _kiss918BLL.FundReturn(request);
        //}

        ///// <summary>
        ///// FundBetResult
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/api/fundbetresult")]
        //public async Task<FundBetResultResponse_Kiss918> FundBetResultAsync()
        //{
        //    if (!CheckToken(out string message))
        //        return new FundBetResultResponse_Kiss918() { error = "100", description = message };

        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<FundBetResultRequest_Kiss918>(content);
        //    return await _kiss918BLL.FundBetResult(request);
        //}

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


        //private bool CheckPlayerByUserName(out int playerId, string userName = null)
        //{
        //    playerId = 0;
        //    if (string.IsNullOrWhiteSpace(userName))
        //        return false;

        //    var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, userName);
        //    if (string.IsNullOrWhiteSpace(tpCustInfo.TpCustId))
        //        return false;

        //    if (!int.TryParse(tpCustInfo.TpCustId, out int number))
        //        return false;

        //    playerId = number;
        //    return true;
        //}

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

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }

        #endregion
    }
}
