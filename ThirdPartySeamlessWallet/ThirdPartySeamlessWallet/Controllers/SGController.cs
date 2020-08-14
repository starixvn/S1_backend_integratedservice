using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class SGController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tp thisTp;
        private readonly AuthBLL authBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly IHostEnvironment environment;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly int gameType = 0;

        public SGController(IHostEnvironment environment, Log2File log2File, ThirdPartyBLL thirdPartyBLL, AuthBLL authBLL, TpCustInfoBLL tpCustInfoBLL, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.log2File = log2File;
            this.thirdPartyBLL = thirdPartyBLL;
            thisTp = Tp.SG;
            this.authBLL = authBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.environment = environment;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            gameType = (int)GameType.SG_RNG;
        }
        public class Resp
        {
            public string merchantCode { get; set; }
            public string msg { get; set; }
            public int code { get; set; }
            public string serialNo { get; set; }
        }
        public class AcctInfo
        {
            public string acctId { get; set; }
            public decimal balance { get; set; }
            public string userName { get; set; }
            public string currency { get; set; }
            //public string siteId { get; set; }
        }

        public class AuthorizeResp : Resp
        {
            public AcctInfo acctInfo { get; set; }
        }
        public class AuthorizeReq
        {
            public string acctId { get; set; }
            public string language { get; set; }
            public string merchantCode { get; set; }
            public string token { get; set; }
            public string serialNo { get; set; }
        }

        [Route("")]
        [HttpPost]
        public async Task<Resp> IndexAsync()
        {
            Resp resp = new Resp();
            Request.Headers.TryGetValue("API", out StringValues vs);
            var api = vs.FirstOrDefault();
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            if (api.Equals("authorize", StringComparison.OrdinalIgnoreCase))
            {
                AuthorizeResp authorizeResp = new AuthorizeResp
                {
                    code = 1,
                    msg = "System Error"
                };
                var authorizeReq = JsonConvert.DeserializeObject<AuthorizeReq>(Content);
                try
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(authorizeReq.token));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    string authToken = sessionKeyArr[0]; /* 1 Time*/
                    string sessionToken = sessionKeyArr[1];/* Long Time*/
                    if (authBLL.Authenticate(authToken, out TpCustInfoAuth tpCustInfoAuth))
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, tpCustInfoAuth.TpUserName);
                        if (tpCheck == null)
                        {
                            authorizeResp.code = 50104;
                            authorizeResp.msg = "Token Validation Failed.";
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            authorizeResp.code = 50104;
                            authorizeResp.msg = $"Token Validation Failed.";
                        }
                        else if (tpCheck.Locked)
                        {
                            authorizeResp.code = 50102;
                            authorizeResp.msg = "Acct Locked";
                        }
                        else
                        {
                            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                            var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, tpCustInfoAuth.TpUserName, gameType);
                            authorizeResp.acctInfo = new AcctInfo
                            {
                                acctId = tpCustInfoAuth.TpUserName,
                                currency = tpCustInfoAuth.TpCurrencyId,
                                userName = tpCustInfoAuth.TpUserName,
                                balance = tpCustBalance.Balance
                            };
                            authorizeResp.merchantCode = thirdPartySetting.TpSetting["merchantCode"];
                            authorizeResp.serialNo = authorizeReq.serialNo;
                            authorizeResp.code = 0;
                            authorizeResp.msg = "Success";
                        }
                    }
                    else
                    {
                        authorizeResp.code = 50104;
                        authorizeResp.msg = "Token Validation Failed.";
                    }
                }
                catch (Exception ex)
                {
                    authorizeResp.code = 1;
                    authorizeResp.msg = "System Error";
                    log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}\r\n{ex}", $"SG_{api}");
                }

                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(authorizeResp)}", $"SG_{api}");
                return authorizeResp;
            }
            else if (api.Equals("getBalance", StringComparison.OrdinalIgnoreCase))
            {
                GetBalanceResp getBalanceResp = new GetBalanceResp
                {
                    code = 1,
                    msg = "System Error"
                };
                var getBalanceReq = JsonConvert.DeserializeObject<GetBalanceReq>(Content);
                string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(getBalanceReq.token));
                var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                string authToken = sessionKeyArr[0]; /* 1 Time*/
                string sessionToken = sessionKeyArr[1];/* Long Time*/

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, getBalanceReq.acctId);
                if (tpCheck == null)
                {
                    getBalanceResp.code = 50104;
                    getBalanceResp.msg = "Token Validation Failed";
                }
                else if (tpCheck.Token != sessionToken)
                {
                    getBalanceResp.code = 50104;
                    getBalanceResp.msg = "Token Validation Failed";
                }
                else if (tpCheck.Locked)
                {
                    getBalanceResp.code = 50102;
                    getBalanceResp.msg = "Acct Locked";
                }
                else
                {
                    var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, getBalanceReq.acctId, gameType);
                    getBalanceResp.acctInfo = new AcctInfo
                    {
                        acctId = getBalanceReq.acctId,
                        currency = tpCustBalance.TpCurrencyId,
                        userName = getBalanceReq.acctId,
                        balance = tpCustBalance.Balance
                    };
                    getBalanceResp.serialNo = getBalanceReq.serialNo;
                    getBalanceResp.merchantCode = thirdPartySetting.TpSetting["merchantCode"];

                    getBalanceResp.code = 0;
                    getBalanceResp.msg = "Success";
                }

                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(getBalanceResp)}", $"SG_{api}");
                return getBalanceResp;
            }
            else if (api.Equals("transfer", StringComparison.OrdinalIgnoreCase))
            {
                TransferResp transferResp = new TransferResp
                {
                    code = 1,
                    msg = "System Error"
                };

                if (environment.IsDevelopment())
                {
                    //Content = @"{""acctId"":""DNOVA88_156"",""transferId"":""20200416011215846126898"",""currency"":""THB"",""amount"":10.000000,""type"":1,""ticketId"":null,""channel"":""Web"",""playerIp"":""218.211.34.185"",""token"":""WyJKbUZrQU41UnlrNmg0NG91OFpJTlEiLCJqczR6YUZBWkFVMkV6ZTM2Wmk1MkhRIl0="",""transferTime"":""2020-04-16 01:12:15.846"",""gameCode"":""S-BM01"",""merchantCode"":""NVC"",""serialNo"":""2f651454-73e7-475e-8ffe-d85e09f3bff5"",""referenceId"":null,""specialGame"":null,""refTicketIds"":null}";
                    //{"serialNo":"ad3c00b7-7902-4ac6-9ca9-5a2995319759","merchantCode":"NVC","transferId":"20200416125847382126900","acctId":"DNOVA88_156","currency":"THB","amount":10.000000,"type":1,"gameCode":"S-BM01","channel":"Web","playerIp":"218.211.34.185","token":"WyI0UGtadXRxd1ZrZXNHMmJ4RERGRXciLCJqczR6YUZBWkFVMkV6ZTM2Wmk1MkhRIl0=","transferTime":"2020-04-16 12:58:47.382"}
                }
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
                var transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
                transferResp.acctId = transferReq.acctId;
                transferResp.transferId = transferReq.transferId;
                transferResp.merchantTxId = transferReq.transferId;
                transferResp.serialNo = transferReq.serialNo;
                transferResp.merchantCode = thirdPartySetting.TpSetting["merchantCode"];

                if (transferReq.type == TransferType.PlaceBet)
                {
                    string sessionKey = Encoding.ASCII.GetString(Convert.FromBase64String(transferReq.token));
                    var sessionKeyArr = JsonConvert.DeserializeObject<string[]>(sessionKey);
                    string authToken = sessionKeyArr[0]; /* 1 Time*/
                    string sessionToken = sessionKeyArr[1];/* Long Time*/
                    try
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferReq.acctId);
                        if (tpCheck == null)
                        {
                            transferResp.code = 50104;
                            transferResp.msg = "Token Validation Failed";
                            return transferResp;
                        }
                        else if (tpCheck.Token != sessionToken)
                        {
                            transferResp.code = 50104;
                            transferResp.msg = "Token Validation Failed";
                            return transferResp;
                        }
                        else if (tpCheck.Locked)
                        {
                            transferResp.code = 50102;
                            transferResp.msg = "Acct Locked";

                            return transferResp;
                        }

                        if (bettingBLL.PlaceBetRNG_SG(transferReq, out decimal tpAffterAmount) > 0)
                        {
                            if (transferReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                                transferResp.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                transferResp.balance = tpAffterAmount;
                            }
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 50110;
                            transferResp.msg = "Insufficient Balance";
                        }
                        else/*Can't Insert Bet*/
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 1;
                            transferResp.msg = "System Error";
                        }
                    }
                    catch(Exception ex)
                    {
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", $"SG_{api}_{transferReq.type}");
                        throw ex;
                    }
                }
                else if (transferReq.type == TransferType.CancelBet)
                {
                    try
                    {
                        if (settledBLL.CancelBetRNG_SG(transferReq, out decimal tpAffterAmount) > 0)
                        {
                            if (transferReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                                transferResp.balance = tpCustBalance.Balance;
                            }
                            else
                            {
                                transferResp.balance = tpAffterAmount;
                            }
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                        else
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                    }
                    catch (Exception ex)
                    {
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", $"SG_{api}_{transferReq.type}");
                        throw ex;
                    }
                }
                else if (transferReq.type == TransferType.Payout)
                {
                    try
                    {
                        int result = settledBLL.SettledRNG_SG(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.amount > 0)
                            {
                                transferResp.balance = tpAffterAmount;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                            else if (transferReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                                transferResp.balance = tpCustBalance.Balance;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                    }
                    catch (Exception ex)
                    {
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", $"SG_{api}_{transferReq.type}");
                        throw ex;
                    }
                }
                else if (transferReq.type == TransferType.Bonus)
                {
                    try
                    {
                        int result = settledBLL.SettledRNG_SG(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {

                            if (transferReq.amount > 0)
                            {
                                transferResp.balance = tpAffterAmount;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                            else if (transferReq.amount == 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                                transferResp.balance = tpCustBalance.Balance;
                                transferResp.code = 0;
                                transferResp.msg = "Success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.acctId, gameType);
                            transferResp.balance = tpCustBalance.Balance;
                            transferResp.code = 0;
                            transferResp.msg = "Success";
                        }
                    }
                    catch (Exception ex)
                    {
                        log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}\r\n{ex.Message}", $"SG_{api}_{transferReq.type}");
                        throw ex;
                    }
                }

                log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(transferResp)}", $"SG_{api}_{transferReq.type}");
                return transferResp;
            }

            return resp;
        }

        public class GetBalanceResp : Resp
        {
            public AcctInfo acctInfo { get; set; }
        }
        public class GetBalanceReq
        {
            public string acctId { get; set; }
            public string merchantCode { get; set; }
            public string serialNo { get; set; }
            public string gameCode { get; set; }
            public string token { get; set; }
        }


        public class TransferReq : BettingDataRNG_SG
        {

        }

        public class TransferResp : Resp
        {
            public string transferId { get; set; }
            public string merchantTxId { get; set; }
            public string acctId { get; set; }
            public decimal balance { get; set; }
        }
    }
}