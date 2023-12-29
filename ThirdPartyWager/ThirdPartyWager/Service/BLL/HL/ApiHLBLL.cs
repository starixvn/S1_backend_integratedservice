using System.Text;
using System.Xml;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;
    
namespace ThirdPartyWager.Service.BLL;

public class ApiHLBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly ILogger<ApiHLBLL> _logger;
    private readonly string _tpName = Tp.HaoLong.ToString();
    private readonly IConfiguration _configuration;
    private readonly HLConfig _siteHLConfig;
    
    public ApiHLBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _log2File = provider.GetRequiredService<Log2File>();
        _logger = provider.GetRequiredService<ILogger<ApiHLBLL>>();
        _configuration = provider.GetRequiredService<IConfiguration>();
        _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(Tp.HaoLong.ToString());
    }

    public async Task<HLResponse> GetBetListByTime(string startTime, string endTime, string guid, int page = 0)
    {
        var param = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                    $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&" +
                    $"dateFrom={DateTimeFormat(startTime)}&dateTo={DateTimeFormat(endTime)}";
                    //保留$"&page={page}";

        var r = await DoHttpClientAsync($"{_siteHLConfig.ApiUrl}betlistPage.aspx?", param, "betData");
        var result = JsonConvert.DeserializeObject<HLResponse>(r);
        
        return result;
    }

    public async Task<HLDrawResultResponse> GetDrawResult(DateTime drawTime)
    {
        var param = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                    $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&" +
                    $"drawDate={drawTime.Date:yyyy-MM-dd}";
                    //$"drawDate={time}";
        
        var r = await DoHttpClientAsync($"{_siteHLConfig.ApiUrl}result.aspx?", param, "drawResult");
        var result = JsonConvert.DeserializeObject<HLDrawResultResponse>(r);

        return result;
    }

    public async Task<HLResponse> GetWinningNumberByPlayer(string tpAccountName,DateTime drawTime)
    {
        var param = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                    $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&loginID={tpAccountName}&" +
                    $"dateFrom={drawTime.Date:yyyy-MM-dd}&dateTo={drawTime.Date:yyyy-MM-dd}";
        
        var r = await DoHttpClientAsync($"{_siteHLConfig.ApiUrl}winNumber.aspx?", param, "betData");
        var result = JsonConvert.DeserializeObject<HLResponse>(r);
        
        return result;
    }
    
    public async Task<HLResponse> GetWinningNumberByDay(DateTime drawTime)
    {
        var param = $"apiUser={_siteHLConfig.ApiUser}&apiPass={_siteHLConfig.ApiPass}&" +
                    $"user={_siteHLConfig.AgentUser}&pass={_siteHLConfig.AgentPass}&" +
                    $"dateFrom={drawTime.Date:yyyy-MM-dd}&dateTo={drawTime.Date:yyyy-MM-dd}";
        
        var r = await DoHttpClientAsync($"{_siteHLConfig.ApiUrl}winNumber.aspx?", param, "betData");
        var result = JsonConvert.DeserializeObject<HLResponse>(r);
        
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

    private string DateTimeFormat(string dateTime)
    { 
        return DateTime.Parse(dateTime).ToString("yyyy-MM-dd");
    }
}   