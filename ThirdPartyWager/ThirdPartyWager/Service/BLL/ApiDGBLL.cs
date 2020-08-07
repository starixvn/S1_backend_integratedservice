using Newtonsoft.Json;
using Org.BouncyCastle.Math;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiDGBLL
    {
        private readonly IHttpClientFactory _clientFactory;

        public ApiDGBLL(IHttpClientFactory clientFactory)
        {
            _clientFactory = clientFactory;
        }

        public async Task<WagerDGModel> GetBetList(string apiUrl, string agentName, string apiKey)
        {
            // param
            string uri = "/game/getReport/";
            string random = GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = $"{token}",
                random = $"{random}"
            };

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (result.list == null || result.list.Count() == 0)
            {
                throw new Exception("No Data.");
            }

            return result;
        }

        public async Task MarkGotBetList(string apiUrl, string agentName, string apiKey, IEnumerable<long> wagerIds)
        {
            // param
            string uri = "/game/markReport/";
            string random = GetRandomString(8);
            string url = $"{apiUrl}{uri}{agentName}";
            string token = ToMD5($"{agentName}{apiKey}{random}").ToLower();
            var param = new
            {
                token = $"{token}",
                random = $"{random}",
                list = wagerIds
            };

            // request
            string r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (result.codeId != 0)
            {
                throw new Exception("Error.");
            }
            return;
        }

        public async Task<WagerDGModel> GetBetListByTime(string apiUrl, string agentName, string apiKey, string beginTime, string endTime, int pageNow)
        {
            // param
            string uri = "/game/getReport/";
            string url = $"{apiUrl}{uri}{agentName}";
            string token = ToMD5($"{agentName}{apiKey}").ToLower();
            var param = new
            {
                token = $"{token}",
                beginTime = $"{beginTime}",
                endTime = $"{endTime}",
                agentName = $"{agentName}"
            };

            // request
            var r = await DoHttpClientAsync(url, param);

            var result = JsonConvert.DeserializeObject<WagerDGModel>(r);

            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var json = JsonConvert.SerializeObject(param);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _clientFactory.CreateClient();
            var response = await client.SendAsync(request);
            return response.Content.ReadAsStringAsync().Result;
        }

        protected string GetRandomString(int length)
        {
            var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }

        protected string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);

            return md5;
        }
    }
}
