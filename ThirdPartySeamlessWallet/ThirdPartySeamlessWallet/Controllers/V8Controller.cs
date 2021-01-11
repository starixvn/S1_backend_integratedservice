using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class V8Controller : ControllerBase
    {
        #region Request

        public class ApiReq
        {
            public string agent { get; set; }
            public long timestamp { get; set; }
            public string param { get; set; }
            public string key { get; set; }
        }

        #endregion

        #region Response

        public class GetApiResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
        }

        public class GetBalanceResp
        {
            public int code { get; set; }
            public decimal money { get; set; }
        }
        #endregion

        private readonly ILogger<V8Controller> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;

        public V8Controller(
            ILogger<V8Controller> logger,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            Log2File log2File,
            Tools tools
        )
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.log2File = log2File;
            this.tools = tools;
            this.thisTp = Tp.V8;
        }

        [HttpGet]
        [Route("[controller]/channelHandle")]
        public GetApiResp channelHandle([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}";
            var subName = "";
            var json = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(json);

            GetApiResp getApiResponse = new GetApiResp
            {
                s = 101,
                m = "/channelHandle",
                d = new info
                {
                    code = 999
                }
            };

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            var param = tools.AesDecrypt(HttpUtility.UrlEncode(apiReq.param), thirdPartySetting.TpSetting["Deskey"]);
            var s = HttpUtility.ParseQueryString(param).Get("s");

            if (s == "11")
            {
                subName = "notify";
                var accountTmp = HttpUtility.ParseQueryString(param).Get("account");
                string[] temp = accountTmp.Split('_');
                var account = $"{temp[1]}_{temp[2]}";
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, account);
                var result = thirdPartyBLL.SetThirdPartyTransferUserList((int)thisTp, tpCustInfo.CustId, (int)Direction.ToMain);
                if (result > 0)
                {
                    getApiResponse.d.code = 0;
                }
                log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
                return getApiResponse;
            }
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]")]
        public GetBalanceResp SeamlessWallet([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}";
            var subName = "";
            var json = JsonConvert.SerializeObject(getReq);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(json);

            GetBalanceResp getBalanceResponse = new GetBalanceResp();
            getBalanceResponse.code = 999;

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            var param = tools.AesDecrypt(HttpUtility.UrlEncode(apiReq.param), thirdPartySetting.TpSetting["Deskey"]);
            var s = HttpUtility.ParseQueryString(param).Get("s");

            if (s == "12")
            {
                subName = "balance";
                var account = HttpUtility.ParseQueryString(param).Get("account");
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, account);
                if (tpCheck == null)
                {
                    getBalanceResponse.code = 4;
                    log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }
                else if (tpCheck.Locked)
                {
                    getBalanceResponse.code = 4;
                    log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, account);
                if (tpCustBalance != null)
                {
                    getBalanceResponse.code = 0;
                    getBalanceResponse.money = tpCustBalance.Balance;
                }

                return getBalanceResponse;
            }
            else if (s == "13")
            {
                subName = "deposit";
                var account = HttpUtility.ParseQueryString(param).Get("account");
                var money = HttpUtility.ParseQueryString(param).Get("money");
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, account);
                if (tpCheck == null)
                {
                    getBalanceResponse.code = 4;
                    log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }
                else if (tpCheck.Locked)
                {
                    getBalanceResponse.code = 4;
                    log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }

                try
                {
                    BettingDataPK_V8 deposit = new BettingDataPK_V8();
                    deposit.account = account;
                    deposit.money = Convert.ToDecimal(money);
                    deposit.timestamp = apiReq.timestamp;

                    var result = bettingBLL.PlaceBetPK_V8(deposit, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        getBalanceResponse.code = 0;
                        getBalanceResponse.money = tpAffterAmount;
                        log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, account);
                        getBalanceResponse.code = 0;
                        getBalanceResponse.money = tpCustBalance.Balance;
                        log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, account);
                        getBalanceResponse.code = 1005;
                        getBalanceResponse.money = tpCustBalance.Balance;
                        log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{ex}", $"{logName}_{subName}");
                }
                return getBalanceResponse;
            }
            return getBalanceResponse;
        }
    }
}
