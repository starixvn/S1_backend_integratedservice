using System.Text;
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

    public async Task<Wager93ConnectModel> GetBetListByTime(string startTime, string endTime, string guid,(string apiUrl, string clientKey, string secretKey) apiParams, int? page = 0)
    {
        var token = await GetToken(apiParams.apiUrl, apiParams.clientKey, apiParams.secretKey);
        var param = new Dictionary<string, object>()
        {
            { "token", token.token },
            { "date_from", startTime},
            { "date_to", endTime},
            { "page", page}
        };

        var sParam = JsonConvert.SerializeObject(param);
        
        var r = await DoHttpClientAsync(apiParams.apiUrl + "player/?a=hourly", sParam);
        var result = JsonConvert.DeserializeObject<Wager93ConnectModel>(r);
        return result;
    }

    public async Task<Wager93ConnectModel> GetBetListByDay(string startTime, string guid,(string apiUrl, string clientKey, string secretKey) apiParams, int? page = 0)
    {
        var token = await GetToken(apiParams.apiUrl, apiParams.clientKey, apiParams.secretKey);
        var param = new Dictionary<string, object>()
        {
            { "token", token.token },
            { "date", startTime},
            { "page", page}
        };

        var sParam = JsonConvert.SerializeObject(param);
        
        var r = await DoHttpClientAsync(apiParams.apiUrl + "player/?a=daily", sParam);
        var result = JsonConvert.DeserializeObject<Wager93ConnectModel>(r);
        return result;
    }

    private async Task<string> DoHttpClientAsync(string url, string json,int timeOutTime = 0)
    {
        try
        {
            //會http status code回傳非200，
            //但同時又會回傳error message的response，可使用。
            using (var client = new HttpClient())
            {
                if (timeOutTime > 0)
                {
                    client.Timeout = TimeSpan.FromSeconds(timeOutTime);
                }
                    
                using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                    {
                        request.Content = content;
                        using (var response = client.Send(request))
                        {
                            var responseContent = response.Content.ReadAsStringAsync().Result;
                            return responseContent;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _log2File.InsertResponseLog(_tpName, url, json, ex.Message);
            return ex.Message;
        }
    }

    private async Task<GetTokenResponse> GetToken(string apiUrl ,string ClientKey, string SecretKey)
    {
        var getTokenUrl = apiUrl + "token/";
        var request = new Dictionary<string, string>
        {
            {"client_key",ClientKey},
            {"client_secret", SecretKey}
        };

        var sParam = JsonConvert.SerializeObject(request);

        var json = await DoHttpClientAsync(getTokenUrl, sParam);
        var result = JsonConvert.DeserializeObject<GetTokenResponse>(json);

        return result;
    }
}   