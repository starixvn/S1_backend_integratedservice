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
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSABLL
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient client;

        public ApiSABLL(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;

            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                client = _clientFactory.CreateClient("with_proxy");
            }
            else
            {
                client = _clientFactory.CreateClient();
            }
        }

        public async Task<WagerSAModel> GetBetListByTime(string apiUrl, string encrypKey, string md5Key, string secretKey, string startTime, string endTime)
        {
            // param
            string url = $"{apiUrl}";
            string method = "GetAllBetDetailsForTimeIntervalDV";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            string parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            string q = encrypt(parameter, encrypKey);
            string s = ToMD5($"{parameter}{md5Key}{time}{secretKey}");
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
            string q = encrypt(parameter, encrypKey);
            string s = ToMD5($"{parameter}{md5Key}{time}{secretKey}");
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
            var response = await client.SendAsync(request);
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

        public static string encrypt(string data, string base64Key)
        {

            byte[] btIv = Encoding.UTF8.GetBytes(base64Key);
            byte[] btKey = Encoding.UTF8.GetBytes(base64Key);
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] inData = Encoding.UTF8.GetBytes(data);

                using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(btKey, btIv), CryptoStreamMode.Write))
                {
                    cs.Write(inData, 0, inData.Length);
                    cs.FlushFinalBlock();
                }
                return Convert.ToBase64String(ms.ToArray());
            }

        }

        protected string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);
            return md5.ToLower();
        }

    }
}
