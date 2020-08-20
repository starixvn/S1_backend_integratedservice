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
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;

        public ApiAGBLL(HttpHelper httpHelper, Common common)
        {
            this.httpHelper = httpHelper;
            this.common = common;
        }
        public async Task<WagerAGModel> GetBetListByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, int page)
        {
            // param
            string uri = "/getorders.xml";
            string url = $"{apiUrl}{uri}";
            int perPage = 500;
            string key = common.ToMD5($"{cagent}{startTime}{endTime}{page}{perPage}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&page={page}&perpage={perPage}&key={key}";
           
            // request
            var r = await DoHttpClientAsync(url, param, "row");
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            
            return result;
        }

        public async Task<WagerAGModel> GetBetSummaryByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime)
        {
            // param
            string uri = "/getsumorders.xml";
            string url = $"{apiUrl}{uri}";
            string key = common.ToMD5($"{cagent}{startTime}{endTime}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&key={key}";

            // request
            var r = await DoHttpClientAsync(url, param, "row");
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
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
