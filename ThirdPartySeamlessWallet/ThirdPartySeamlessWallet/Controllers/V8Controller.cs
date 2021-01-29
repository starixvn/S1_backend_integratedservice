using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
        private readonly IHttpClientFactory clientFactory;
        private readonly string apiUrl;

        public V8Controller(
            ILogger<V8Controller> logger,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            Log2File log2File,
            Tools tools,
            IHttpClientFactory clientFactory,
            IConfiguration configuration
        )
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.log2File = log2File;
            this.tools = tools;
            thisTp = Tp.V8;
            this.clientFactory = clientFactory;
            apiUrl = configuration.GetValue<string>("TpService");
        }
        public class ApiResult<TData>
        {
            public ApiResult()
            {
                ErrorCode = 9000;
            }
            public int ErrorCode { get; set; }
            public string Message { get; set; }
            public TData Data { get; set; }
        }
        public class TransferResult
        {
            public TransferResult()
            {
                Status = 2;
            }
            public int Status { get; set; }
            public string ThirdId { get; set; }
            public decimal Difference { get; set; }
            public decimal Bonus { get; set; }
            public decimal Rollover { get; set; }
            public decimal Turnover { get; set; }
            public int BonusStatus { get; set; }
        }

        [HttpGet]
        [Route("[controller]/channelHandle")]
        public async Task<GetApiResp> channelHandleAsync([FromQuery] Dictionary<string, string> getReq)
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
                log2File.Info($"{json}\r\n{param}", $"{logName}_{subName}");

                var accountTmp = HttpUtility.ParseQueryString(param).Get("account");
                string account = accountTmp;

                if (accountTmp.IndexOf("_") > -1)
                {
                    accountTmp.Substring(accountTmp.IndexOf("_") + 1);
                }

                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, account);

                /*Transfer In*/
                var client = clientFactory.CreateClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Dictionary<string, string> input = new Dictionary<string, string>();
                //input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                //input.Add("ClientIP", tools.GetClientIP());
                input.Add("AllOut", null);

                var fooJSON = JsonConvert.SerializeObject(new
                {
                    TpId = (int)thisTp,
                    CustId = tpCustInfo.CustId,
                    Amount = 1,
                    direction = 2,
                    inputs = input
                });

                var fooContent = new StringContent(fooJSON, Encoding.UTF8, "application/json");
                var myUrl = $"{apiUrl}Transfer";
                HttpResponseMessage response = await client.PostAsync(myUrl, fooContent);
                ApiResult<TransferResult> oApiResult = new ApiResult<TransferResult>();

                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    oApiResult = JsonConvert.DeserializeObject<ApiResult<TransferResult>>(responseBody);
                }

                if (oApiResult.ErrorCode == 0)
                {
                    getApiResponse.d.code = 0;
                    log2File.Info($"{json}\r\n{JsonConvert.SerializeObject(oApiResult)}\r\n{param}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
                }
                else
                {
                    log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(oApiResult)}", $"{logName}_{subName}");
                }

                return getApiResponse;
            }
            return getApiResponse;
        }

        [HttpGet]
        [Route("[controller]")]
        public async Task<GetBalanceResp> SeamlessWalletAsync([FromQuery] Dictionary<string, string> getReq)
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

                log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                return getBalanceResponse;
            }
            else if (s == "13")
            {
                subName = "TransferOut";
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
                    var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, account);
                    /*Transfer Out*/

                    var client = clientFactory.CreateClient();
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    //input.Add("Host", $"{Request.Scheme}://{Request.Host}{Request.PathBase}");
                    //input.Add("ClientIP", tools.GetClientIP());
                    var fooJSON = JsonConvert.SerializeObject(new
                    {
                        TpId = (int)thisTp,
                        CustId = tpCustInfo.CustId,
                        Amount = Convert.ToDecimal(money),
                        direction = 1
                    });

                    var fooContent = new StringContent(fooJSON, Encoding.UTF8, "application/json");
                    var myUrl = $"{apiUrl}Transfer";
                    HttpResponseMessage response = await client.PostAsync(myUrl, fooContent);
                    ApiResult<TransferResult> oApiResult = new ApiResult<TransferResult>();

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        oApiResult = JsonConvert.DeserializeObject<ApiResult<TransferResult>>(responseBody);
                    }

                    if (oApiResult.ErrorCode == 0)
                    {
                        getBalanceResponse.code = 0;
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, account);
                        getBalanceResponse.money = tpCustBalance.Balance;
                    }
                    else if (oApiResult.Data.Difference != 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, account);
                        getBalanceResponse.code = 1005;
                        getBalanceResponse.money = tpCustBalance.Balance;
                    }
                    else
                    {
                        log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(oApiResult)}", $"{logName}_{subName}");
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}\r\n{ex}", $"{logName}_{subName}");
                    return getBalanceResponse;
                }

                log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getBalanceResponse)}", $"{logName}_{subName}");
                return getBalanceResponse;
            }
            return getBalanceResponse;
        }
    }
}
