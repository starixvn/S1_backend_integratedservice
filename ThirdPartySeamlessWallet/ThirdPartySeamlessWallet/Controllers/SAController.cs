using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SAController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        private readonly Log2File log2File;
        private readonly int thisTp;
        private readonly int gameType = 0;

        public SAController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools, BettingBLL bettingBLL, SettledBLL settledBLL, Log2File log2File)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
            this.log2File = log2File;
            thisTp = (int)Tp.SA;
            gameType = (int)GameType.SA_LiveCasino;
        }
        public class requestReturn
        {
            public requestReturn()
            {
                error = (int)Error.SystemError;
            }
            public string username { get; set; }
            public string currency { get; set; }
            public decimal amount { get; set; }
            public int error { get; set; }
        }
        public class UserBalance
        {
            public string username { get; set; }
            public string currency { get; set; }
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> GetUserBalanceAsync()
        {
            string Content = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                var uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var userBalance = JsonConvert.DeserializeObject<UserBalance>(JsonConvert.SerializeObject(p));
                string username = userBalance.username;
                string currency = userBalance.currency;

                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache(thisTp, username);
                if (tpCheck == null)
                {
                    resp.error = (int)Error.SessionExpird;
                    return resp;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = (int)Error.CustLocked;
                    return resp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, username, gameType);
                if (tpCustBalance.TpCurrencyId != currency)
                {
                    resp.error = (int)Error.CurrencyInvaild;
                    return resp;
                }

                resp.username = username;
                resp.currency = tpCustBalance.TpCurrencyId;
                resp.amount = tpCustBalance.Balance;
                resp.error = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return resp;
        }
        public class BettingData
        {
            public string username { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string Payouttime { get; set; }
            public string txnid { get; set; }
            public string gametype { get; set; }
            public string txn_reverse_id { get; set; }
            public string timestamp { get; set; }
            public string gameid { get; set; }
            public string hostid { get; set; }
            public string betdetails { get; set; }
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetAsync()
        {
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                //Content = "MDQGx7yYdmalkSSksfvs8qF40umY7PzTZM9E%2bVnXPPH%2b7nFrESN26si7ZCj%2flQZySPPQnneskcnOJRoWF9FKpBwi2cPnExkRydkpDFsLX7FNVcTqbEB%2bVF0bel5OlnZDhc9ENCSYH8kU5%2fycrhMah1xJ31ru2oqpVh99nkEaXdZeSiSrLQlL9LARk16BDN4mXScj%2bSX9RXgjRIcW4dNRY3cp9Pt2w%2fZd";
                //logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));
                var tpCheck = thirdPartyBLL.ThirdPartyCheckWithCache(thisTp, bettingDataLC_SA.username);
                if (tpCheck == null)
                {
                    resp.error = (int)Error.SessionExpird;
                    return resp;
                }
                else if (tpCheck.Locked)
                {
                    resp.error = (int)Error.CustLocked;
                    return resp;
                }

                var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                if (tpCustBalance.TpCurrencyId != bettingDataLC_SA.currency)
                {
                    resp.error = (int)Error.CurrencyInvaild;
                    return resp;
                }

                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_SA));
                    if (bettingBLL.PlaceBetLC_SA(bettingDataLC_SA, out tpAffterAmount) > 0)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        resp.amount = tpAffterAmount;
                        resp.username = bettingDataLC_SA.username;
                        //throw new NotImplementedException();
                    }
                    else if (tpAffterAmount < 0)
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.AmountInvaild;
                }

                log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", $"SA_PlaceBet");
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                //logger.LogError(ex.Message);
                log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "SA_PlaceBet");
                logger.LogError(ex, $"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}");
            }

            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerWinAsync()
        {
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                logger.LogInformation(Content);
                //Content = "MDQGx7yYdmalkSSksfvs8qF40umY7PzTZM9E%2bVnXPPH%2b7nFrESN26hkTglOqQkLkjUBl8D9dP4z7La4%2fjyq5KkoKfNdAfQlBUMZjEinFXaUSdsCtQKuZ%2b%2ff%2fcnVlyj45nY1YNVSHVB0n9NlO3wju7SDFfeGZ6%2b25JJuLpXQppdvWu1PHq3f0aDsfG4ETIgXIVXwObhedPr%2fnKLfOXuemzdW04Y0Hvmt5uZQSev0oU9k%3d";
                var content = HttpUtility.UrlDecode(Content);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var payoutDataLC_SA = JsonConvert.DeserializeObject<PayoutDataLC_SA>(JsonConvert.SerializeObject(p));
                decimal tpAffterAmount = 0;
                if (payoutDataLC_SA.amount > 0)
                {
                    var result = settledBLL.PayoutLC_SA(payoutDataLC_SA, out tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = payoutDataLC_SA.currency;
                        var tpCustBalance = tpCustInfoBLL.GetBalance(thisTp, payoutDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = payoutDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.AmountInvaild;
                }

                log2File.Info($"{JsonConvert.SerializeObject(payoutDataLC_SA)}\r\n{JsonConvert.SerializeObject(resp)}", $"SA_PlayerWin");
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "SA_PlayerWin");
                logger.LogError(ex, $"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}");
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerLostAsync()
        {
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var payoutDataLC_SA = JsonConvert.DeserializeObject<PayoutDataLC_SA>(JsonConvert.SerializeObject(p));
                decimal tpAffterAmount = 0;

                var result = settledBLL.PayoutLC_SA(payoutDataLC_SA, out _);
                if (result > 0 || result == -1)
                {
                    resp.error = (int)Error.Success;
                    resp.currency = payoutDataLC_SA.currency;
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.username = payoutDataLC_SA.username;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.BalanceNotEnough;
                }
                else
                {
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, payoutDataLC_SA.username, gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.Error;
                }

                log2File.Info($"{JsonConvert.SerializeObject(payoutDataLC_SA)}\r\n{JsonConvert.SerializeObject(resp)}", $"SA_PlayerLost");
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "SA_PlayerLost");
                logger.LogError(ex, $"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}");
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetCancelAsync()
        {
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));
                logger.LogInformation(JsonConvert.SerializeObject(p));
                //
                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    var result = settledBLL.CancelBetLC_SA(bettingDataLC_SA, out tpAffterAmount);
                    if (result > 0)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        resp.amount = tpAffterAmount;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (result == -1)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    resp.error = (int)Error.AmountInvaild;
                }

                log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", $"SA_PlaceBetCancel");
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "SA_PlaceBetCancel");
                logger.LogError(ex, $"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}");
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetCancelFixAsync()
        {
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();

            if (tools.GetClientIP(HttpContext) != "211.75.236.133" && tools.GetClientIP(HttpContext) != "220.130.219.4")
            {
                resp.error = (int)Error.Error;
                return resp;
            }

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting(thisTp);
                var p = QueryHelpers.ParseQuery(Content).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));
                //
                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    var result = settledBLL.CancelBetLC_SA(bettingDataLC_SA, out tpAffterAmount);
                    if (result > 0)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        resp.amount = tpAffterAmount;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (result == -1)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache(thisTp, bettingDataLC_SA.username, gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    resp.error = (int)Error.AmountInvaild;
                }

                log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", $"SA_PlaceBetCancel");
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "SA_PlaceBetCancel");
                logger.LogError(ex, $"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}");
            }
            return resp;
        }
        public enum Error
        {
            Success = 0,
            CustNotExists = 1000,
            CurrencyInvaild = 1001,
            AmountInvaild = 1002,
            CustLocked = 1003,
            BalanceNotEnough = 1004,
            Error = 1005,
            DecryptInvaild = 1006,
            SessionExpird = 1007,
            SystemError = 9999
        }
    }
}
