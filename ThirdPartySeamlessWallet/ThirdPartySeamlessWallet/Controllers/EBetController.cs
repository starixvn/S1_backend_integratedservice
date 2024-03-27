using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.AspNetCore.Mvc;
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
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly int gameType = 0;
        private readonly EBetConfig _siteEBetConfig;

        public EBetController(IServiceProvider provider)
        {
            authBLL = provider.GetRequiredService<AuthBLL>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            log2File = provider.GetRequiredService<Log2File>();
            thisTp = Tp.EBet;
            gameType = (int)GameType.EBet_LiveCasino;
            _siteEBetConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<EBetConfig>(Tp.EBet.ToString());
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

        public class GetRefundSingleWalletResp
        {
            public int status { get; set; }
            public string seqNo { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public long timestamp { get; set; }
            public decimal moneyBefore { get; set; }
            public decimal moneyAfter { get; set; }
            public decimal refundMoney { get; set; }
            public List<RefundList> refundList { get; set; }
        }

        public class RefundList
        {
            public string refundSeqNo { get; set; }
            public int status { get; set; }
        }

        public class GetAutoBatchRefundResp
        {
            public int status { get; set; }
            public long timestamp { get; set; }
            [JsonProperty("event")]
            public string Event { get; set; }
            public string seqNo { get; set; }
            public IEnumerable<RefundResultList> refundResultList { get; set; }
        }

        public class RefundResultList
        {
            public decimal refundTotalMoney { get; set; }
            public string roundCode { get; set; }
            public string username { get; set; }
            public List<SucRefundSeqNoList> sucRefundSeqNoList { get; set; }
        }

        public class SucRefundSeqNoList
        {
            public int status { get; set; }
            public string seqNo { get; set; }
        }
        
        public class EBetConfig
        {
            public string ApiUrl { get; set; }
            public List<Currency> Currency { get; set; }
        }
        
        public class Currency
        {
            public string CurrencyId { get; set; }
            public string GameUrl { get; set; }
            public int ChannelId { get; set; }
            public string PublicKey { get; set; }
            public string PrivateKey { get; set; }
        }

        #endregion

        [HttpPost]
        public GetRegisterLoginResp registerOrLogin(RegisterLoginData_EBet registerLoginReq)
        {
            string logName = $"{thisTp}_Register_Login";

            GetRegisterLoginResp getRegisterLoginResp = new GetRegisterLoginResp();
            getRegisterLoginResp.status = 500;
            // check signature
            if (string.IsNullOrEmpty(registerLoginReq.signature))
            {
                getRegisterLoginResp.status = 4026;
                log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", logName);
                return getRegisterLoginResp;
            }
            
            string sign = $"{registerLoginReq.seqNo}{registerLoginReq.Event}{registerLoginReq.channelId}{registerLoginReq.timestamp}{registerLoginReq.username}{registerLoginReq.accessToken}";
            string privateKey = _siteEBetConfig.Currency.FirstOrDefault(x => x.CurrencyId == registerLoginReq.currency).PrivateKey;
            var check = VerifySignature(sign, registerLoginReq.signature, privateKey);
            if (!check)
            {
                getRegisterLoginResp.status = 4026;
                log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", logName);
                return getRegisterLoginResp;
            }

            try
            {
                if (authBLL.Authenticate(HttpUtility.UrlDecode(registerLoginReq.accessToken), out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null || tpCheck.Locked)
                    {
                        getRegisterLoginResp.status = 4037;
                        log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", logName);
                        return getRegisterLoginResp;
                    }

                    getRegisterLoginResp.status = 200;
                    getRegisterLoginResp.accessToken = registerLoginReq.accessToken;
                    getRegisterLoginResp.subChannelId = 0;
                    getRegisterLoginResp.sessionToken = HttpUtility.UrlEncode(tpCheck.Token);
                    getRegisterLoginResp.username = registerLoginReq.username;
                    getRegisterLoginResp.currency = registerLoginReq.currency;
                    getRegisterLoginResp.Event = registerLoginReq.Event;
                    getRegisterLoginResp.seqNo = registerLoginReq.seqNo;
                    getRegisterLoginResp.nickname = registerLoginReq.username;
                }
                else
                {
                    getRegisterLoginResp.status = 410;
                    log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", logName);
                    return getRegisterLoginResp;
                }
            }
            catch (Exception ex)
            {
                getRegisterLoginResp.status = 500;
                log2File.Error($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}\r\n{ex}", logName, ex);
                return getRegisterLoginResp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(registerLoginReq)}\r\n{JsonConvert.SerializeObject(getRegisterLoginResp)}", logName);
            return getRegisterLoginResp;
        }

        [HttpPost]
        public GetBalanceResp syncCredit(SyncCreditData_EBet syncCreditReq)
        {
            string logName = $"{thisTp}_Sync_Credit";

            GetBalanceResp getBalanceResp = new GetBalanceResp();

            // check user (exist status online)
            var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, syncCreditReq.username);
            if (tpCheck == null || tpCheck.Locked)
            {
                getBalanceResp.status = 4037;
                log2File.Error($"{JsonConvert.SerializeObject(syncCreditReq)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);
                return getBalanceResp;
            }

            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, syncCreditReq.username, gameType);
            getBalanceResp.status = 200;
            getBalanceResp.username = syncCreditReq.username;
            getBalanceResp.money = tpCustBalance.Balance;
            getBalanceResp.currency = tpCustBalance.TpCurrencyId;
            getBalanceResp.Event = syncCreditReq.Event;
            getBalanceResp.seqNo = syncCreditReq.seqNo;
            getBalanceResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            log2File.Info($"{JsonConvert.SerializeObject(syncCreditReq)}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", logName);

            return getBalanceResp;
        }

        [HttpPost]
        public async Task<GetBetResp> increaseCreditAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            string logName = $"{thisTp}_increaseCredit";
            log2File.Info(Content, logName);

            BettingData_EBet getBetReq = JsonConvert.DeserializeObject<BettingData_EBet>(Content);
            GetBetResp getBetResp = new GetBetResp();
            getBetResp.status = 500;
            getBetResp.money = 0;
            getBetResp.seqNo = getBetReq.seqNo;
            getBetResp.username = getBetReq.username;
            getBetResp.Event = getBetReq.Event;
            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // check signature
            if (string.IsNullOrEmpty(getBetReq.signature))
            {
                getBetResp.status = 4026;
                getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            // verify signature
            string sign = $"{getBetReq.seqNo}{getBetReq.Event}{getBetReq.channelId}{getBetReq.timestamp}{getBetReq.username}{getBetReq.money}";
            string privateKey = _siteEBetConfig.Currency.FirstOrDefault(x => x.CurrencyId == getBetReq.currency).PrivateKey;
            var check = VerifySignature(sign, getBetReq.signature, privateKey);
            if (!check)
            {
                getBetResp.status = 4026;
                getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                return getBetResp;
            }

            var result = 0;
            try
            {
                switch (getBetReq.type)
                {
                    case 1:
                    case 27:
                        // 1: bet、27: pre-bet
                        logName = $"{logName}_Bet";
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBetReq.username);
                        if (tpCheck == null || tpCheck.Locked)
                        {
                            getBetResp.status = 4037;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }

                        if (tpCheck.Token != HttpUtility.UrlDecode(getBetReq.sessionToken))
                        {
                            getBetResp.status = 410;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        /*
                         * type 11測試: 該情境 type 27成功 並且 type 1是失敗的
                        if (getBetReq.type == 1)
                        {
                            getBetResp.status = 500;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"refund test\r\n{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        */
                        result = bettingBLL.PlaceBetLC_EBet(getBetReq, out decimal tpAffterAmount);
                        if (result > 0)
                        {
                            getBetResp.status = 200;
                            getBetResp.money = tpAffterAmount;
                            getBetResp.moneyBefore = tpAffterAmount - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetReq.username);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (tpAffterAmount < 0)
                        {
                            getBetResp.status = 500;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);

                        break;
                    case 2:
                        // 2: payout
                        logName = $"{logName}_Payout";
                        result = settledBLL.PayoutLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 201;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);

                        break;
                    case 28:
                        // 28: pre-refund
                        logName = $"{logName}_PreRefund";
                        result = settledBLL.PreRefundLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 201;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);

                        break;
                    case 11:
                        // cancel bet
                        logName = $"{logName}_Cancel_Bet";
                        result = settledBLL.CancelBetLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {

                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);

                        break;
                    case 38:
                        // 宝箱奖金
                        logName = $"{logName}_Reward";
                        result = settledBLL.RewardLC_EBet(getBetReq, out tpAffterAmount);
                        if (result > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 200;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 201;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.moneyBefore = tpCustBalance.Balance - getBetReq.money;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBetResp.username, gameType);
                            getBetResp.status = 500;
                            getBetResp.money = tpCustBalance.Balance;
                            getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                            log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);
                            return getBetResp;
                        }
                        log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}", logName);

                        break;
                    default:
                        break;
                }

                return getBetResp;
            }
            catch (Exception ex)
            {
                getBetResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getBetResp)}\r\n{ex}", $"EBet_increaseCredit", ex);
                return getBetResp;
            }
        }

        [HttpPost]
        public async Task<GetCreditRecordResp> queryIncreaseCreditRecordAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            string logName = $"{thisTp}_queryIncreaseCreditRecord";
            log2File.Info(Content, logName);

            CreditRecordData_EBet getCreditRecordReq = JsonConvert.DeserializeObject<CreditRecordData_EBet>(Content);
            GetCreditRecordResp getCreditRecordResp = new GetCreditRecordResp();
            getCreditRecordResp.status = 200;
            getCreditRecordResp.seqNo = getCreditRecordReq.seqNo;
            getCreditRecordResp.Event = getCreditRecordReq.Event;
            getCreditRecordResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            getCreditRecordResp.username = getCreditRecordReq.username;

            var data = settledBLL.GetCreditRecordLC_EBet(getCreditRecordReq.username, getCreditRecordReq.querySeqNo);
            if (data == null || data.Count() == 0)
            {
                // seqNo 不存在
                getCreditRecordResp.status = 206;
            }
            getCreditRecordResp.creditRecord = data;

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getCreditRecordResp)}", logName);
            return getCreditRecordResp;
        }

        [HttpPost]
        public async Task<GetRefundSingleWalletResp> refundSingleWalletAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();

            string logName = $"{thisTp}_refundSingleWallet";
            log2File.Info(Content, logName);

            RefundSingleWallet_EBet getRefundBetReq = JsonConvert.DeserializeObject<RefundSingleWallet_EBet>(Content);
            GetRefundSingleWalletResp getRefundSingleWalletResp = new GetRefundSingleWalletResp();
            getRefundSingleWalletResp.status = 500;
            getRefundSingleWalletResp.seqNo = getRefundBetReq.seqNo;
            getRefundSingleWalletResp.Event = getRefundBetReq.Event;
            try
            {
                getRefundSingleWalletResp.refundList = new List<RefundList>();
                foreach (var item in getRefundBetReq.refundList)
                {
                    var result = settledBLL.RefundLC_EBet(item, out decimal tpAffterAmount);
                    RefundList refRefundList = new RefundList();
                    if (result > 0)
                    {
                        // 200: 渠道已收到eBET请求并已对其进行处理
                        var refundMoney = Math.Abs(item.refundMoney);
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, item.username, gameType);
                        getRefundSingleWalletResp.status = 200;
                        getRefundSingleWalletResp.moneyBefore = tpCustBalance.Balance - refundMoney;
                        getRefundSingleWalletResp.moneyAfter = tpCustBalance.Balance + refundMoney;
                        getRefundSingleWalletResp.refundMoney = refundMoney;
                        getRefundSingleWalletResp.refundList.Add(refRefundList);
                        refRefundList.refundSeqNo = item.refundSeqNo;
                        refRefundList.status = 200;
                    }
                    else if (result == -2)
                    {
                        // 208 记录不存在
                        getRefundSingleWalletResp.status = 200;
                        getRefundSingleWalletResp.refundList.Add(refRefundList);
                        refRefundList.refundSeqNo = item.refundSeqNo;
                        refRefundList.status = 208;
                    }
                    else if (result == -3)
                    {
                        // 209: 渠道已自行退款
                        getRefundSingleWalletResp.status = 200;
                        getRefundSingleWalletResp.refundList.Add(refRefundList);
                        refRefundList.refundSeqNo = item.refundSeqNo;
                        refRefundList.status = 209;
                    }
                    else
                    {
                        getRefundSingleWalletResp.status = 500;
                        getRefundSingleWalletResp.refundList.Add(refRefundList);
                        refRefundList.refundSeqNo = item.refundSeqNo;
                        refRefundList.status = 500;
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getRefundSingleWalletResp)}", logName);
                        return getRefundSingleWalletResp;
                    }
                }
                getRefundSingleWalletResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getRefundSingleWalletResp)}", logName);
                return getRefundSingleWalletResp;
            }
            catch (Exception ex)
            {
                getRefundSingleWalletResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getRefundSingleWalletResp)}\r\n{ex}", logName, ex);
                return getRefundSingleWalletResp;
            }
        }

        [HttpPost]
        public async Task<GetAutoBatchRefundResp> autoBatchRefundAsync()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            string logName = $"{thisTp}_autoBatchRefund";
            log2File.Info(Content, logName);

            AutoBatchRefund_EBet getAutoBatchRefundReq = JsonConvert.DeserializeObject<AutoBatchRefund_EBet>(Content);
            GetAutoBatchRefundResp getAutoBatchRefundResp = new GetAutoBatchRefundResp();
            getAutoBatchRefundResp.status = 500;
            getAutoBatchRefundResp.seqNo = getAutoBatchRefundReq.seqNo;
            getAutoBatchRefundResp.Event = getAutoBatchRefundReq.Event;
            try
            {
                Dictionary<string, RefundResultList> keyValuePairs = new Dictionary<string, RefundResultList>();
                foreach (var item in getAutoBatchRefundReq.batchRefundList)
                {
                    var refundMoney = item.refundMoney;
                    var result = settledBLL.RefundBatchLC_EBet(item, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        RefundResultList refBatchRefundList;
                        if (keyValuePairs.ContainsKey(item.roundCode))
                        {
                            refBatchRefundList = keyValuePairs[item.roundCode];
                            refBatchRefundList.refundTotalMoney += refundMoney;
                        }
                        else
                        {
                            refBatchRefundList = new RefundResultList();
                            keyValuePairs.Add(item.roundCode, refBatchRefundList);
                            refBatchRefundList.username = item.username;
                            refBatchRefundList.roundCode = item.roundCode;
                            refBatchRefundList.refundTotalMoney = refundMoney;
                            refBatchRefundList.sucRefundSeqNoList = new List<SucRefundSeqNoList>();
                        }

                        SucRefundSeqNoList sucRefundSeqNoList = new SucRefundSeqNoList();
                        refBatchRefundList.sucRefundSeqNoList.Add(sucRefundSeqNoList);
                        sucRefundSeqNoList.seqNo = item.seqNo;
                        sucRefundSeqNoList.status = 200;
                    }
                }
                getAutoBatchRefundResp.refundResultList = keyValuePairs.Values.ToList();
                getAutoBatchRefundResp.status = 200;
                getAutoBatchRefundResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getAutoBatchRefundResp)}", logName);
                return getAutoBatchRefundResp;
            }
            catch (Exception ex)
            {
                getAutoBatchRefundResp.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(getAutoBatchRefundResp)}\r\n{ex}", logName, ex);
                return getAutoBatchRefundResp;
            }
        }

        private static bool VerifySignature(string dataToSign, string signedData, string publicKey)
        {
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
