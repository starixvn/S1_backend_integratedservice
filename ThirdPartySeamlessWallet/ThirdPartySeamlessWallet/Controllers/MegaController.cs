using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Org.BouncyCastle.Asn1.Ocsp;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class MegaController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tp thisTp;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly IHostEnvironment environment;

        public MegaController(
            IHostEnvironment environment,
            Log2File log2File,
            ThirdPartyBLL thirdPartyBLL,
            AuthBLL authBLL,
            TpCustInfoBLL tpCustInfoBLL
        )
        {
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.environment = environment;
            thisTp = Tp.Mega;
        }

        #region Request

        public class GetLoginReq
        {
            public string id { get; set; }
            public string method { get; set; }
            public Params Params { get; set; }
            public string jsonrpc { get; set; }
        }

        public class Params
        {
            public string sn { get; set; }
            public string loginId { get; set; }
            public string password { get; set; }
            public string random { get; set; }
            public string digest { get; set; }

        }

        #endregion

        #region Response

        public class GetSeamlessWalletResp
        {
            public string id { get; set; }
            public Result result { get; set; }
            public string error { get; set; }
            public string jsonrpc { get; set; }
        }

        public class Result
        {
            public string success { get; set; }
            public string sessionId { get; set; }
            public string msg { get; set; }

        }

        #endregion

        [Route("[controller]")]
        [HttpPost]
        public async Task<GetSeamlessWalletResp> SeamlessWalletAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, $"{thisTp}_open.operator.user.login");
            var reqData = Content.TrimStart("json=".ToArray());

            GetLoginReq getLoginReq = JsonConvert.DeserializeObject<GetLoginReq>(reqData);
            GetSeamlessWalletResp resp = new GetSeamlessWalletResp
            {
                id = getLoginReq.id,
                jsonrpc = getLoginReq.jsonrpc,
                error = null,
                result = new Result
                {
                    success = "0",
                    sessionId = "",
                    msg = "failed",
                }
            };

            string logName = $"{thisTp}_{getLoginReq.method}";
            if (string.Equals(getLoginReq.method, "open.operator.user.login", StringComparison.OrdinalIgnoreCase))
            {
                var tpCustInfo = tpCustInfoBLL.GetTpCustInfo((int)thisTp, getLoginReq.Params.loginId);
                if (tpCustInfo == null)
                {
                    resp.error = "Invalid user Id";
                    log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, getLoginReq.Params.loginId);
                if (tpCheck == null)
                {
                    tpCustInfoBLL.AddLoginRecord(tpCustInfo.CustId, tpCustInfo.SiteId, "", out _);
                    tpCustInfoBLL.AddUserLoginLog(tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, "", "App", "App");
                    tpCheck = thirdPartyBLL.ThirdPartyCheck((int)thisTp, getLoginReq.Params.loginId);
                }

                if (tpCheck.Locked)
                {
                    resp.error = "Account is Lock";
                    log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (string.IsNullOrEmpty(getLoginReq.Params.password))
                {
                    resp.error = "Invalid password";
                    log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCustInfo.TpPwd == getLoginReq.Params.password)
                {
                    resp.result.success = "1";
                    resp.result.sessionId = tpCheck.Token;
                    resp.result.msg = "login success";
                    log2File.Info($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                else
                {
                    resp.error = "Password failed";
                    log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                return resp;
            }

            resp.error = "404 failed";
            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }
    }
}
