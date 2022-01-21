using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSABLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiSABLL> _logger;
        private readonly string _tpName = Tp.SA.ToString();

        public ApiSABLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiSABLL>>();
        }

        public async Task<WagerSAModel> GetBetListByTime(string apiUrl, string encrypKey, string md5Key, string secretKey, string startTime, string endTime, string guid)
        {
            // param
            string url = $"{apiUrl}";
            string method = "GetAllBetDetailsForTimeIntervalDV";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            string parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            string q = _common.encrypt(parameter, encrypKey);
            string s = _common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            string param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={ System.Web.HttpUtility.UrlEncode(s)}";

            // log
            _logger.LogInformation(parameter);

            // request
            var r = await DoHttpClientAsync(url, param, "BetDetail");
            var result = JsonConvert.DeserializeObject<WagerSAModel>(r);
            return result;
        }

        public async Task<WagerRedoSAModel> GetBetSummaryByTime(string apiUrl, string encrypKey, string md5Key, string secretKey, string startTime, string endTime, string guid)
        {
            // param
            string url = $"{apiUrl}";
            string method = "GetTransactionDetails";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            string parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            string q = _common.encrypt(parameter, encrypKey);
            string s = _common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            string param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={ System.Web.HttpUtility.UrlEncode(s)}";

            // log
            _logger.LogInformation(parameter);

            // request
            var r = await DoHttpClientAsync(url, param, "TransactionDetails");
            var result = JsonConvert.DeserializeObject<WagerRedoSAModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param, string tagName)
        {
            _log2File.InsertRequestLog(_tpName, url, param, "1");

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");

            var response = await _httpHelper.GetClient().SendAsync(request);
            var result = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, param.ToString(), result);
                return null;
            }

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
            return JsonConvert.SerializeXmlNode(xmlDoc);
        }
    }
}
