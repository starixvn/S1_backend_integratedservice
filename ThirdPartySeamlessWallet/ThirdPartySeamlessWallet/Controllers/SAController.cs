using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.BLL;
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

        public SAController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
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

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn GetUserBalance([FromBody]string Content)
        {
            var content = HttpUtility.UrlDecode(Content);



            var resp = new requestReturn();
            /*resp.username = username;
            resp.currency = currency;*/
            try
            {
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
                resp.amount = tpCustBalance.Balance;
                resp.error = 0;
            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }

            return resp;
        }

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn PlaceBet(string username, string currency, decimal amount, string txnid, DateTime timestamp, string ip, string gametype, byte platform, int hostid, string gameid, string betdetails)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn PlayerWin(string username, string currency, decimal amount, string txnid, DateTime timestamp, string gametype, DateTime Payouttime, int hostid, string gameid)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn PlayerLost(string username, string currency, string txnid, DateTime timestamp, string gametype, DateTime Payouttime, int hostid, string gameid)
        {
            throw new NotImplementedException();
        }

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn PlaceBetCancel(string username, string currency, decimal amount, string txnid, DateTime timestamp, string gametype, int hostid, string gameid, string txn_reverse_id)
        {
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
