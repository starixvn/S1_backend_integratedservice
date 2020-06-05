using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class PGController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly AuthBLL authBLL;

        public PGController(Log2File log2File, ThirdPartyBLL thirdPartyBLL, AuthBLL authBLL)
        {
            this.log2File = log2File;
            thisTp = Tp.PG;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
        }

        public class VerifySessionReq
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }

            [JsonProperty("game_id")]
            public long GameId { get; set; }

            [JsonProperty("ip")]
            public string Ip { get; set; }
        }

        public partial class Resp
        {
            [JsonProperty("data")]
            public RespData Data { get; set; }

            [JsonProperty("error")]
            public int? Error { get; set; }
        }

        public partial class RespData
        {
            [JsonProperty("player_name")]
            public string PlayerName { get; set; }

            [JsonProperty("nickname")]
            public string Nickname { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("reminder_time")]
            public string ReminderTime { get; set; }
        }

        [Route("[controller]/Auth")]
        [HttpPost]
        public Resp VerifySessionAsync(string traceId, [FromForm]Dictionary<string, string> reqq)
        {
            Resp resp = new Resp();
            resp.Error = 1034;

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}", $"{thisTp}_VerifySession");

            var verifySessionReq = JsonConvert.DeserializeObject<VerifySessionReq>(JsonConvert.SerializeObject(reqq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string secret_key = thirdPartySetting.TpSetting["secret_key"];
            string operator_token = thirdPartySetting.TpSetting["operator_token"];

            if (!verifySessionReq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !verifySessionReq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.Error = 1034;
                return resp;
            }

            if (!string.IsNullOrEmpty(verifySessionReq.OperatorPlayerSession))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(verifySessionReq.OperatorPlayerSession));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null)
                    {
                        resp.Error = 1308;
                    }
                    else if (tpCheck.Token != sessionToken)
                    {
                        resp.Error = 1308;
                    }
                    else if (tpCheck.Locked)
                    {
                        resp.Error = 1309;
                    }
                    else
                    {
                        RespData respData = new RespData(); ;
                        resp.Data = respData;

                        respData.Currency = tpCustInfoAuth.TpCurrencyId;
                        respData.Nickname = tpCustInfoAuth.TpUserName;
                        respData.PlayerName = tpCustInfoAuth.TpUserName;
                        respData.ReminderTime = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                        resp.Error = null;
                    }
                }
                else
                {
                    resp.Error = 1300;
                }
            }
            else
            {
                resp.Error = 1301;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_VerifySession");

            return resp;
        }
    }
}