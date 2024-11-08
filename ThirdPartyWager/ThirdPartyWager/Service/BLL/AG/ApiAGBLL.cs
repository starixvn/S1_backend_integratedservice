using System.Text;
using System.Xml;
using System.Xml.Linq;
using Newtonsoft.Json;
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

        public ApiAGBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiAGBLL>>();
            _seamlessUrl = provider.GetRequiredService<ConfigBLL>().GetConfigValueByEnvironment<ServiceHostConfig>("ServiceHost").SeamlessService;
        }

        public async Task<WagerAGModel> GetBetListByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime, int page)
        {
            // param
            var httpMethod = "Post";
            var uri = "/getorders.xml";
            var url = $"{apiUrl}{uri}";
            var perPage = 500;
            var key = _common.ToMD5($"{cagent}{startTime}{endTime}{page}{perPage}{hashKey}");
            var param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&page={page}&perpage={perPage}&key={key}";

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row");
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<WagerAGModel> GetBetSummaryByTime(string apiUrl, string hashKey, string cagent, string startTime, string endTime)
        {
            // param
            var httpMethod = "Post";
            var uri = "/getsumorders.xml";
            var url = $"{apiUrl}{uri}";
            var key = _common.ToMD5($"{cagent}{startTime}{endTime}{hashKey}");
            var param = $"cagent={cagent}&startdate={startTime}&enddate={endTime}&key={key}";

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row");
            var result = JsonConvert.DeserializeObject<WagerAGModel>(r);
            return result;
        }

        public async Task<TicketDataAG> CheckTicketStatus(string apiUrl, string transactionID)
        {
            // param
            var httpMethod = "Get";
            var uri = "/resource/CheckTicketStatus.ucs";
            var url = $"{apiUrl}{uri}";
            var param = $"transactionID={transactionID}";

            // request
            var r = await DoHttpClientAsync(httpMethod, url, param, "row");
            var result = JsonConvert.DeserializeObject<TicketDataAG>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string method, string url, string param, string tagName)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");
            var httpMethod = (method == "Get") ? HttpMethod.Get : HttpMethod.Post;
            var request = new HttpRequestMessage(httpMethod, url)
            {
                Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded")
            };

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param, result);
                return null;
            }

            // xml to json
            var xmlDoc = new XmlDocument();
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

            return JsonConvert.SerializeXmlNode(xmlDoc);
        }

        public async Task<string> callSeamlessApi(string type, BettransDataAG bettransDataAG)
        {
            // param
            var url = $"{_seamlessUrl}AG";
            var uri = "/postTransfer";
            var cagent = "GF4";
            var platformType = "AGIN";
            var wr = new StringWriter();
            var tag = true;

            switch (type)
            {
                case "Refund":
                    var doc = new XDocument(
                        new XElement("Data",
                        new XElement("Record",
                            new XElement("ticketStatus", type),
                            new XElement("sessionToken", ""),
                            new XElement("currency", bettransDataAG.currency),
                            new XElement("value", Math.Abs(bettransDataAG.Amount)),
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
                _log2File.InsertRequestLog(_tpName, url, param, "1");
                HttpRequestMessage request;
                request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(param, Encoding.UTF8, "application/xml");

                var response = await _httpHelper.GetClient().SendAsync(request);
                var content = response.Content.ReadAsStringAsync().Result;

                if (!response.IsSuccessStatusCode)
                {
                    _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                    _log2File.InsertResponseLog(_tpName, url, param, content);
                    return content;
                }

                // xml to json
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(content);
                return JsonConvert.SerializeXmlNode(xmlDoc);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return ex.Message;
            }
        }
    }
}
