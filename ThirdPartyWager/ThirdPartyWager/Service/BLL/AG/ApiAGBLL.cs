using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAGBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Common common;
        private readonly Log2File log2File;
        private readonly ILogger<ApiAGBLL> logger;
        private readonly string logName = Tp.AG.ToString();

        public ApiAGBLL(
            HttpHelper httpHelper,
            Common common,
            Log2File log2File,
            ILogger<ApiAGBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.common = common;
            this.log2File = log2File;
            this.logger = logger;
        }
        public async Task<WagerAGModel> GetBetListByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, int page, string guid)
        {
            // param
            string uri = "/getorders.xml";
            string url = $"{apiUrl}{uri}";
            int perPage = 500;
            string key = common.ToMD5($"{cagent}{startTime}{endTime}{page}{perPage}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&page={page}&perpage={perPage}&key={key}";

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, "row", guid);
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<WagerAGModel> GetBetSummaryByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, string guid)
        {
            // param
            string uri = "/getsumorders.xml";
            string url = $"{apiUrl}{uri}";
            string key = common.ToMD5($"{cagent}{startTime}{endTime}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&key={key}";

            // log
            log2File.AddRequestLog(logName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(url, param, "row", guid);
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<TicketAGModel> CheckTicketStatus(string apiUrl, string transactionID)
        {
            // param
            string uri = "/CheckTicketStatus.ucs";
            string url = $"{apiUrl}{uri}";
            string param = $"transactionID={transactionID}";
            // request
            var r = await DoHttpClientByGetAsync(url, param);
            var result = JsonConvert.DeserializeObject<TicketAGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string tagName, string guid)
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
                LogContext.PushProperty("TpId", logName);
                logger.LogError($"URL {url}");
                logger.LogError($"REQUEST: {param}");
                logger.LogError($"RESPONSE: {response}");
                log2File.AddResponseLog(logName, guid, url, param, response);
                return null;
            }
        }

        private async Task<string> DoHttpClientByGetAsync(string url, string param)
        {

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Content = new StringContent(param, Encoding.UTF8);
            var response = await httpHelper.GetClient().SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;

                // xml to json
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
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
