using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<MegaController> _logger;
        private readonly IHostEnvironment _environment;
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp thisTp;

        public MegaController(
            IServiceProvider provider,
            ILogger<MegaController> logger,
            IHostEnvironment environment
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _environment = environment;
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tools = provider.GetRequiredService<Tools>();
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
            _log2File.Info(Content, $"{thisTp}_open.operator.user.login");
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
                if (_environment.IsDevelopment())
                {
                    resp.error = "Not allowed environment";
                    _log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfo((int)thisTp, getLoginReq.Params.loginId);
                if (tpCustInfo == null)
                {
                    resp.error = "Invalid user Id";
                    _log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                
                var tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)thisTp, getLoginReq.Params.loginId);
                if (tpCheck == null)
                {
                    tpCheck = _thirdPartyBLL.ThirdPartyCheck((int)thisTp, getLoginReq.Params.loginId);
                }

                if (tpCheck.Locked)
                {
                    resp.error = "Account is Lock";
                    _log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (string.IsNullOrEmpty(getLoginReq.Params.password))
                {
                    resp.error = "Invalid password";
                    _log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }

                if (tpCustInfo.TpPwd == getLoginReq.Params.password)
                {
                    _tpCustInfoBLL.AddLoginRecord(tpCustInfo.CustId, tpCustInfo.SiteId, "", out _);
                    _tpCustInfoBLL.AddUserLoginLog(tpCustInfo.CustId, tpCustInfo.UserName, tpCustInfo.SiteId, "", "App", "App");
                    resp.result.success = "1";
                    resp.result.sessionId = tpCheck.Token;
                    resp.result.msg = "login success";
                    _log2File.Info($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                else
                {
                    resp.error = "Password failed";
                    _log2File.Error($"{JsonConvert.SerializeObject(getLoginReq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                }
                return resp;
            }

            resp.error = "404 failed";
            _log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }
    }
}
