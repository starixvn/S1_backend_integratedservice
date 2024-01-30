using System.Text;
using System.Xml;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
    
namespace ThirdPartyWager.Service.BLL;

public class ApiNTConnectBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly ILogger<ApiHLBLL> _logger;
    private readonly string _tpName = Tp.NTConnect.ToString();
    private readonly IConfiguration _configuration;
    private readonly Dictionary<int, NTConnectConfig> _siteConfigDic;
    
    public ApiNTConnectBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _log2File = provider.GetRequiredService<Log2File>();
        _logger = provider.GetRequiredService<ILogger<ApiHLBLL>>();
        _configuration = provider.GetRequiredService<IConfiguration>();
        _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<NTConnectConfig>(Tp.NTConnect.ToString());
    }

    public async Task<HLResponse> GetBetListByTime(string startTime, string endTime, string guid, int page = 0)
    {
        return null;
    }

    public async Task<HLDrawResultResponse> GetDrawResult(DateTime drawTime)
    {
        return null;
    }

    public async Task<HLResponse> GetWinningNumberByPlayer(string tpAccountName,DateTime drawTime)
    {
        return null;
    }
    
    public async Task<HLResponse> GetWinningNumberByDay(DateTime drawTime)
    {
        return null;
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
        xmlDoc.LoadXml(result); ;
        
        
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

    private GetTokenResponse GetToken(string apiUrl ,string ClientKey, string SecretKey)
    {
        var getTokenUrl = apiUrl + "token/";
        var request = new Dictionary<string, string>
        {
            {"client_key",ClientKey},
            {"client_secret", SecretKey}
        };

        var sParam = JsonConvert.SerializeObject(request);

        // var json = HttpClientPost(getTokenUrl, sParam, ContentType);
        // var result = FromJson<GetTokenResponse>(json);

        return null;
    }
}   