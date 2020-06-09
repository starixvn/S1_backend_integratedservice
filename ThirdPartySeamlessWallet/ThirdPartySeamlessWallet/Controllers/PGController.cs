using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
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
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;

        public PGController(Log2File log2File, ThirdPartyBLL thirdPartyBLL, AuthBLL authBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.log2File = log2File;
            thisTp = Tp.PG;
            this.thirdPartyBLL = thirdPartyBLL;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
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

        public partial class VerifySessionResp
        {
            public VerifySessionRespData data { get; set; }
            public int? error { get; set; }
        }

        public class VerifySessionRespData
        {
            public string player_name { get; set; }
            public string nickname { get; set; }
            public string currency { get; set; }
            public string reminder_time { get; set; }
        }

        [Route("[controller]/Auth")]
        [HttpPost]
        public VerifySessionResp VerifySession(string traceId, [FromForm]Dictionary<string, string> reqq)
        {
            VerifySessionResp resp = new VerifySessionResp();
            resp.error = 1034;

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}", $"{thisTp}_VerifySession_1");

            var verifySessionReq = JsonConvert.DeserializeObject<VerifySessionReq>(JsonConvert.SerializeObject(reqq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string secret_key = thirdPartySetting.TpSetting["secret_key"];
            string operator_token = thirdPartySetting.TpSetting["operator_token"];

            if (!verifySessionReq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !verifySessionReq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = 1034;
                return resp;
            }

            if (!string.IsNullOrEmpty(verifySessionReq.OperatorPlayerSession))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(verifySessionReq.OperatorPlayerSession)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                {
                    var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                    if (tpCheck == null)
                    {
                        resp.error = 1308;
                    }
                    else if (tpCheck.Token != sessionToken)
                    {
                        resp.error = 1308;
                    }
                    else if (tpCheck.Locked)
                    {
                        resp.error = 1309;
                    }
                    else
                    {
                        VerifySessionRespData respData = new VerifySessionRespData(); ;
                        resp.data = respData;

                        respData.currency = tpCustInfoAuth.TpCurrencyId;
                        respData.nickname = tpCustInfoAuth.TpUserName;
                        respData.player_name = tpCustInfoAuth.TpUserName;
                        respData.reminder_time = DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString();
                        resp.error = null;
                    }
                }
                else
                {
                    resp.error = 1300;
                }
            }
            else
            {
                resp.error = 1301;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_VerifySession");

            return resp;
        }

        public class GetBalanceReq
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }

            [JsonProperty("game_id")]
            public long GameId { get; set; }

            [JsonProperty("player_name")]
            public string PlayerName { get; set; }
        }

        public class Resp
        {
            public BalanceData data { get; set; }
            public int? error { get; set; }
        }

        public class BalanceData
        {
            public string currency_code { get; set; }
            public decimal balance_amount { get; set; }
            public long updated_time { get; set; }
        }

        [Route("[controller]/GetUserBalance")]
        [HttpPost]
        public Resp GetBalance(string traceId, [FromForm]Dictionary<string, string> reqq)
        {
            Resp resp = new Resp();
            resp.error = 1034;

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}", $"{thisTp}_GetBalance_1");

            var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(JsonConvert.SerializeObject(reqq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string secret_key = thirdPartySetting.TpSetting["secret_key"];
            string operator_token = thirdPartySetting.TpSetting["operator_token"];

            if (!getBalanceReq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !getBalanceReq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = 1034;
                return resp;
            }

            if (!string.IsNullOrEmpty(getBalanceReq.OperatorPlayerSession))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(getBalanceReq.OperatorPlayerSession)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.PlayerName);
                if (tpCheck == null)
                {
                    resp.error = 1308;
                }
                else if (tpCheck.Token != sessionToken)
                {
                    resp.error = 1308;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = 1309;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.PlayerName);

                    BalanceData respData = new BalanceData(); ;
                    resp.data = respData;

                    respData.currency_code = tpCustBalance.TpCurrencyId;
                    respData.balance_amount = tpCustBalance.Balance;
                    respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    resp.error = null;
                }
            }
            else
            {
                resp.error = 1300;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_GetBalance");

            return resp;
        }

        public class TransferOutReqq : BettingDataRNG_PG
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }

            [JsonProperty("wallet_type")]
            public string WalletType { get; set; }

            [JsonProperty("bet_type")]
            public int BetType { get; set; }

            [JsonProperty("is_feature")]
            public bool IsFeature { get; set; }

            [JsonProperty("is_minus_count")]
            public bool IsMinusCount { get; set; }

            [JsonProperty("is_wager")]
            public bool IsWager { get; set; }

            [JsonProperty("platform")]
            public int Platform { get; set; }

            [JsonProperty("is_parent_zero_stake")]
            public bool IsParentZeroStake { get; set; }

            [JsonProperty("jackpot_rtp_contribution_amount")]
            public decimal JackpotRtpContributionAmount { get; set; }

            [JsonProperty("jackpot_win_amount")]
            public decimal JackpotWinAmount { get; set; }
        }

        [Route("[controller]/PlaceBet")]
        [HttpPost]
        public Resp TransferOut(string traceId, [FromForm]Dictionary<string, string> reqq)
        {
            Resp resp = new Resp();
            resp.error = 1034;

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}", $"{thisTp}_TransferOut_1");

            var transferOutReqq = JsonConvert.DeserializeObject<TransferOutReqq>(JsonConvert.SerializeObject(reqq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string secret_key = thirdPartySetting.TpSetting["secret_key"];
            string operator_token = thirdPartySetting.TpSetting["operator_token"];

            if (!transferOutReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !transferOutReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase) || transferOutReqq.TransferAmount < 0)
            {
                resp.error = 1034;
                return resp;
            }

            if (!string.IsNullOrEmpty(transferOutReqq.OperatorPlayerSession))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(transferOutReqq.OperatorPlayerSession)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/
                try
                {
                    if (transferOutReqq.IsWager)
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferOutReqq.PlayerName);
                        if (tpCheck == null)
                        {
                            resp.error = 1308;
                            return resp;
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            resp.error = 1308;
                            return resp;
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error = 1309;
                            return resp;
                        }
                    }

                    transferOutReqq.type = TransferType.PlaceBet;
                    transferOutReqq.BetTransactionId = transferOutReqq.BetTransactionId ?? transferOutReqq.TransactionId;
                    int result = bettingBLL.PlaceBetRNG_PG(transferOutReqq, out decimal tpAffterAmount);
                    BalanceData respData = new BalanceData();
                    resp.data = respData;
                    respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                    if (result > 0)
                    {
                        if (transferOutReqq.TransferAmount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferOutReqq.PlayerName);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpCustBalance.Balance;
                            resp.error = null;
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferOutReqq.PlayerName);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpAffterAmount;
                            resp.error = null;
                        }
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferOutReqq.PlayerName);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = 3202;
                    }
                    else/*Can't Insert Bet*/
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferOutReqq.PlayerName);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = 1034;
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(transferOutReqq)}\r\n{ex}", $"{thisTp}_TransferOut");
                    throw ex;
                }
            }
            else
            {
                resp.error = 1300;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_TransferOut");

            return resp;
        }

        public class TransferInReqq : BettingDataRNG_PG
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }

            [JsonProperty("wallet_type")]
            public string WalletType { get; set; }

            [JsonProperty("bet_type")]
            public int BetType { get; set; }

            [JsonProperty("is_feature")]
            public bool IsFeature { get; set; }

            [JsonProperty("is_minus_count")]
            public bool IsMinusCount { get; set; }

            [JsonProperty("is_wager")]
            public bool IsWager { get; set; }

            [JsonProperty("platform")]
            public int Platform { get; set; }

            [JsonProperty("is_parent_zero_stake")]
            public bool IsParentZeroStake { get; set; }

            [JsonProperty("jackpot_rtp_contribution_amount")]
            public decimal JackpotRtpContributionAmount { get; set; }

            [JsonProperty("jackpot_win_amount")]
            public decimal JackpotWinAmount { get; set; }

            [JsonProperty("jackpot_pool_id")]
            public long JackpotPoolId { get; set; }

            [JsonProperty("jackpot_type")]
            public long JackpotType { get; set; }

            [JsonProperty("is_validate_bet")]
            public bool IsValidateBet { get; set; }
        }

        [Route("[controller]/Payout")]
        [HttpPost]
        public Resp TransferIn(string traceId, [FromForm]Dictionary<string, string> reqq)
        {
            Resp resp = new Resp();
            resp.error = 1034;

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}", $"{thisTp}_TransferIn_1");

            var transferInReqq = JsonConvert.DeserializeObject<TransferInReqq>(JsonConvert.SerializeObject(reqq));
            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            string secret_key = thirdPartySetting.TpSetting["secret_key"];
            string operator_token = thirdPartySetting.TpSetting["operator_token"];

            if (!transferInReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !transferInReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase) || transferInReqq.TransferAmount < 0)
            {
                resp.error = 1034;
                return resp;
            }

            if (!string.IsNullOrEmpty(transferInReqq.OperatorPlayerSession))
            {
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(transferInReqq.OperatorPlayerSession)));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);

                BalanceData respData = new BalanceData();
                resp.data = respData;
                respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                resp.error = null;

                try
                {
                    transferInReqq.type = GetType(transferInReqq.TransactionId);
                    int result = settledBLL.SettledRNG_PG(transferInReqq, out decimal tpAffterAmount);

                    if (result > 0)
                    {

                        if (transferInReqq.TransferAmount > 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferInReqq.PlayerName);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpAffterAmount;
                            resp.error = null;
                        }
                        else if (transferInReqq.TransferAmount == 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInReqq.PlayerName);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpCustBalance.Balance;
                            resp.error = null;
                        }
                    }
                    else if (result == -1)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInReqq.PlayerName);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = null;
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(transferInReqq)}\r\n{ex}", $"{thisTp}_TransferIn");
                    throw ex;
                }
            }
            else
            {
                resp.error = 1300;
            }
            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_TransferIn");

            return resp;
        }

        private TransferType GetType(string TransactionId)
        {
            string transactionType = TransactionId.Split("-")[2];

            if (transactionType == "101")
            {
                return TransferType.Payout;
            }
            else if (transactionType == "201")
            {
                return TransferType.PlaceBet;
            }
            else if (transactionType == "400")
            {
                return TransferType.Bonus;
            }
            else if (transactionType == "403")
            {
                return TransferType.PlaceBet;
            }
            else
            {
                return TransferType.PlaceBet;
            }
        }
    }
}
