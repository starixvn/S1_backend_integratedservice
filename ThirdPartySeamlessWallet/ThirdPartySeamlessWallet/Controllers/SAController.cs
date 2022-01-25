using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly ILogger<SAController> _logger;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly Tools _tools;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly int _thisTp;
        private readonly int _gameType = 0;
        private readonly string _tpName = Tp.SA.ToString();
        private readonly IHostEnvironment _environment;

        public SAController(IServiceProvider provider)
        {
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            _logger = provider.GetRequiredService<ILogger<SAController>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _thisTp = (int)Tp.SA;
            _gameType = (int)GameType.SA_LiveCasino;
            _environment = provider.GetRequiredService<IHostEnvironment>();
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

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                var uriQuery = _tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var userBalance = JsonConvert.DeserializeObject<UserBalance>(JsonConvert.SerializeObject(p));
                string username = userBalance.username;
                string currency = userBalance.currency;

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, username);
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

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, username, _gameType);
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
                _logger.LogError(ex, ex.Message);
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
            string logName = $"{_tpName}_PlaceBet";
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                uriQuery = _tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));

                var tpCheck = _thirdPartyBLL.ThirdPartyCheckWithCache(_thisTp, bettingDataLC_SA.username);
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

                var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                if (tpCustBalance.TpCurrencyId != bettingDataLC_SA.currency)
                {
                    resp.error = (int)Error.CurrencyInvaild;
                    return resp;
                }

                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    _logger.LogInformation(JsonConvert.SerializeObject(bettingDataLC_SA));
                    var result = _bettingBLL.PlaceBetLC_SA(bettingDataLC_SA, out tpAffterAmount);
                    if (result > 0)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = bettingDataLC_SA.currency;
                        resp.amount = tpAffterAmount;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                        _log2File.Error($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\ntpAffterAmount={tpAffterAmount}", logName);
                        return resp;
                    }
                    else
                    {
                        tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                        _log2File.Error($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\nresult={result}", logName);
                        return resp;
                    }
                }
                else
                {
                    tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.AmountInvaild;
                }

                _log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                _log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
            }

            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerWinAsync()
        {
            string logName = $"{_tpName}_PlayerWin";
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                _logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                uriQuery = _tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var payoutDataLC_SA = JsonConvert.DeserializeObject<PayoutDataLC_SA>(JsonConvert.SerializeObject(p));
                decimal tpAffterAmount = 0;

                if (payoutDataLC_SA.amount > 0)
                {
                    var result = _settledBLL.PayoutLC_SA(payoutDataLC_SA, out tpAffterAmount);
                    if (result > 0 || result == -1)
                    {
                        resp.error = (int)Error.Success;
                        resp.currency = payoutDataLC_SA.currency;
                        var tpCustBalance = _tpCustInfoBLL.GetBalance(_thisTp, payoutDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = payoutDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.AmountInvaild;
                }

                _log2File.Info($"{JsonConvert.SerializeObject(payoutDataLC_SA)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                _log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerLostAsync()
        {
            string logName = $"{_tpName}_PlayerLost";
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                _logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                uriQuery = _tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var payoutDataLC_SA = JsonConvert.DeserializeObject<PayoutDataLC_SA>(JsonConvert.SerializeObject(p));
                decimal tpAffterAmount = 0;

                var result = _settledBLL.PayoutLC_SA(payoutDataLC_SA, out _);
                if (result > 0 || result == -1)
                {
                    resp.error = (int)Error.Success;
                    resp.currency = payoutDataLC_SA.currency;
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.username = payoutDataLC_SA.username;
                }
                else if (tpAffterAmount < 0)
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.BalanceNotEnough;
                }
                else
                {
                    var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, payoutDataLC_SA.username, _gameType);
                    resp.amount = tpCustBalance.Balance;
                    resp.error = (int)Error.Error;
                }

                _log2File.Info($"{JsonConvert.SerializeObject(payoutDataLC_SA)}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                _log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetCancelAsync()
        {
            string logName = $"{_tpName}_PlaceBetCancel";
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                _logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = _thirdPartyBLL.GetThirdPartySetting(_thisTp);
                uriQuery = _tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));
                _logger.LogInformation(JsonConvert.SerializeObject(p));

                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    var result = _settledBLL.CancelBetLC_SA(bettingDataLC_SA, out tpAffterAmount);
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
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    resp.error = (int)Error.AmountInvaild;
                }

                _log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                _log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
            }
            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetCancelFixAsync()
        {
            string logName = $"{_tpName}_PlaceBetCancelFix";
            string Content = "";
            string uriQuery = "";
            var resp = new requestReturn();

            if (_tools.GetClientIP(HttpContext) != "211.75.236.133" && _tools.GetClientIP(HttpContext) != "220.130.219.4")
            {
                resp.error = (int)Error.Error;
                return resp;
            }

            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var p = QueryHelpers.ParseQuery(Content).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());

                var bettingDataLC_SA = JsonConvert.DeserializeObject<BettingDataLC_SA>(JsonConvert.SerializeObject(p));

                decimal tpAffterAmount = 0;
                if (bettingDataLC_SA.amount > 0)
                {
                    var result = _settledBLL.CancelBetLC_SA(bettingDataLC_SA, out tpAffterAmount);
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
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.username = bettingDataLC_SA.username;
                    }
                    else if (tpAffterAmount < 0)
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.BalanceNotEnough;
                    }
                    else
                    {
                        var tpCustBalance = _tpCustInfoBLL.GetBalanceByHalfSecCache(_thisTp, bettingDataLC_SA.username, _gameType);
                        resp.amount = tpCustBalance.Balance;
                        resp.error = (int)Error.Error;
                    }
                }
                else
                {
                    resp.error = (int)Error.AmountInvaild;
                }

                _log2File.Info($"{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}", logName);
            }
            catch (Exception ex)
            {
                resp.error = (int)Error.SystemError;
                _log2File.Error($"{Content}\r\n{uriQuery}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", logName, ex);
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
