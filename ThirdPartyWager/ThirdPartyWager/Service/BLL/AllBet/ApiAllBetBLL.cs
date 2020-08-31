using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAllBetBLL
    {
        private byte[] NULL_IV = Convert.FromBase64String("AAAAAAAAAAA=");
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiAllBetBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerAllBetModel> GetBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex)
        {
            // param
            string uri = "/betlog_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={common.random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = common.encrypt(parameter, desKey, null, NULL_IV);
            string sign = common.base64edMd5($"{data}{md5Key}");
            string param = $"data={HttpUtility.UrlEncode(data)}&sign={HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // request
            var r = await DoHttpClientAsync(url, param);

            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);
                return result;
            }
        }

        public async Task<WagerAllBetModel> GetModifiedBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex)
        {
            // param
            string uri = "/betlog_modified_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={common.random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = common.encrypt(parameter, desKey, null, NULL_IV);
            string sign = common.base64edMd5($"{data}{md5Key}");
            string param = $"data={HttpUtility.UrlEncode(data)}&sign={HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // request
            var r = await DoHttpClientAsync(url, param);
            if (string.IsNullOrEmpty(r))
            {
                return null;
            }
            else
            {
                var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);
                return result;
            }
        }

        private async Task<string> DoHttpClientAsync(string url, string param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return null;
            }
        }
    }
}
