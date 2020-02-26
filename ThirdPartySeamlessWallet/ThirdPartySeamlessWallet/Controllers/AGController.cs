using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class AGController : ControllerBase
    {
        private readonly TpCustInfoBLL tpCustInfoBLL;
        private readonly ILogger<SAController> logger;
        private readonly ThirdPartyBLL thirdPartyBLL;
        private readonly Tools tools;
        private readonly BettingBLL bettingBLL;
        private readonly SettledBLL settledBLL;
        public AGController(ILogger<SAController> logger, TpCustInfoBLL tpCustInfoBLL, ThirdPartyBLL thirdPartyBLL, Tools tools, BettingBLL bettingBLL, SettledBLL settledBLL)
        {
            this.tpCustInfoBLL = tpCustInfoBLL;
            this.logger = logger;
            this.thirdPartyBLL = thirdPartyBLL;
            this.tools = tools;
            this.bettingBLL = bettingBLL;
            this.settledBLL = settledBLL;
        }
        [HttpPost]
        public async Task<string> postTransferAsync()
        {
            string Content = "";
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            Content = await reader.ReadToEndAsync();
            //Content= @"<?xml version=""1.0"" encoding=""UTF - 8"" standalone=""yes""?><Data><Record><sessionToken>flsoD88qyUOfnszWkVjMog</sessionToken><currency>CNY</currency><value>100.0000</value><playname>GF4DNova88_204</playname><agentCode>GF4</agentCode><betTime>02/26/2020 03:23:21</betTime><transactionID>000820022603232100084</transactionID><platformType>AGIN</platformType><round>DSP</round><gametype>BAC</gametype><gameCode>GC0012022607B</gameCode><tableCode>C101</tableCode><transactionType>BET</transactionType><transactionCode>BCB</transactionCode><deviceType>DESKTOP</deviceType><playtype>2</playtype></Record></Data>";
            logger.LogInformation(Content);
            //var content = HttpUtility.UrlDecode(Content);
            return "";
        }
    }
}