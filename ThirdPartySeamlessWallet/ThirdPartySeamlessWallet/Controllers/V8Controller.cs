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

        public class GetSeamlessWalletResp
        {
            public int s { get; set; }
            public string m { get; set; }
            public info d { get; set; }
        }

        public class info
        {
            public int code { get; set; }
        }
        #endregion

        private readonly ILogger<V8Controller> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly Log2File log2File;
        private readonly Tools tools;
        private readonly Tp thisTp;

        public V8Controller(
            ILogger<V8Controller> logger,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            Log2File log2File,
            Tools tools
        )
        {
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.log2File = log2File;
            this.tools = tools;
            this.thisTp = Tp.V8;
        }

        [HttpGet]
        [Route("[controller]/channelHandle")]
        public GetSeamlessWalletResp channelHandle([FromQuery] Dictionary<string, string> getReq)
        {
            string logName = $"{thisTp}_channelHandle";
            var json = JsonConvert.SerializeObject(getReq);
            log2File.Info(json, logName);
            var apiReq = JsonConvert.DeserializeObject<ApiReq>(json);

            GetSeamlessWalletResp getApiResponse = new GetSeamlessWalletResp
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
                var subName = "notify";
                var account = HttpUtility.ParseQueryString(param).Get("account");
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, account);
                var result = thirdPartyBLL.SetThirdPartyTransferUserList((int)thisTp, tpCustInfo.CustId, (int)Direction.ToMain);
                if (result > 0)
                {
                    getApiResponse.d.code = 0;
                }
                log2File.Info($"{json}\r\n{param}\r\n{JsonConvert.SerializeObject(getApiResponse)}", $"{logName}_{subName}");
            }

            return getApiResponse;
        }
    }
}
