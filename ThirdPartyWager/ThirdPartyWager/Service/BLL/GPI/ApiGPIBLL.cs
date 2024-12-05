using System.Text;
using System.Web;
using System.Xml;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiGPIBLL
    {
        private readonly HttpHelper _httpHelper;
        private readonly Common _common;
        private readonly Log2File _log2File;
        private readonly string _tpName = Tp.GPI.ToString();
        private readonly ProxyConfig _tokyoProxy;

        private readonly Dictionary<int, string> _productList = new Dictionary<int, string>() {
            {(int)GameType.GPI_RNG, "slots"},
            {(int)GameType.GPI_Keno, "lottery"},//使用一款代表取lottery資料
        };

        public ApiGPIBLL(IServiceProvider provider, IConfiguration configuration)
        {
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _common = provider.GetRequiredService<Common>();
            _log2File = provider.GetRequiredService<Log2File>();
            _tokyoProxy = provider.GetRequiredService<ConfigBLL>().GetProxyConfig();
        }

        public async Task<WagerGPIModel> GetBetListByTime(GPIConfig setting, int gameType, int page, string startTime, string endTime)
        {
            var apiUrl = setting.WagerUrl;
            var merchId = setting.OperatorId;
            var merchPwd = setting.WagerMerchPwd;
            var pageSize = 500;
            var product = _productList[gameType];

            // param
            var uri = "api/gateway/betDetail.html";
            var url = $"{apiUrl}{uri}";
            var param = new
            {
                merch_id = merchId,
                merch_pwd = merchPwd,
                date_from = HttpUtility.UrlEncode(startTime),
                date_to = HttpUtility.UrlEncode(endTime),
                product = product,
                page_num = page,
                page_size = pageSize,
            };

            // request
            var r = await DoHttpClientAsync(url, param, "item");
            var result = JsonConvert.DeserializeObject<WagerGPIModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param, string tagName)
        {
            var dicP = _common.FromJson<Dictionary<string, string>>(_common.ToJson(param));
            string sParam = string.Join("&", dicP.Where(x => x.Value != null).Select(kp => $"{kp.Key}={kp.Value}").ToArray());

            _log2File.InsertRequestLog(_tpName, url, sParam, "1");

            url = $"{url}?{sParam}";
            HttpResponseMessage response;
            string result;

            //if (!_env.IsProduction())
            //{
            using var client = _httpHelper.GetClientWithHandler(_httpHelper.CreateProxyConfigHandler(_tokyoProxy));
            response = await client.GetAsync(url);
            using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            result = await sr.ReadToEndAsync();
            //}
            //else
            //{
            //    var request = new HttpRequestMessage(HttpMethod.Get, url);
            //    response = await _httpHelper.GetClient().SendAsync(request);
            //    result = response.Content.ReadAsStringAsync().Result;
            //}

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
                _log2File.InsertResponseLog(_tpName, url, sParam, result);
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

            if (result.Contains("Too many requests"))
            {
                return "{\"resp\":{\"error_code\":-1,\"error_msg\":\"Too many requests\"}}";
            }
            return JsonConvert.SerializeXmlNode(xmlDoc);
        }

        protected GameType GetGameType(string gameId)
        {
            var gameType = GameType.GPI_RNG;
            if (string.Equals(gameId, "keno", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.GPI_Keno;
            }
            else if (string.Equals(gameId, "sode", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.GPI_Sode;
            }
            else if (string.Equals(gameId, "thailottery", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.GPI_Thai_Lottery;
            }
            else if (string.Equals(gameId, "fast3", StringComparison.OrdinalIgnoreCase))
            {
                gameType = GameType.GPI_Fast3;
            }
            return gameType;
        }
    }
}