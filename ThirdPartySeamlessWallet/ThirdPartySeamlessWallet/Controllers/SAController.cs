using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.BLL;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ThirdPartySeamlessWallet
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class SAController : ControllerBase
    {
        private readonly CustBLL custBLL;

        public SAController(CustBLL custBLL)
        {
            this.custBLL = custBLL;
        }
        public class requestReturn
        {
            public string username { get; set; }
            public string currency { get; set; }
            public decimal amount { get; set; }
            public int error { get; set; }
        }

        [HttpPost]
        [Produces("application/xml")]
        public requestReturn GetUserBalance([FromForm]string username, string currency)
        {

            var resp = new requestReturn();
            resp.username = username;
            resp.currency = currency;

            //resp.amount = custBLL.GetBalance;
            resp.error = 0;
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
}
