using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class Kiss918Controller : ControllerBase
    {
        #region Fields

        private readonly Tp thisTp;

        //private readonly ThirdPartyBLL _thirdPartyBLL;
        //private readonly TpCustInfoBLL _tpCustInfoBLL;
        //private readonly BettingBLL _bettingBLL;
        //private readonly SettledBLL _settledBLL;
        //private readonly Log2File _log2File;
        private readonly IKiss918BLL _kiss918BLL;

        #endregion

        #region Constructors

        public Kiss918Controller(IServiceProvider provider)
        {
            thisTp = Tp.Kiss918;

            //_thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            //_tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
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
        public async Task<GetBalanceResponse_Kiss918> GetBalanceAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// Bet (BetRequest of Wallet API) 押注
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/bet")]
        public async Task<GetBalanceResponse_Kiss918> BetAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// BetResult 押注结果
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/betresult")]
        public async Task<GetBalanceResponse_Kiss918> BetResultAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// Refund 退款
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/refund")]
        public async Task<GetBalanceResponse_Kiss918> RefundAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// FundRequest 余额要求
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/fundrequest")]
        public async Task<GetBalanceResponse_Kiss918> FundRequestAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// FundReturn 余额退还
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/fundreturn")]
        public async Task<GetBalanceResponse_Kiss918> FundReturnAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// FundBetResult
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/fundbetresult")]
        public async Task<GetBalanceResponse_Kiss918> FundBetResultAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        /// <summary>
        /// JackpotWin
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/api/jackpotwin")]
        public async Task<GetBalanceResponse_Kiss918> JackpotWinAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        #endregion

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }
    }
}
