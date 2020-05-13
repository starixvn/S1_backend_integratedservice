using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    public class AllbetController : ControllerBase
    {
        private readonly Log2File log2File;
        private readonly Tp thisTp;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly TpCustInfoBLL tpCustInfoBLL;

        public AllbetController(ThirdPartyBLL thirdPartyBLL, TpCustInfoBLL tpCustInfoBLL, Log2File log2File)
        {
            this.log2File = log2File;
            thisTp = Tp.Allbet;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tpCustInfoBLL = tpCustInfoBLL;
        }

        public class Resp
        {
            public string error_code { get; set; }
            public string message { get; set; }
            public decimal balance { get; set; }
        }
        public class TransferReq
        {
            [JsonProperty("amount")]
            public long Amount { get; set; }

            [JsonProperty("client")]
            public string Client { get; set; }

            [JsonProperty("currency")]
            public string Currency { get; set; }

            [JsonProperty("details")]
            public List<Detail> Details { get; set; }

            [JsonProperty("originalTranId")]
            public double OriginalTranId { get; set; }

            [JsonProperty("tranId")]
            public double TranId { get; set; }

            [JsonProperty("transferType")]
            public long TransferType { get; set; }
        }

        public class Detail
        {
            [JsonProperty("amount")]
            public long Amount { get; set; }

            [JsonProperty("betNum")]
            public long BetNum { get; set; }
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

            var transferReq = JsonConvert.DeserializeObject<TransferReq>(Content);

            log2File.Info($"{Content}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Transfer");
            return resp;
        }

        [HttpGet]
        [Route("[controller]/[action]/{client}")]
        public Resp get_balance(string client)
        {
            Resp resp = new Resp();
            resp.error_code = "50000";
            resp.message = "server error";

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
                    var tpCustBalance = tpCustInfoBLL.GetBalanceByHalfSecCache((int)thisTp, client);
                    resp.balance = tpCustBalance.Balance;
                    resp.error_code = "0";
                    resp.message = "success";
                }

            }
            catch (Exception ex)
            {
                resp.error_code = "50000";
                resp.message = "server error";
                log2File.Error($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", $"{thisTp}_Balance");
            }

            log2File.Info($"{JsonConvert.SerializeObject(client)}\r\n{JsonConvert.SerializeObject(resp)}", $"{thisTp}_Balance");
            return resp;
        }
    }
}