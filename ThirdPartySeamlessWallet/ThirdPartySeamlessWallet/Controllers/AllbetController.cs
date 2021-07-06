using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AllbetController : ControllerBase
    {
        private readonly Tools tools;
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly IHostEnvironment environment;
        private readonly int gameType = 0;
        public enum TransferType : int
        {
            Bet = 10,
            Cancel = 11,
            Settle = 20,
            ReSettle = 21
        }

        public AllbetController(IHostEnvironment environment, BettingBLL bettingBLL, ThirdPartyBLL thirdPartyBLL, SettledBLL settledBLL, TpCustInfoBLL tpCustInfoBLL, Log2File log2File, Tools tools)
        {
            this.tools = tools;
            this.log2File = log2File;
            thisTp = Tp.Allbet;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.environment = environment;
            gameType = (int)GameType.Allbet_LiveCasino;
        }
        public class Resp
        {
            public string error_code { get; set; }
            public string message { get; set; }
            public decimal balance { get; set; }
        }
        public class TransferReq : BettingDataLC_Allbet
        {

        }

        [HttpPost]
        [Route("[controller]/[action]")]
        public async Task<Resp> TransferAsync()/*[FromForm]Dictionary<string, string> transferReqq*/
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

            TransferReq transferReq = null;
            try
            {
                transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
            }
            catch (Exception ex)
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Transfer", ex);
                return resp;
            }
            TransferType transferType = TransferType.Bet;
            Enum.TryParse(transferReq.TransferType.ToString(), out transferType);

            Request.Headers.TryGetValue("Authorization", out var authorization);
            Request.Headers.TryGetValue("Accept", out var accept);
            Request.Headers.TryGetValue("Content-MD5", out var md5);
            Request.Headers.TryGetValue("Content-Type", out var cType);
            Request.Headers.TryGetValue("Date", out var dtDate);
            //var absoluteUri = $"{Request.Scheme}://{Request.Host.ToUriComponent()}{Request.PathBase.ToUriComponent()}{Request.Path.ToUriComponent()}{Request.QueryString.ToUriComponent()}";
            string propertyId = "";
            string sign = "";
            string sAuthorization = authorization.ToString();
            if (!string.IsNullOrEmpty(sAuthorization))
            {
                propertyId = sAuthorization.Substring(3, sAuthorization.IndexOf(":") - 3);
                sign = sAuthorization.Substring(sAuthorization.IndexOf(":") + 1);
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            if (propertyId != thirdPartySetting.TpSetting["PropertyId"])
            {
                resp.error_code = "10000";
                resp.message = "invalid propertyId";
                log2File.Error($"{propertyId} != {thirdPartySetting.TpSetting["PropertyId"]}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            string signString = $"POST\n{md5}\n{cType}\n{dtDate}\n/transfer";

            //log2File.Info($"{Sign(signString, thirdPartySetting.TpSetting["SHA1key"])}\r\n{signString}\r\n{authorization}", $"{thisTp}_transfer_Sign");

            if (Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) != sign)
            {
                resp.error_code = "10001";
                resp.message = "invalid signature";
                log2File.Error($"{sign} != {Sign(signString, thirdPartySetting.TpSetting["SHA1key"])}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            if (string.IsNullOrEmpty(transferReq.Client) || transferReq.TranId <= 0 || transferReq.Details == null || transferReq.Details.Any(bet => bet.BetNum <= 0))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferReq.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);

            if (!(tpCurrency.TpCurrencyId.Equals(transferReq.Currency, StringComparison.OrdinalIgnoreCase) || (environment.IsDevelopment() && transferReq.Currency.Equals("CNY", StringComparison.OrdinalIgnoreCase))))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            try
            {
                if (transferType == TransferType.Bet)
                {
                    decimal amountSum = 0;
                    bool valid = true;

                    if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, transferReq.Client);

                        if (tpCheck == null)
                        {
                            resp.error_code = "10005";
                            resp.message = "User is disable logons";
                        }
                        else if (tpCheck.Locked)
                        {
                            resp.error_code = "10005";
                            resp.message = "User is disable logons";
                        }
                        else
                        {
                            int result = bettingBLL.PlaceBetLC_Allbet(transferReq, out decimal tpAffterAmount);
                            if (result > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else if (tpAffterAmount < 0)
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "10101";
                                resp.message = "Credit is not enough.";
                            }
                            else if (result == -1)
                            {
                                resp.error_code = "10007";
                                resp.message = "invalid status: transaction had already been cancelled/settled";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "50000";
                                resp.message = "server error";
                            }
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == TransferType.Settle)
                {
                    decimal amountSum = 0;
                    bool valid = true;

                    if (transferReq.Details.Count > 1)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        int result = settledBLL.PayoutLC_Allbet(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == 0)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                        else if (result == -1)
                        {
                            resp.error_code = "10007";
                            resp.message = "invalid status: transaction had already been cancelled/settled";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == TransferType.Cancel)
                {
                    decimal amountSum = 0;
                    bool valid = true;
                    if (transferReq.OriginalTranId <= 0)
                    {
                        valid = false;
                    }
                    else if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        int result = settledBLL.CancelBetLC_Allbet(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (result == -3)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
                else if (transferType == TransferType.ReSettle)
                {
                    decimal amountSum = 0;
                    bool valid = true;

                    if (transferReq.Details.Count > 1)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        int result = settledBLL.ReSettledLC_Allbet(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (tpAffterAmount < 0)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "10101";
                            resp.message = "Credit is not enough.";
                        }
                        else if (result == 0)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                        else if (result == -1)
                        {
                            resp.error_code = "10007";
                            resp.message = "invalid status: transaction had already been cancelled/settled";
                        }
                        else if (result == -2)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{transferType}_Transfer", ex);
                throw ex;
            }

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
            return resp;
        }

        [HttpPost]
        [Route("[controller]/[action]")]
        public async Task<Resp> TransferFixAsync()/*[FromForm]Dictionary<string, string> transferReqq*/
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            string Content = await reader.ReadToEndAsync();

            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

            if (tools.GetClientIP(HttpContext) != "211.75.236.133" && tools.GetClientIP(HttpContext) != "220.130.219.4")
            {
                return resp;
            }

            TransferReq transferReq = null;
            try
            {
                transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);
            }
            catch (Exception ex)
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Transfer", ex);
                return resp;
            }
            TransferType transferType = TransferType.Bet;
            Enum.TryParse(transferReq.TransferType.ToString(), out transferType);

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);
 
            if (string.IsNullOrEmpty(transferReq.Client) || transferReq.TranId <= 0 || transferReq.Details == null || transferReq.Details.Any(bet => bet.BetNum <= 0))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)thisTp, transferReq.Client);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)thisTp, tpCustInfo.CurrencyId);

            if (!(tpCurrency.TpCurrencyId.Equals(transferReq.Currency, StringComparison.OrdinalIgnoreCase) || (environment.IsDevelopment() && transferReq.Currency.Equals("CNY", StringComparison.OrdinalIgnoreCase))))
            {
                resp.error_code = "40000";
                resp.message = "invalid request parameter";
                log2File.Error($"{JsonConvert.SerializeObject(transferReq)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
                return resp;
            }

            try
            {
                if (transferType == TransferType.Cancel)
                {
                    decimal amountSum = 0;
                    bool valid = true;
                    if (transferReq.OriginalTranId <= 0)
                    {
                        valid = false;
                    }
                    else if (transferReq.Amount <= 0)
                    {
                        valid = false;
                    }
                    else
                    {
                        foreach (var betAmt in transferReq.Details)
                        {
                            if (betAmt.Amount <= 0)
                            {
                                valid = false;
                                break;
                            }

                            amountSum += betAmt.Amount;
                        }

                        if (amountSum != transferReq.Amount)
                        {
                            valid = false;
                        }
                    }

                    if (valid)
                    {
                        int result = settledBLL.CancelBetLC_Allbet(transferReq, out decimal tpAffterAmount);

                        if (result > 0)
                        {
                            if (transferReq.Amount > 0)
                            {
                                resp.balance = tpAffterAmount;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                            else
                            {
                                var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                                resp.balance = tpCustBalance.Balance;
                                resp.error_code = "0";
                                resp.message = "success";
                            }
                        }
                        else if (result == -1)
                        {
                            var tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, transferReq.Client, gameType);
                            resp.balance = tpCustBalance.Balance;
                            resp.error_code = "0";
                            resp.message = "success";
                        }
                        else if (result == -3)
                        {
                            resp.error_code = "10006";
                            resp.message = "transaction not existed";
                        }
                    }
                    else
                    {
                        resp.error_code = "40000";
                        resp.message = "invalid request parameter";
                    }
                }
            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{Content}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_{transferType}_Transfer", ex);
                throw ex;
            }

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_{transferType}_Transfer");
            return resp;
        }

        [HttpGet]
        [Route("[controller]/[action]/{client}")]
        public Resp get_balance(string client)
        {
            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

            Request.Headers.TryGetValue("Authorization", out var authorization);
            Request.Headers.TryGetValue("Accept", out var accept);
            Request.Headers.TryGetValue("Date", out var dtDate);

            string propertyId = "";
            string sign = "";
            string sAuthorization = authorization.ToString();
            if (!string.IsNullOrEmpty(sAuthorization))
            {
                propertyId = sAuthorization.Substring(3, sAuthorization.IndexOf(":") - 3);
                sign = sAuthorization.Substring(sAuthorization.IndexOf(":") + 1);
            }

            var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)thisTp);

            if (propertyId != thirdPartySetting.TpSetting["PropertyId"])
            {
                resp.error_code = "10000";
                resp.message = "invalid propertyId";
                log2File.Error($"{propertyId} != {thirdPartySetting.TpSetting["PropertyId"]}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
                return resp;
            }

            string signString = $"GET\n\n\n{dtDate}\n/get_balance/{client}";
            //log2File.Info($"{Sign(signString, thirdPartySetting.TpSetting["SHA1key"])}\r\n{signString}\r\n{authorization}", $"{thisTp}_get_balance_Sign");

            if (Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) != sign)
            {
                resp.error_code = "10001";
                resp.message = "invalid signature";
                log2File.Error($"{sign} != {Sign(signString, thirdPartySetting.TpSetting["SHA1key"]) }\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
                return resp;
            }

            try
            {
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache((int)thisTp, client);
                if (tpCheck == null)
                {
                    resp.error_code = "10005";
                    resp.message = "User is disable logons";
                }
                else if (tpCheck.Locked)
                {
                    resp.error_code = "10005";
                    resp.message = "User is disable logons";
                }
                else
                {
                    //var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client, gameType);
                    TpCustBalance tpCustBalance = null;
                    if (environment.IsDevelopment())
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalance((int)thisTp, client, gameType);
                    }
                    else
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client, gameType);
                    }

                    resp.balance = tpCustBalance.Balance;
                    resp.error_code = "0";
                    resp.message = "success";
                }

            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Balance", ex);
            }

            //log2File.Info($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
            return resp;
        }
        private string Sign(string EncryptText, string EncryptKey)
        {
            using HMACSHA1 myHMACSHA1 = new HMACSHA1(Convert.FromBase64String(EncryptKey));
            byte[] RstRes = myHMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(EncryptText));

            return Convert.ToBase64String(RstRes);
        }
    }
}