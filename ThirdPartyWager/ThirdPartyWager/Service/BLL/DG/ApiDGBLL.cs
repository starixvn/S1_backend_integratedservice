using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;

        public ApiDGBLL(HttpHelper httpHelper, Common common, Log2File log2File)
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
        }
        public async Task<WagerDGModel> GetBetList(string apiUrl, string agentName, string apiKey)
        {
            // param
            string uri = "/game/getReport/";
            string random = common.GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = common.ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random
            };

            // request
            log2File.Info($"Url:\r\n{url}\r\nRequest:\r\n{JsonConvert.SerializeObject(param)}", Tp.DG.ToString());
            var r = await DoHttpClientAsync(url, param);
            log2File.Info($"\r\nUrl:\r\n{url}\r\nResponse:\r\n{r}", Tp.DG.ToString());
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            return result;
        }

        public async Task MarkGotBetList(string apiUrl, string agentName, string apiKey, IEnumerable<long> wagerIds)
        {
            // param
            string uri = "/game/markReport/";
            string random = common.GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = common.ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = token,
                random = random,
                list = wagerIds
            };

            // request
            log2File.Info($"Url:\r\n{url}\r\nRequest:\r\n{JsonConvert.SerializeObject(param)}", Tp.DG.ToString());
            string r = await DoHttpClientAsync(url, param);
            log2File.Info($"\r\nUrl:\r\n{url}\r\nResponse:\r\n{r}", Tp.DG.ToString());
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (result.codeId != 0)
            {
                throw new Exception("Error.");
            }
            return;
        }

        public async Task<WagerRedoDGModel> GetBetListByTime(string apiUrl, string agentName, string apiKey, string beginTime, string endTime, int pageNow)
        {
            // param
            string uri = "/game/getReport";
            string url = $"{apiUrl}{uri}";
            string token = common.ToMD5($"{agentName}{apiKey}").ToLower();

            var param = new
            {
                token = token,
                beginTime = beginTime,
                endTime = endTime,
                agentName = agentName,
                pageNow = pageNow
            };
            // request
            log2File.Info($"Url:\r\n{url}\r\nRequest:\r\n{JsonConvert.SerializeObject(param)}", Tp.DG.ToString());
            var r = await DoHttpClientAsync(url, param);
            log2File.Info($"\r\nUrl:\r\n{url}\r\nResponse:\r\n{r}", Tp.DG.ToString());
            var result = JsonConvert.DeserializeObject<WagerRedoDGModel>(r);
            return result;
        }
        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var json = JsonConvert.SerializeObject(param);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return response.Content.ReadAsStringAsync().Result;
                //return null;
            }
        }
    }
}
