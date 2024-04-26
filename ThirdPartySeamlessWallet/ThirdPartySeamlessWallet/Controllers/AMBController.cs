using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]

    public class AMBController : ControllerBase
    {

        #region Fields

        private readonly Tp thisTp;

        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly IAMBBLL _AMBBLL;

        #endregion

        #region Constructors

        public AMBController(IServiceProvider provider)
        {
            thisTp = Tp.AMB;

            _thirdPartyBLL = provider.GetService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetService<TpCustInfoBLL>();
            _bettingBLL = provider.GetService<BettingBLL>();
            _settledBLL = provider.GetService<SettledBLL>();
            _log2File = provider.GetService<Log2File>();

            _AMBBLL = provider.GetService<IAMBBLL>();
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

        #region AMB Poker Actions

        [HttpPost]
        [Route("[controller]/balance")]
        public async Task<dynamic> balanceAsync()
        {
            string logName = $"{thisTp}_balance";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            BalanceReq getReq = JsonConvert.DeserializeObject<BalanceReq>(Content);

            BalanceResp getApiResponse = new BalanceResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getApiResponse.status.code = 997;
                getApiResponse.status.message = "Invalid request data";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.username, (int)GameType.AMB_Poker);
            if (tpCustBalance != null)
            {
                getApiResponse.status.code = 0;
                getApiResponse.status.message = "Success";
                getApiResponse.data.balance = tpCustBalance.Balance;
                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/bet")]
        public async Task<dynamic> betAsync()
        {
            string logName = $"{thisTp}_bet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            BettingDataPK_AMB getReq = JsonConvert.DeserializeObject<BettingDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getApiResponse.status.code = 997;
                getApiResponse.status.message = "Invalid request data";
                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                var result = _bettingBLL.PlaceBetPK_AMB(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getApiResponse.status.code = 0;
                    getApiResponse.status.message = "Success";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.wallet.balance = tpAffterAmount;
                    getApiResponse.data.wallet.lastUpdate = getCurrentGameTime();
                    getApiResponse.data.balance.before = tpAffterAmount + getReq.amount;
                    getApiResponse.data.balance.after = tpAffterAmount;
                    getApiResponse.data.refId = getReq.refId;
                }
                else if (result == -1)
                {
                    getApiResponse.status.code = 806;
                    getApiResponse.status.message = "Duplicate Round Id";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                }
                else if (tpAffterAmount < 0)
                {
                    getApiResponse.status.code = 800;
                    getApiResponse.status.message = "Balance insufficient";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                }

                _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/cancel")]
        public async Task<dynamic> cancelAsync()
        {
            string logName = $"{thisTp}_cancel";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            CancelDataPK_AMB getReq = JsonConvert.DeserializeObject<CancelDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            try
            {
                var result = _settledBLL.CancelBetPK_AMB(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getApiResponse.status.code = 0;
                    getApiResponse.status.message = "Success";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.wallet.balance = tpAffterAmount;
                    getApiResponse.data.wallet.lastUpdate = getCurrentGameTime();
                    getApiResponse.data.balance.before = tpAffterAmount + getReq.amount;
                    getApiResponse.data.balance.after = tpAffterAmount;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    getApiResponse.status.code = 807;
                    getApiResponse.status.message = "Transaction already canceled";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/payout")]
        public async Task<dynamic> payoutAsync()
        {
            string logName = $"{thisTp}_payout";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            PayoutDataPK_AMB getReq = JsonConvert.DeserializeObject<PayoutDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            //if (environment.IsDevelopment())
            //{
            //    System.Threading.Thread.Sleep(4000);
            //    log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            //    return getApiResponse;
            //}

            try
            {
                var result = _settledBLL.PayoutPK_AMB(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = _tpCustInfoBLL.GetBalance((int)thisTp, getReq.username).Balance;
                    }
                    getApiResponse.status.code = 0;
                    getApiResponse.status.message = "Success";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.wallet.balance = tpAffterAmount;
                    getApiResponse.data.wallet.lastUpdate = getCurrentGameTime();
                    getApiResponse.data.balance.before = tpAffterAmount - getReq.amount;
                    getApiResponse.data.balance.after = tpAffterAmount;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    getApiResponse.status.code = 806;
                    getApiResponse.status.message = "Duplicate Round Id";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                return getApiResponse;
            }
        }

        [HttpPost]
        [Route("[controller]/void")]
        public async Task<dynamic> voidAsync()
        {
            string logName = $"{thisTp}_void";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Info(Content, logName);

            VoidDataPK_AMB getReq = JsonConvert.DeserializeObject<VoidDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            try
            {
                var result = _settledBLL.ResettlePK_AMB(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    getApiResponse.status.code = 0;
                    getApiResponse.status.message = "Success";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.wallet.balance = tpAffterAmount;
                    getApiResponse.data.wallet.lastUpdate = getCurrentGameTime();
                    getApiResponse.data.balance.before = tpAffterAmount + getReq.amount;
                    getApiResponse.data.balance.after = tpAffterAmount;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    //getApiResponse.status.code = 807;
                    //getApiResponse.status.message = "Transaction already voided";

                    getApiResponse.status.code = 806;
                    getApiResponse.status.message = "Duplicate Round Id";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                    _log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                _log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                return getApiResponse;
            }
        }

        #endregion

        #region AMB Slot Actions

        /// <summary>
        /// Slot取得餘額
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/v2/balance")]
        public AMBSlotGetBalanceResponse SlotGetBalanceAsync(ANBSlotGetBalanceRequest request)
        {
            return _AMBBLL.SlotGetBalance(request);
        }

        /// <summary>
        /// Slot下注
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/v2/bet")]
        public async Task<AMBSlotBetResponse> SlotBetAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AMBSlotBetRequest>(content);
            return await _AMBBLL.SlotBet(request);
        }

        /// <summary>
        /// Slot派發彩金
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/v2/payout")]
        public async Task<AMBSlotPayoutResponse> SlotPayoutAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AMBSlotPayoutRequest>(content);
            return await _AMBBLL.SlotPayout(request);
        }

        /// <summary>
        /// Slot取消下注
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/v2/cancel")]
        public async Task<AMBSlotCancelResponse> SlotCancelAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AMBSlotCancelRequest>(content);
            return await _AMBBLL.SlotCancel(request);
        }

        /// <summary>
        /// Slot取消下注
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("[controller]/v2/settle")]
        public async Task<AMBSlotSettleResponse> SlotSettleAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var content = await reader.ReadToEndAsync();
            var request = JsonConvert.DeserializeObject<AMBSlotSettleRequest>(content);
            return await _AMBBLL.SlotSettle(request);
        }

        #endregion

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }
    }
}
