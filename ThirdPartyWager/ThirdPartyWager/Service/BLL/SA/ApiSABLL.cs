using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSABLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiSABLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }

        public async Task<WagerSAModel> GetBetListByTime(string apiUrl, string encrypKey, string md5Key, string secretKey, string startTime, string endTime)
        {
            // param
            string url = $"{apiUrl}";
            string method = "GetAllBetDetailsForTimeIntervalDV";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            string parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            string q = common.encrypt(parameter, encrypKey);
            string s = common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            string param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={ System.Web.HttpUtility.UrlEncode(s)}";

            // request
            var r = await DoHttpClientAsync(url, param, "BetDetail");
            var result = JsonConvert.DeserializeObject<WagerSAModel>(r);
            return result;
        }

        public async Task<WagerRedoSAModel> GetBetSummaryByTime(string apiUrl, string encrypKey, string md5Key, string secretKey, string startTime, string endTime)
        {
            // param
            string url = $"{apiUrl}";
            string method = "GetTransactionDetails";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            string parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            string q = common.encrypt(parameter, encrypKey);
            string s = common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            string param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={ System.Web.HttpUtility.UrlEncode(s)}";

            // request
            var r = await DoHttpClientAsync(url, param, "TransactionDetails");
            var result = JsonConvert.DeserializeObject<WagerRedoSAModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string tagName)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;

                // xml to json
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);

                // single data force to array
                var eleTag = xmlDoc.GetElementsByTagName(tagName);
                if (eleTag.Count == 1)
                {
                    var attribute = xmlDoc.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
                    attribute.InnerText = "true";
                    var node = eleTag.Item(0) as XmlElement;
                    node.Attributes.Append(attribute);
                }
                string json = JsonConvert.SerializeXmlNode(xmlDoc);

                return json;
            }
            else
            {
                return null;
            }
        }
    }
}
