using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAGBLL
    {
        private readonly string _seamlessUrl;
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiAGBLL> _logger;
        private readonly string _tpName = Tp.AG.ToString();

        public ApiAGBLL(
            IServiceProvider provider,
            IConfiguration configuration
        )
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAGBLL>>();
            _seamlessUrl = configuration.GetValue<string>("TpSWserviceUrl");
        }

        public async Task<WagerAGModel> GetBetListByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, int page, string guid)
        {
            // param
            var httpMethod = "Post";
            string uri = "/getorders.xml";
            string url = $"{apiUrl}{uri}";
            int perPage = 500;
            string key = _common.ToMD5($"{cagent}{startTime}{endTime}{page}{perPage}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&page={page}&perpage={perPage}&key={key}";

            // log
            _log2File.AddRequestLog(_tpName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row", guid);
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<WagerAGModel> GetBetSummaryByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, string guid)
        {
            // param
            var httpMethod = "Post";
            string uri = "/getsumorders.xml";
            string url = $"{apiUrl}{uri}";
            string key = _common.ToMD5($"{cagent}{startTime}{endTime}{hashKey}");
            string param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&key={key}";

            // log
            _log2File.AddRequestLog(_tpName, guid, url, param);

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row", guid);
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<TicketAGModel> CheckTicketStatus(string apiUrl, string transactionID)
        {
            // param
            var httpMethod = "Get";
            string uri = "/resource/CheckTicketStatus.ucs";
            string url = $"{apiUrl}{uri}";
            string param = $"transactionID={transactionID}";

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row", "");
            var result = JsonConvert.DeserializeObject<TicketAGModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string method, string url, string param, string tagName, string guid)
        {
            var httpMethod = (method == "Get") ? HttpMethod.Get : HttpMethod.Post;
            var request = new HttpRequestMessage(httpMethod, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await _httpHelper.GetClient().SendAsync(request);
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
                LogContext.PushProperty("Tp", _tpName);
                _log2File.AddResponseLog(_tpName, guid, url, param, response);
                return null;
            }
        }

        public async Task<string> callSeamlessApi(string type, BettransDataAG bettransDataAG)
        {
            // param
            string url = $"{_seamlessUrl}/AG";
            string uri = "/postTransfer";
            string cagent = "GF4";
            string platformType = "AGIN";
            var wr = new StringWriter();
            bool tag = true;

            switch (type)
            {
                case "Refund":
                    XDocument doc = new XDocument(
                        new XElement("Data",
                        new XElement("Record",
                            new XElement("ticketStatus", type),
                            new XElement("sessionToken", ""),
                            new XElement("currency", bettransDataAG.currency),
                            new XElement("value", bettransDataAG.Amount),
                            new XElement("playname", bettransDataAG.playname),
                            new XElement("agentCode", cagent),
                            new XElement("betTime", bettransDataAG.betTime),
                            new XElement("transactionID", bettransDataAG.transactionID),
                            new XElement("platformType", platformType),
                            new XElement("round", bettransDataAG.round),
                            new XElement("gametype", bettransDataAG.gametype),
                            new XElement("gameCode", bettransDataAG.gameCode),
                            new XElement("tableCode", bettransDataAG.tableCode),
                            new XElement("transactionType", type.ToUpper()),
                            new XElement("transactionCode", bettransDataAG.transactionCode),
                            new XElement("playtype", bettransDataAG.playtype)
                            )
                        )
                    );
                    doc.Declaration = new XDeclaration("1.0", "utf-8", null);
                    doc.Save(wr);
                    break;
                default:
                    tag = false;
                    _logger.LogWarning($"{type} not found");
                    break;
            }

            if (!tag)
            {
                return null;
            }

            // request
            var seamlessApi = $"{url}{uri}";
            var param = wr.ToString();
            return await DoSeamlessHttpClientAsync(seamlessApi, param);
        }

        public async Task<string> DoSeamlessHttpClientAsync(string url, dynamic param)
        {
            try
            {
                HttpRequestMessage request;
                request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(param.ToString(), Encoding.UTF8, "application/xml");

                var response = await _httpHelper.GetClient().SendAsync(request);
                var result = response.Content.ReadAsStringAsync().Result;

                // xml to json
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);
                return JsonConvert.SerializeXmlNode(xmlDoc);
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
