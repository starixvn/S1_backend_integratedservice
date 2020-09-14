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
            public decimal moneyBefore { get; set; }
            public int status { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public long timestamp { get; set; }
        }

        public class GetCreditRecordResp
        {
            public string seqNo { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public long timestamp { get; set; }
            public string username { get; set; }
            public IEnumerable<CreditRecord_EBet> creditRecord { get; set; }
            public int status { get; set; }
        }
        public class GetCancelBetResp
        {
            public int status { get; set; }
            public string seqNo { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public long timestamp { get; set; }
            public decimal moneyBefore { get; set; }
            public decimal moneyAfter { get; set; }
            public decimal refundMoney { get; set; }
            public IList<RefundList> refundList { get; set; }
        }

        public class RefundList
        {
            public string refundSeqNo { get; set; }
            public int status { get; set; }
        }

        public class GetBatchCancelBetResp
        {
            public int status { get; set; }
            public long timestamp { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public IList<RefundResultList> refundResultList { get; set; }
        }

        public class RefundResultList
        {
            public decimal refundTotalMoney { get; set; }
            public string roundCode { get; set; }
            public string username { get; set; }
            public IList<SucRefundSeqNoList> sucRefundSeqNoList { get; set; }
        }

        public class SucRefundSeqNoList
        {
            public int status { get; set; }
            public string seqNo { get; set; }
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
            getBalanceResp.Event = syncCreditReq.Event;
            getBalanceResp.seqNo = syncCreditReq.seqNo;
            getBalanceResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            log2File.Info($"{JsonConvert.SerializeObject(syncCreditReq)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", $"EBet_Sync_Credit");

            return getBalanceResp;
        }

        [HttpPost]
        public async Task<GetBetResp> increaseCreditAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            BettingData_EBet getBetReq = JsonConvert.DeserializeObject<BettingData_EBet>(Content);

            GetBetResp getBetResp = new GetBetResp();
            getBetResp.status = 500;
            getBetResp.money = 0;
            getBetResp.seqNo = getBetReq.seqNo;
            getBetResp.username = getBetReq.username;
            getBetResp.Event = getBetReq.Event;
            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // check signature
            if (string.IsNullOrEmpty(getBetReq.signature))
            {
                getBetResp.status = 4026;
                getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return getBetResp;
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
            string sign = $"{getBetReq.seqNo}{getBetReq.Event}{getBetReq.channelId}{getBetReq.timestamp}{getBetReq.username}{getBetReq.money}";
            string privateKey = thirdPartySetting.TpSetting[$"PrivateKey_{getBetReq.currency}"];
            var check = VerifySignature(sign, getBetReq.signature, privateKey);
            if (!check)
            {
                getBetResp.status = 4026;
                getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                return getBetResp;
            }


            var result = 0;
            try
            {
                switch (getBetReq.type)
                {
                    case 1:
                        // bet
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBetReq.username);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            getBetResp.status = 4037;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            return getBetResp;
                        }

                        if (tpCheck.Token != HttpUtility.UrlDecode(getBetReq.sessionToken))
                        {
                            getBetResp.status = 410;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            return getBetResp;
                        }
                        result = bettingBLL.PlaceBetLC_EBet(getBetReq, out decimal tpAffterAmount);
                        if (result > 0)
                        {
                            getBetResp.status = 200;
                            getBetResp.money = tpAffterAmount;
                            getBetResp.moneyBefore = tpAffterAmount - getBetReq.money;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetReq.username);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetReq.username);
                            getBetResp.status = 500;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        log2File.Info($"{JsonConvert.SerializeObject(getBetReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", $"EBet_Bet");

                        break;
                    case 2:
                        // payout
                        
                        var data = settledBLL.CheckPayoutLC_EBet(getBetResp.seqNo);
                        // check seqNo
                        if (string.IsNullOrEmpty(data.seqNo))
                        {
                            getBetResp.status = 201;
                        }
                        result = settledBLL.PayoutLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0 || result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        log2File.Info($"{JsonConvert.SerializeObject(getBetReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", $"EBet_Payout");

                        break;
                    case 11:
                        // cancel bet
                        result = settledBLL.CancelBetLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {

                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        log2File.Info($"{JsonConvert.SerializeObject(getBetReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}", $"EBet_Cancel_Bet");

                        break;
                    case 27:
                        // pre-bet
                        /*
                         * 投注: 先请求预扣（type = 27），
                         * 如果预扣响应成功，则请求下注（type = 1）。 
                         * 预扣回应失败则使用基本流程的投注失败处理。
                         * 如果投注回应失败，
                         * 会使用下注seqNo将下注错误退款（type = 11）
                         * 请求发送至渠道以要求退还预扣款。
                         */
                        break;
                    case 28:
                        // pre-refund
                        /*
                         * 派彩: 如果还有剩余的预扣金额，
                         * 请先请求预扣返还（type = 28），
                         * 然后再请求派彩（type = 2）。
                         * 如果没有剩余的预扣金额，
                         * 则直接请求派彩（type = 2）。
                         */

                        break;
                    default:
                        break;
                }

                return getBetResp;
            }
            catch (Exception ex)
            {
                getBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{JsonConvert.SerializeObject(getBetReq)}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex}", $"EBet_increaseCredit");
                return getBetResp;
            }
        }

        [HttpPost]
        public async Task<GetCreditRecordResp> queryIncreaseCreditRecordAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            log2File.Info(Content, $"EBet_queryIncreaseCreditRecord");
            CreditRecordData_EBet getCreditRecordReq = JsonConvert.DeserializeObject<CreditRecordData_EBet>(Content);

            GetCreditRecordResp getCreditRecordResp = new GetCreditRecordResp();
            getCreditRecordResp.status = 200;
            getCreditRecordResp.seqNo = getCreditRecordReq.seqNo;
            getCreditRecordResp.Event = getCreditRecordReq.Event;
            getCreditRecordResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            getCreditRecordResp.username = getCreditRecordReq.username;

            var data = settledBLL.GetCreditRecordLC_EBet(getCreditRecordReq.seqNo);
            if (data == null || data.Count() == 0)
            {
                // seqNo 不存在
                getCreditRecordResp.status = 206;
            }
            getCreditRecordResp.creditRecord = data;
            log2File.Info($"{JsonConvert.SerializeObject(getCreditRecordReq)}\r\n{JsonConvert.SerializeObject(getCreditRecordResp)}", $"EBet_queryIncreaseCreditRecord");

            return getCreditRecordResp;
        }

        [HttpPost]
        public async Task<GetCancelBetResp> refundSingleWalletAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            CancelBetData_EBet getCancelBetReq = JsonConvert.DeserializeObject<CancelBetData_EBet>(Content);
            GetCancelBetResp getCancelBetResp = new GetCancelBetResp();
            getCancelBetResp.status = 500;
            getCancelBetResp.seqNo = getCancelBetReq.seqNo;
            getCancelBetResp.Event = getCancelBetReq.Event;
            getCancelBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            getCancelBetResp.moneyBefore = 0;
            getCancelBetResp.moneyAfter = 0;
            getCancelBetResp.refundMoney = 0;
            ////bet
            //getCancelBetResp.refundList.refundSeqNo = 500;
            //getCancelBetResp.refundList.status = 500;
            log2File.Info(Content, $"EBet_refundSingleWallet");
            return getCancelBetResp;
        }

        [HttpPost]
        public async Task<GetBatchCancelBetResp> autoBatchRefundAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            BatchCancelBetData_EBet getBatchCancelBetReq = JsonConvert.DeserializeObject<BatchCancelBetData_EBet>(Content);
            GetBatchCancelBetResp getBatchCancelBetResp = new GetBatchCancelBetResp();
            getBatchCancelBetResp.status = 500;
            getBatchCancelBetResp.seqNo = getBatchCancelBetReq.seqNo;
            getBatchCancelBetResp.Event = getBatchCancelBetResp.Event;
            getBatchCancelBetResp.timestamp = (long)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            log2File.Info(Content, $"EBet_autoBatchRefund");
            return getBatchCancelBetResp;
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
