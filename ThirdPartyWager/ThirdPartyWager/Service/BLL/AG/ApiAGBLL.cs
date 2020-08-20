using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAGBLL
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient client;

        public ApiAGBLL(IHttpClientFactory clientFactory, IConfiguration configuration)
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

        public async Task<WagerAGModel> GetBetListByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, int page)
        {
            // param
            string uri = "/getorders.xml";
            string url = $"{apiUrl}{uri}";
            int perPage = 5;
            string key = ToMD5($"{cagent}{startTime}{endTime}{page}{perPage}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&page={page}&perpage={perPage}&key={key}";
           
            // request
            var r = await DoHttpClientAsync(url, param, "row");
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            
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
