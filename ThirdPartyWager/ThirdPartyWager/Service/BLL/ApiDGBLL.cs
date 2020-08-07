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

        public async Task GetBetList(string apiUrl, string agentName, string apiKey)
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
            var response = DoHttpClientAsync(url, param);
            string r = await response.Content.ReadAsStringAsync().Result;
            var dataWager = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (dataWager.codeId != 0)
            {
                // error
                // return
            }
            if (dataWager.list == null || dataWager.list.Count() == 0)
            {
                // no data
                // return
            }

            // data
            return;
        }

        public Task MarkGotBetList(string apiUrl, string agentName, string apiKey, int[] idList)
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
                list = $"{idList}"
            };

            // request
            var response = DoHttpClientAsync(url, param);

            string r = response.Content.ReadAsStringAsync().Result;
            var dataWager = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (dataWager.codeId != 0)
            {
                // error
                // return
            }

            // data
            return;
        }

        public Task GetBetListByTime(string apiUrl, string agentName, string apiKey, string beginTime, string endTime, int pageNow)
        {
            // param
            string uri = "/game/markReport/";
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
            var response = DoHttpClientAsync(url, param);

            string r = response.Content.ReadAsStringAsync().Result;
            var dataWager = JsonConvert.DeserializeObject<WagerDGModel>(r);

            if (dataWager.codeId != 0)
            {
                // error
                // return
            }
            if (dataWager.list == null || dataWager.list.Count() == 0)
            {
                // no data
                // return
            }

            // data
            return;
        }

        private async Task<HttpResponseMessage> DoHttpClientAsync(string url, object param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            var json = JsonConvert.SerializeObject(param);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            var client = _clientFactory.CreateClient();
            // response
            //var response = await client.SendAsync(request);
            // check status code
            //if (response.IsSuccessStatusCode)
            //{
            //}
            //else
            //{
            //}

            //return response;
            return await client.SendAsync(request);
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
