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
using ThirdPartySeamlessWallet.Service.Model.enums;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ThirdPartySeamlessWallet
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SAController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;

        public SAController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
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
                logger.LogInformation(Content);
                var content = HttpUtility.UrlDecode(Content);

                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.SA);
                var uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var userBalance = JsonConvert.DeserializeObject<UserBalance>(JsonConvert.SerializeObject(p));

                string username = userBalance.username;
                string currency = userBalance.currency;

                var tpCheck = tpCustInfoBLL.ThirdPartyCheckWithCache((int)Tp.SA, username);
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

                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.SA, username);
                if (tpCustBalance.TpCurrencyId != currency)
                {
                    resp.error = (int)Error.CurrencyInvaild;
                    return resp;
                }

                resp.username = username;
                resp.currency = tpCustBalance.TpCurrencyId;
                resp.amount = 10000000m;//tpCustBalance.Balance;
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
            public string txnid { get; set; }
            public string gametype { get; set; }
            public string txn_reverse_id { get; set; }
            public string timestamp { get; set; }
            public string gameid { get; set; }
            public string hostid { get; set; }
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetAsync()
        {
            string Content = "";
            var resp = new requestReturn();
            try
            {
                using var reader = new StreamReader(Request.Body, Encoding.UTF8);
                Content = await reader.ReadToEndAsync();
                var content = HttpUtility.UrlDecode(Content);
                var thirdPartySetting = thirdPartyBLL.GetThirdPartySetting((int)Tp.SA);
                var uriQuery = tools.DESDecrypt(content, thirdPartySetting.TpSetting["EncrypKey"]);
                var p = QueryHelpers.ParseQuery(uriQuery).ToDictionary(kv => kv.Key, kv => kv.Value.ToString());
                var bettingData = JsonConvert.DeserializeObject<BettingData>(JsonConvert.SerializeObject(p));
                //bettingData.
                var tpCheck = tpCustInfoBLL.ThirdPartyCheckWithCache((int)Tp.SA, bettingData.username);
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

                var tpCustBalance = tpCustInfoBLL.GetBalance((int)Tp.SA, bettingData.username);
                if (tpCustBalance.TpCurrencyId != bettingData.currency)
                {
                    resp.error = (int)Error.CurrencyInvaild;
                    return resp;
                }


            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerWinAsync()
        {
            string Content = "";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            Content = await reader.ReadToEndAsync();
            logger.LogInformation(Content);
            throw new NotImplementedException();
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlayerLostAsync()
        {
            string Content = "";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            Content = await reader.ReadToEndAsync();
            logger.LogInformation(Content);
            throw new NotImplementedException();
        }

        [HttpPost]
        [Produces("application/xml")]
        public async Task<requestReturn> PlaceBetCancelAsync()
        {
            string Content = "";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            Content = await reader.ReadToEndAsync();
            logger.LogInformation(Content);
            throw new NotImplementedException();
        }
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
