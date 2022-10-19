using Newtonsoft.Json;
using System.Text;
using System.Xml;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiSimplePlayBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly ILogger<ApiSimplePlayBLL> _logger;
        private readonly string _tpName = Tp.SimplePlay.ToString();

        public ApiSimplePlayBLL(IServiceProvider provider)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _logger = provider.GetRequiredService<ILogger<ApiSimplePlayBLL>>();
        }

        public async Task<WagerSimplePlayModel> GetBetListByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var encrypKey = setting["EncrypKey"];
            var md5Key = setting["MD5Key"];
            var secretKey = setting["SecretKey"];

            // param
            var url = $"{apiUrl}";
            var method = "GetAllBetDetailsForTimeInterval";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            var parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            var q = _common.encrypt(parameter, encrypKey);
            var s = _common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            var param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={System.Web.HttpUtility.UrlEncode(s)}";

            // log
            _logger.LogInformation(parameter);

            // request
            var r = await DoHttpClientAsync(url, param, "BetDetail");
            var result = JsonConvert.DeserializeObject<WagerSimplePlayModel>(r);
            return result;
        }

        public async Task<CurrencyStats_SimplePlay> GetBetSummaryByTime(dynamic setting, string startTime, string endTime)
        {
            var apiUrl = setting["ApiUrl"];
            var encrypKey = setting["EncrypKey"];
            var md5Key = setting["MD5Key"];
            var secretKey = setting["SecretKey"];

            // param
            var url = $"{apiUrl}";
            var method = "GetTransactionDetails";
            var time = new DateTimeOffset(DateTime.Now).ToString("yyyyMMddHHmmss");
            var parameter = $"method={method}&Time={time}&FromTime={startTime}&ToTime={endTime}&Key={secretKey}";

            // encrype param
            var q = _common.encrypt(parameter, encrypKey);
            var s = _common.ToMD5($"{parameter}{md5Key}{time}{secretKey}");
            var param = $"q={System.Web.HttpUtility.UrlEncode(q)}&s={ System.Web.HttpUtility.UrlEncode(s)}";

            // log
            _logger.LogInformation(parameter);

            // request
            var r = await DoHttpClientAsync(url, param, "TransactionDetails");
            var result = JsonConvert.DeserializeObject<CurrencyStats_SimplePlay>(r);
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
    }
}
