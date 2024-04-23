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
    public class PGController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;
        private readonly int gameType = 0;
        private readonly PGSiteConfig _config;

        public PGController(IServiceProvider provider)
        {
            log2File = provider.GetRequiredService<Log2File>();
            thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            authBLL = provider.GetRequiredService<AuthBLL>();
            tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            bettingBLL = provider.GetRequiredService<BettingBLL>();
            settledBLL = provider.GetRequiredService<SettledBLL>();
            environment = provider.GetRequiredService<IHostEnvironment>();
            thisTp = Tp.PG;
            gameType = (int)GameType.PG_RNG;
            _config = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<PGSiteConfig>(thisTp.ToString()).FirstOrDefault().Value;
        }

        private class PGSiteConfig : TpSiteConfig
        {
            public string GameUrl_CNY { get; set; }
            public string GameUrl_THB { get; set; }
            public string secret_key { get; set; }
            public string BetDetailUrl { get; set; }
            public string operator_token { get; set; }
        }


        public class Error
        {
            public string code { get; set; }
            public string message { get; set; }
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
            public Error error { get; set; }
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
        public VerifySessionResp VerifySession(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_VerifySession";
            VerifySessionResp resp = new VerifySessionResp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var verifySessionReq = JsonConvert.DeserializeObject<VerifySessionReq>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(verifySessionReq.OperatorToken) || string.IsNullOrEmpty(verifySessionReq.SecretKey) || string.IsNullOrEmpty(verifySessionReq.OperatorPlayerSession) || !verifySessionReq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !verifySessionReq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            if (!string.IsNullOrEmpty(verifySessionReq.OperatorPlayerSession))
            {
                try
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
                            resp.error = new Error
                            {
                                code = "1308",
                                message = "Player session is expired"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            resp.error = new Error
                            {
                                code = "1308",
                                message = "Player session is expired"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error = new Error
                            {
                                code = "1309",
                                message = "Player is inactive"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else
                        {
                            VerifySessionRespData respData = new VerifySessionRespData();
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
                        resp.error = new Error
                        {
                            code = "1300",
                            message = "Invalid player session"
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (FormatException fEx)
                {
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{fEx}", logName, fEx);
                }
            }
            else
            {
                resp.error = new Error
                {
                    code = "1301",
                    message = "Player session token is empty"
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
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
            public Error error { get; set; }
        }

        public class BalanceData
        {
            public string currency_code { get; set; }
            public decimal balance_amount { get; set; }
            public long updated_time { get; set; }
        }

        [Route("[controller]/GetUserBalance")]
        [HttpPost]
        public Resp GetBalance(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_GetBalance";
            Resp resp = new Resp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (!getBalanceReq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !getBalanceReq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            if (!string.IsNullOrEmpty(getBalanceReq.OperatorPlayerSession))
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.PlayerName);
                if (tpCheck == null)
                {
                    resp.error = new Error
                    {
                        code = "1308",
                        message = "Invalid request."
                    };
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = new Error
                    {
                        code = "1309",
                        message = "Player is inactive"
                    };
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
                else
                {
                    TpCustBalance tpCustBalance = null;
                    if (environment.IsDevelopment())
                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, getBalanceReq.PlayerName, gameType);
                    else
                        tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.PlayerName, gameType);

                    BalanceData respData = new BalanceData();
                    resp.data = respData;

                    respData.currency_code = tpCustBalance.TpCurrencyId;
                    respData.balance_amount = tpCustBalance.Balance;
                    respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    resp.error = null;
                }
            }
            else
            {
                resp.error = new Error
                {
                    code = "1300",
                    message = "Invalid player session"
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);

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

            [JsonProperty("is_adjustment")]
            public bool IsAdjustment { get; set; }
            [JsonProperty("is_validate_bet")]
            public bool IsValidateBet { get; set; }

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
        public Resp TransferOut(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_TransferOut";
            Resp resp = new Resp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var transferOutReqq = JsonConvert.DeserializeObject<TransferOutReqq>(JsonConvert.SerializeObject(reqq));

            //transferOutReqq = JsonConvert.DeserializeObject<TransferOutReqq>(@"{""operator_token"":""5884cec7905af23c1586a6819ae0b1e1"",""secret_key"":""645b1f5db5c7c6e3d0fb087f11f62e50"",""operator_player_session"":""WyJldW9WN1BqNDFVQ2dTaUhnSzVmQWxRIiwiS2VSbDRhbm5qVXl0eTdFSlBZRmZ3Il0%3D"",""game_id"":""1"",""parent_bet_id"":""6669305"",""bet_id"":""6669305"",""player_name"":""DNova88_156"",""currency_code"":""THB"",""transfer_amount"":""30.00"",""wallet_type"":""C"",""bet_type"":""1"",""transaction_id"":""6669305-6669305-201-0"",""updated_time"":""1592192524927"",""is_feature"":""False"",""is_minus_count"":""False"",""is_wager"":""True"",""platform"":""1"",""create_time"":""1592192524927"",""is_parent_zero_stake"":""False"",""jackpot_rtp_contribution_amount"":""0"",""jackpot_win_amount"":""0""}");

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(transferOutReqq.PlayerName) || !transferOutReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !transferOutReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase) || transferOutReqq.TransferAmount < 0)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferOutReqq.PlayerName);
            if (tpCustInfo == null)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            TpCustBalance tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferOutReqq.PlayerName, gameType);

            if (tpCustBalance == null || !tpCustBalance.TpCurrencyId.Equals(transferOutReqq.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            if (!string.IsNullOrEmpty(transferOutReqq.OperatorPlayerSession) || transferOutReqq.IsAdjustment || transferOutReqq.IsValidateBet)
            {
                try
                {
                    if (!transferOutReqq.IsAdjustment && !transferOutReqq.IsValidateBet)
                    {
                        string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(transferOutReqq.OperatorPlayerSession)));
                        var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                        string authToken = sessionKeyArr[0]; /* 1 Time*/
                        string sessionToken = sessionKeyArr[1];/* Long Time*/

                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferOutReqq.PlayerName);
                        if (tpCheck == null)
                        {
                            resp.error = new Error
                            {
                                code = "1308",
                                message = "Player session is expired"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            resp.error = new Error
                            {
                                code = "1308",
                                message = "Player session is expired"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error = new Error
                            {
                                code = "1309",
                                message = "Player is inactive"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                    }

                    transferOutReqq.type = TransferType.PlaceBet;
                    transferOutReqq.TraceId = trace_id;
                    //transferOutReqq.BetTransactionId = transferOutReqq.BetTransactionId ?? transferOutReqq.TransactionId;
                    BalanceData respData = new BalanceData();
                    resp.data = respData;
                    respData.updated_time = transferOutReqq.UpdatedTime;
                    int result = bettingBLL.PlaceBetRNG_PG(transferOutReqq, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        if (transferOutReqq.TransferAmount == 0)
                        {
                            tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferOutReqq.PlayerName, gameType);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpCustBalance.Balance;
                            resp.error = null;
                        }
                        else
                        {
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpAffterAmount;
                            resp.error = null;
                        }
                    }
                    else if (result == -1)
                    {
                        if (environment.IsDevelopment())
                            tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferOutReqq.PlayerName, gameType);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = null;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.data = null;
                        resp.error = new Error
                        {
                            code = "3202",
                            message = "No enough cash balance to bet."
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else/*Can't Insert Bet*/
                    {
                        resp.data = null;
                        resp.error = new Error
                        {
                            code = "1034",
                            message = $"Invalid request."
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(transferOutReqq)}\r\n{ex}", $"{thisTp}_TransferOut", ex);
                    throw ex;
                }
            }
            else
            {
                resp.error = new Error
                {
                    code = "1300",
                    message = "Invalid player session"
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
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
            [JsonProperty("is_adjustment")]
            public bool IsAdjustment { get; set; }

            [JsonProperty("is_validate_bet")]
            public bool IsValidateBet { get; set; }
        }

        [Route("[controller]/Payout")]
        [HttpPost]
        public Resp TransferIn(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_TransferIn";
            Resp resp = new Resp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var transferInReqq = JsonConvert.DeserializeObject<TransferInReqq>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(transferInReqq.PlayerName) || !transferInReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !transferInReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase) || transferInReqq.TransferAmount < 0)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferInReqq.PlayerName);
            if (tpCustInfo == null)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            TpCustBalance tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferInReqq.PlayerName, gameType);
            if (tpCustBalance == null || !tpCustBalance.TpCurrencyId.Equals(transferInReqq.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            BalanceData respData = new BalanceData();
            resp.data = respData;
            respData.updated_time = transferInReqq.UpdatedTime;
            resp.error = null;

            try
            {
                transferInReqq.type = GetType(transferInReqq.TransactionId);
                transferInReqq.TraceId = trace_id;
                int result = settledBLL.SettledRNG_PG(transferInReqq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    if (transferInReqq.TransferAmount > 0)
                    {
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpAffterAmount;
                        resp.error = null;
                    }
                    else if (transferInReqq.TransferAmount == 0)
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInReqq.PlayerName, gameType);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = null;
                    }
                }
                else if (result == -1)
                {
                    tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInReqq.PlayerName, gameType);
                    respData.currency_code = tpCustBalance.TpCurrencyId;
                    respData.balance_amount = tpCustBalance.Balance;
                    resp.error = null;
                }
                else if (tpAffterAmount < 0)
                {
                    resp.data = null;
                    resp.error = new Error
                    {
                        code = "3202",
                        message = "No enough cash balance."
                    };
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(transferInReqq)}\r\n{ex}", $"{thisTp}_TransferIn", ex);
                throw ex;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);

            return resp;
        }

        public class TransferInOutReqq : TransferInOutDataRNG_PG
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }
        }

        [Route("[controller]/TransferInOut")]
        [HttpPost]
        public Resp TransferInOut(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_TransferInOut";
            Resp resp = new Resp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var transferInOutReqq = JsonConvert.DeserializeObject<TransferInOutReqq>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(transferInOutReqq.PlayerName) || !transferInOutReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !transferInOutReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferInOutReqq.PlayerName);
            if (tpCustInfo == null)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            TpCustBalance tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, transferInOutReqq.PlayerName, gameType);
            if (tpCustBalance == null || !tpCustBalance.TpCurrencyId.Equals(transferInOutReqq.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            if (!string.IsNullOrEmpty(transferInOutReqq.OperatorPlayerSession) || transferInOutReqq.IsAdjustment || transferInOutReqq.IsValidateBet)
            {
                try
                {
                    if (!transferInOutReqq.IsAdjustment && !transferInOutReqq.IsValidateBet)
                    {
                        string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(HttpUtility.UrlDecode(transferInOutReqq.OperatorPlayerSession)));
                        var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                        string authToken = sessionKeyArr[0]; /* 1 Time*/
                        string sessionToken = sessionKeyArr[1];/* Long Time*/

                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferInOutReqq.PlayerName);
                        if (tpCheck == null || tpCheck.Token != sessionToken)
                        {
                            resp.error = new Error
                            {
                                code = "1308",
                                message = "Player session is expired"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error = new Error
                            {
                                code = "1309",
                                message = "Player is inactive"
                            };
                            log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                            return resp;
                        }
                    }
                    /*
                     * 不會直接使用TransferAmount進行金額異動，因為我們其他遊戲曾碰過會員會用此方式套利
                     * 若貴司不直接使用TransferAmount進行金額異動，可以在win_amount  - bet_amount != TransferAmount返回錯誤訊息
                     * 3073 BetFailedException
                     */
                    if ((transferInOutReqq.WinAmount - transferInOutReqq.BetAmount) != transferInOutReqq.TransferAmount)
                    {
                        resp.error = new Error
                        {
                            code = "3073",
                            message = "BetFailedException"
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }

                    BalanceData respData = new BalanceData();
                    resp.data = respData;
                    respData.updated_time = transferInOutReqq.UpdatedTime;
                    transferInOutReqq.type = GetType(transferInOutReqq.TransactionId);
                    transferInOutReqq.TraceId = trace_id;
                    int result = settledBLL.BetAndPayoutRNG_PG(transferInOutReqq, out decimal tpAffterAmount);

                    if (result > 0)
                    {
                        if (transferInOutReqq.TransferAmount == 0)
                        {
                            tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInOutReqq.PlayerName, gameType);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpCustBalance.Balance;
                            resp.error = null;
                        }
                        else
                        {
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpAffterAmount;
                            resp.error = null;
                        }
                    }
                    else if (result == -1)
                    {
                        if (environment.IsDevelopment())
                        {
                            tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferInOutReqq.PlayerName, gameType);
                        }
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = null;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        resp.data = null;
                        resp.error = new Error
                        {
                            code = "3202",
                            message = "No enough cash balance to bet."
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                    else
                    {
                        resp.data = null;
                        resp.error = new Error
                        {
                            code = "1034",
                            message = "Invalid request."
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                    throw;
                }
            }
            else
            {
                resp.error = new Error
                {
                    code = "1300",
                    message = "Invalid player session"
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            return resp;
        }

        public class AdjustmentResp
        {
            public AdjustmentBalanceData data { get; set; }
            public Error error { get; set; }
        }

        public class AdjustmentBalanceData
        {
            public decimal adjust_amount { get; set; }
            public decimal balance_before { get; set; }
            public decimal balance_after { get; set; }
            public long updated_time { get; set; }
        }

        [Route("[controller]/Adjustment")]
        [HttpPost]
        public AdjustmentResp Adjustment(string trace_id, [FromForm] Dictionary<string, string> reqq)
        {
            // api used for future certain PG event only , such as Cash Tournament.
            string logName = $"{thisTp}_Adjustment";
            AdjustmentResp resp = new AdjustmentResp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var adjustmentReqq = JsonConvert.DeserializeObject<AdjustmentDataRNG_PG>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(adjustmentReqq.PlayerName) || !adjustmentReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !adjustmentReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, adjustmentReqq.PlayerName);
            if (tpCustInfo == null)
            {
                resp.error = new Error
                {
                    code = "3004",
                    message = "Player does not exist."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            TpCustBalance tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, adjustmentReqq.PlayerName, gameType);
            if (tpCustBalance == null || !tpCustBalance.TpCurrencyId.Equals(adjustmentReqq.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "3005",
                    message = "Player wallet does not exist."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            AdjustmentBalanceData respData = new AdjustmentBalanceData();
            resp.data = respData;
            resp.error = null;

            try
            {
                var balanceBefore = tpCustBalance.Balance;
                adjustmentReqq.TraceId = trace_id;
                int result = settledBLL.RewardRNG_PG(adjustmentReqq, out decimal tpAffterAmount);

                if (result > 0)
                {
                    if (adjustmentReqq.TransferAmount != 0)
                    {
                        respData.adjust_amount = adjustmentReqq.TransferAmount;
                        respData.balance_before = balanceBefore;
                        respData.balance_after = tpAffterAmount;
                        respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        resp.error = null;
                    }
                    else if (adjustmentReqq.TransferAmount == 0)
                    {

                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, adjustmentReqq.PlayerName, gameType);
                        respData.adjust_amount = adjustmentReqq.TransferAmount;
                        respData.balance_before = balanceBefore;
                        respData.balance_after = tpCustBalance.Balance;
                        respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                        resp.error = null;
                    }
                }
                else if (result == -1)
                {
                    tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, adjustmentReqq.PlayerName, gameType);
                    respData.adjust_amount = adjustmentReqq.TransferAmount;
                    respData.balance_before = balanceBefore;
                    respData.balance_after = tpCustBalance.Balance;
                    respData.updated_time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    resp.error = null;
                }
                else if (tpAffterAmount < 0)
                {
                    resp.data = null;
                    resp.error = new Error
                    {
                        code = "3202",
                        message = "No enough cash balance."
                    };
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(adjustmentReqq)}\r\n{ex}", logName, ex);
                throw ex;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);

            return resp;
        }

        public class ResettleReqq : ResettleDataRNG_PG
        {
            [JsonProperty("operator_token")]
            public string OperatorToken { get; set; }

            [JsonProperty("secret_key")]
            public string SecretKey { get; set; }

            [JsonProperty("operator_player_session")]
            public string OperatorPlayerSession { get; set; }
        }

        [Route("[controller]/Resettle")]
        [HttpPost]
        public Resp Resettle([FromForm] Dictionary<string, string> reqq)
        {
            string logName = $"{thisTp}_Resettle";
            Resp resp = new Resp();
            resp.error = new Error
            {
                code = "1034",
                message = "Invalid request."
            };

            var resettleReqq = JsonConvert.DeserializeObject<ResettleReqq>(JsonConvert.SerializeObject(reqq));

            var secret_key = _config.secret_key;
            var operator_token = _config.operator_token;

            if (string.IsNullOrEmpty(resettleReqq.PlayerName) || !resettleReqq.OperatorToken.Equals(operator_token, StringComparison.OrdinalIgnoreCase) || !resettleReqq.SecretKey.Equals(secret_key, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, resettleReqq.PlayerName);
            if (tpCustInfo == null)
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            TpCustBalance tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, resettleReqq.PlayerName, gameType);
            if (tpCustBalance == null || !tpCustBalance.TpCurrencyId.Equals(resettleReqq.CurrencyCode, StringComparison.OrdinalIgnoreCase))
            {
                resp.error = new Error
                {
                    code = "1034",
                    message = "Invalid request."
                };
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                return resp;
            }

            // 目前以bettrans table資料處理，外部傳進來的金額IDR & VND 是有已有轉換過匯率的
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.PG, tpCustInfo.CurrencyId);
            if (tpCurrency.ExchangeRate != 0)
            {
                resettleReqq.BetAmount /= tpCurrency.ExchangeRate;
                resettleReqq.WinAmount /= tpCurrency.ExchangeRate;
                resettleReqq.TransferAmount /= tpCurrency.ExchangeRate;
            }

            try
            {
                BalanceData respData = new BalanceData();
                resp.data = respData;
                respData.updated_time = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
                if (resettleReqq.type == TransferType.PlaceBet || resettleReqq.type == TransferType.Payout)
                {
                    int result = settledBLL.ResettleRNG_PG(resettleReqq, out decimal tpAffterAmount);
                    if (result > 0)
                    {
                        if (resettleReqq.TransferAmount == 0)
                        {
                            tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, resettleReqq.PlayerName, gameType);
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpCustBalance.Balance;
                            resp.error = null;
                        }
                        else
                        {
                            respData.currency_code = tpCustBalance.TpCurrencyId;
                            respData.balance_amount = tpAffterAmount;
                            resp.error = null;
                        }
                    }
                    else if (result == -1)
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, resettleReqq.PlayerName, gameType);
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpCustBalance.Balance;
                        resp.error = null;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        respData.currency_code = tpCustBalance.TpCurrencyId;
                        respData.balance_amount = tpAffterAmount;
                        resp.error = null;
                        log2File.Error($"Insufficient {resettleReqq.PlayerName} {tpAffterAmount}", logName);
                    }
                    else
                    {
                        resp.data = null;
                        resp.error = new Error
                        {
                            code = "1034",
                            message = $"Invalid request.({result})"
                        };
                        log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                        return resp;
                    }
                }
                else
                {
                    resp.data = null;
                    resp.error = new Error
                    {
                        code = "1034",
                        message = "Invalid request."
                    };
                    log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
                    return resp;
                }
            }
            catch (Exception ex)
            {
                log2File.Error($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
                throw;
            }

            log2File.Info($"{JsonConvert.SerializeObject(reqq)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
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
                //400: BonusToCash
                return TransferType.Bonus;
            }
            else if (transactionType == "403")
            {
                //403: FreeGameToCash
                return TransferType.PlaceBet;
            }
            else if (transactionType == "106")
            {
                //106: BetPayout
                return TransferType.Payout;
            }
            else
            {
                return TransferType.PlaceBet;
            }
        }
    }
}
