using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]

    public class AMBController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly ILogger<AMBController> logger;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;
        private readonly IHostEnvironment environment;

        public AMBController(
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            ILogger<AMBController> logger,
            Log2File log2File,
            Tools tools,
            IHostEnvironment environment
        )
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.tools = tools;
            this.thisTp = Tp.AMB;
            this.environment = environment;
        }

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

        [HttpPost]
        [Route("[controller]/balance")]
        public async Task<dynamic> balanceAsync()
        {
            string logName = $"{thisTp}_balance";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);

            BalanceReq getReq = JsonConvert.DeserializeObject<BalanceReq>(Content);

            BalanceResp getApiResponse = new BalanceResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getApiResponse.status.code = 997;
                getApiResponse.status.message = "Invalid request data";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getReq.username, (int)GameType.AMB_Poker);
            if (tpCustBalance != null)
            {
                getApiResponse.status.code = 0;
                getApiResponse.status.message = "Success";
                getApiResponse.data.balance = tpCustBalance.Balance;
                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
            return getApiResponse;
        }

        [HttpPost]
        [Route("[controller]/bet")]
        public async Task<dynamic> betAsync()
        {
            string logName = $"{thisTp}_bet";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, logName);

            BettingDataPK_AMB getReq = JsonConvert.DeserializeObject<BettingDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getApiResponse.status.code = 997;
                getApiResponse.status.message = "Invalid request data";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }

            try
            {
                var result = bettingBLL.PlaceBetPK_AMB(getReq, out decimal tpAffterAmount);
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

                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
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
            log2File.Info(Content, logName);

            CancelDataPK_AMB getReq = JsonConvert.DeserializeObject<CancelDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            try
            {
                var result = settledBLL.CancelBetPK_AMB(getReq, out decimal tpAffterAmount);
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
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    getApiResponse.status.code = 807;
                    getApiResponse.status.message = "Transaction already canceled";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
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
            log2File.Info(Content, logName);

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
                var result = settledBLL.PayoutPK_AMB(getReq, out decimal tpAffterAmount);
                if (result > 0)
                {
                    if (tpAffterAmount == 0)
                    {
                        tpAffterAmount = tpCustInfoBLL.GetBalance((int)thisTp, getReq.username).Balance;
                    }
                    getApiResponse.status.code = 0;
                    getApiResponse.status.message = "Success";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.wallet.balance = tpAffterAmount;
                    getApiResponse.data.wallet.lastUpdate = getCurrentGameTime();
                    getApiResponse.data.balance.before = tpAffterAmount - getReq.amount;
                    getApiResponse.data.balance.after = tpAffterAmount;
                    getApiResponse.data.refId = getReq.refId;
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }
                else if (result == -1)
                {
                    getApiResponse.status.code = 806;
                    getApiResponse.status.message = "Duplicate Round Id";
                    getApiResponse.data.username = getReq.username;
                    getApiResponse.data.refId = getReq.refId;
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
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
            log2File.Info(Content, logName);

            VoidDataPK_AMB getReq = JsonConvert.DeserializeObject<VoidDataPK_AMB>(Content);

            BetResp getApiResponse = new BetResp();
            getApiResponse.status.code = 999;
            getApiResponse.status.message = "Service not available";

            try
            {
                var result = settledBLL.ResettlePK_AMB(getReq, out decimal tpAffterAmount);
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
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
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
                    log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                    return getApiResponse;
                }

                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getApiResponse)}", logName);
                return getApiResponse;
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(getReq)}\r\n{JsonConvert.SerializeObject(getApiResponse)}\r\n{ex}", logName, ex);
                return getApiResponse;
            }
        }

        private string getCurrentGameTime()
        {
            var currentAt = new DateTimeOffset(DateTime.Now);
            return currentAt.ToOffset(new TimeSpan(8, 0, 0)).ToString("yyyy-MM-dd'T'HH:mm:ss.fffZ");
        }
    }
}
