using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class EBetController : ControllerBase
    {
        private readonly AuthBLL authBLL;
        private readonly ILogger<EBetController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly IHostEnvironment environment;

        public EBetController(
            IHostEnvironment environment,
            ILogger<EBetController> logger,
            AuthBLL authBLL,
            ThirdPartyBLL thirdPartyBLL,
            TpCustInfoBLL tpCustInfoBLL,
            BettingBLL bettingBLL,
            SettledBLL settledBLL,
            Log2File log2File
        )
        {
            this.authBLL = authBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            this.thisTp = Tp.EBet;
            gameType = (int)GameType.EBet_LiveCasino;
            this.environment = environment;
        }

        #region Response

        public class GetRegisterLoginResp
        {
            public string accessToken { get; set; }
            public int subChannelId { get; set; }
            public string username { get; set; }
            public string sessionToken { get; set; }
            public string currency { get; set; }
            public int status { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public string nickname { get; set; }
        }

        public class GetBalanceResp
        {
            public string username { get; set; }
            public decimal money { get; set; }
            public string currency { get; set; }
            public int status { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public long timestamp { get; set; }
        }

        public class GetBetResp
        {
            public string username { get; set; }
            public decimal money { get; set; }
            public string currency { get; set; }
            public int status { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public long timestamp { get; set; }
        }

        #endregion

        [HttpPost]
        //[Produces("application/json")]
        public GetRegisterLoginResp registerOrLogin(RegisterLoginData_EBet registerLoginReq)
        {
            GetRegisterLoginResp getRegisterLoginResp = new GetRegisterLoginResp();
            getRegisterLoginResp.status = 500;
            // check signature
            if (string.IsNullOrEmpty(registerLoginReq.signature))
            {
                getRegisterLoginResp.status = 4026;
                return getRegisterLoginResp;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string sign = $"{registerLoginReq.seqNo}{registerLoginReq.Event}{registerLoginReq.channelId}{registerLoginReq.timestamp}{registerLoginReq.username}{registerLoginReq.accessToken}";
            string privateKey = thirdPartySetting.TpSetting[$"PrivateKey_{registerLoginReq.currency}"];
            var check = VerifySignature(sign, registerLoginReq.signature, privateKey);
            if (!check)
            {
                getRegisterLoginResp.status = 4026;
                return getRegisterLoginResp;
            }

            try
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(registerLoginReq.accessToken)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                /* 1 Time*/
                string authToken = sessionKeyArr[0];
                /* Long Time*/
                string sessionToken = sessionKeyArr[1];

                if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        getRegisterLoginResp.status = 4037;
                        return getRegisterLoginResp;
                    }
                    if (tpCheck.Token != sessionToken)
                    {
                        getRegisterLoginResp.status = 410;
                        return getRegisterLoginResp;
                    }
                }
                else
                {
                    getRegisterLoginResp.status = 410;
                    return getRegisterLoginResp;
                }

                getRegisterLoginResp.status = 200;
                getRegisterLoginResp.accessToken = registerLoginReq.accessToken;
                getRegisterLoginResp.subChannelId = 0;
                getRegisterLoginResp.sessionToken = sessionToken;
                getRegisterLoginResp.username = registerLoginReq.username;
                getRegisterLoginResp.currency = registerLoginReq.currency;
                getRegisterLoginResp.Event = registerLoginReq.Event;
                getRegisterLoginResp.seqNo = registerLoginReq.seqNo;
                getRegisterLoginResp.nickname = registerLoginReq.username;
            }
            catch (Exception ex)
            {
                getRegisterLoginResp.status = 500;
                log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}\r\n{ex}", $"EBet_Register_Login");
            }

            log2File.Info($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", $"EBet_Register_Login");

            return getRegisterLoginResp;
        }

        [HttpPost]
        public GetBalanceResp syncCredit(SyncCreditData_EBet syncCreditReq)
        {
            GetBalanceResp getBalanceResp = new GetBalanceResp();

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, syncCreditReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBalanceResp.status = 4037;
                return getBalanceResp;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, syncCreditReq.username, gameType);
            getBalanceResp.status = 200;
            getBalanceResp.username = syncCreditReq.username;
            getBalanceResp.money = tpCustBalance.Balance;
            getBalanceResp.currency = tpCustBalance.TpCurrencyId;
            getBalanceResp.Event = "syncCredit";
            getBalanceResp.seqNo = syncCreditReq.seqNo;
            getBalanceResp.timestamp = (int)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            log2File.Info($"{JsonConvert.SerializeObject(syncCreditReq)}\n{JsonConvert.SerializeObject(getBalanceResp)}", $"EBet_Sync_Credit");

            return getBalanceResp;
        }

        [HttpPost]
        public async Task<GetBetResp> increaseCreditAsync(BettingData_EBet getBetReq)
        {
            GetBetResp getBetResp = new GetBetResp();
            
            if (getBetReq.type == 1)
            {
                log2File.Info($"{JsonConvert.SerializeObject(getBetReq)}", $"EBet_Betting");
                getBetResp.status = 200;
                getBetResp.seqNo = getBetReq.seqNo;
            }
            else if (getBetReq.type == 2)
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var Content = await reader.ReadToEndAsync();
                log2File.Info(Content, $"EBet_Payout");
                getBetResp.status = 500;
                getBetResp.seqNo = getBetReq.seqNo;
            }
            else
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                var Content = await reader.ReadToEndAsync();
                log2File.Info(Content, $"EBet_Betting");
                getBetResp.status = 500;
                getBetResp.seqNo = getBetReq.seqNo;
            }
            return getBetResp;

            //var syncCreditReq = JsonConvert.DeserializeObject<BettingData_EBet>(Content);



            //GetBetResp getBetResp = new GetBetResp();
            //getBetResp.status = 200;

            //log2File.Info($"{JsonConvert.SerializeObject(Content)}\n{JsonConvert.SerializeObject(getBetResp)}", $"EBet_Betting");
            ////log2File.Info($"{JsonConvert.SerializeObject(getBetReq)}\n{JsonConvert.SerializeObject(getBetResp)}", $"EBet_Betting");

            //return getBetResp;
        }


        private bool VerifySignature(string dataToSign, string signedData, string publicKey)
        {
            //UnicodeEncoding ByteConverter = new UnicodeEncoding();
            var encoder = new UTF8Encoding();
            using var RSAalg = new RSACryptoServiceProvider();
            RSAParameters priKey = RSAalg.ExportParameters(true);
            RSAalg.ImportParameters(priKey);
            RSAalg.FromXmlString(publicKey);
            byte[] plaintext = encoder.GetBytes(dataToSign);
            return RSAalg.VerifyData(plaintext, CryptoConfig.MapNameToOID("MD5"), Convert.FromBase64String(signedData));
        }
    }
}
