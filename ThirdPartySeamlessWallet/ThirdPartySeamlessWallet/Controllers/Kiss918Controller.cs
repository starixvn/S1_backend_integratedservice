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
        [HttpPost]
        [Route("[controller]/api/getbalance")]
        public async Task<GetBalanceResponse_Kiss918> GetBalanceAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<GetBalanceRequest_Kiss918>(content);
            return await _kiss918BLL.GetBalance(request);
        }

        ///// <summary>
        ///// Slot下注
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/v2/bet")]
        //public async Task<AMBSlotBetResponse> SlotBetAsync()
        //{
        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<AMBSlotBetRequest>(content);
        //    return await _AMBBLL.SlotBet(request);
        //}

        ///// <summary>
        ///// Slot派發彩金
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/v2/payout")]
        //public async Task<AMBSlotPayoutResponse> SlotPayoutAsync()
        //{
        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<AMBSlotPayoutRequest>(content);
        //    return await _AMBBLL.SlotPayout(request);
        //}

        ///// <summary>
        ///// Slot取消下注
        ///// </summary>
        ///// <returns></returns>
        //[HttpPost]
        //[Route("[controller]/v2/cancel")]
        //public async Task<AMBSlotCancelResponse> SlotCancelAsync()
        //{
        //    using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        //    var content = await reader.ReadToEndAsync();
        //    var request = JsonConvert.DeserializeObject<AMBSlotCancelRequest>(content);
        //    return await _AMBBLL.SlotCancel(request);
        //}

        #endregion

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }
    }
}
