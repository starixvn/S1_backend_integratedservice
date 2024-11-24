using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class ApiEdayBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly ILogger<ApiEdayBLL> _logger;
    private const string ContentType = "application/json";
    
    public ApiEdayBLL(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<ApiEdayBLL>>();
        _httpHelper = provider.GetRequiredService<HttpHelper>();
    }
    
    public async Task<WagerEdayModel> GetBetListByTime(string startTime, string endTime,EdayConfig siteConfig, int? page = 0)
    {
        var path = "/apilot/GetLottoTicket";
        var parameters = new Dictionary<string, object>()
        { 
            {"startTime" , startTime},
            {"endTime" , endTime},
            {"pageSize" , 1000},
            {"pageIndex" , page},
            {"requestTimeType" , 0}
        };
        
        var sParam = JsonConvert.SerializeObject(parameters);
        
        var header = GetAuthData( path, sParam ,siteConfig);
        var r = await HttpPostClientAsync($"{siteConfig.ApiUrl}{path}", sParam, ContentType, header);
        
        var result = JsonConvert.DeserializeObject<WagerEdayModel>(r);
        return result;
    }

    public async Task<EdayBillDetailModel> GetBillDetail(string billId,EdayConfig siteConfig)
    {
        var path = "/apilot/GetBillDetail";
        var parameters = new Dictionary<string, object>()
        { 
            {"billId",billId}
        };
        
        var sParam = JsonConvert.SerializeObject(parameters);
        
        var header = GetAuthData( path, sParam ,siteConfig);
        var r = await HttpPostClientAsync($"{siteConfig.ApiUrl}{path}", sParam, ContentType, header);
        
        var result = JsonConvert.DeserializeObject<EdayBillDetailModel>(r);
        return result;
        
    }

    private async Task<string> HttpPostClientAsync(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
    {
        var parametersLog = parameters;
        if (headers != null)
        {
            parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
        }
        _logger.LogInformation($"url:{url} parametersLog:{parametersLog}");

        var client = _httpHelper.GetClient();
        try
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                Content = new StringContent(parameters),
                Method = HttpMethod.Post,
                RequestUri = new Uri(url)
            };
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);
            if (headers != null)
            {
                foreach (var (key, value) in headers)
                {
                    if (string.Compare(key, "Accept", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(value));
                    }
                    else
                    {
                        httpRequestMessage.Headers.TryAddWithoutValidation(key, value);
                    }
                }
            }

            var response = await client.SendAsync(httpRequestMessage);
            response.EnsureSuccessStatusCode();

            var sb = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"url:{url} | parametersLog:{parametersLog} | Response:{sb}");
            return sb;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"url:{url} parametersLog:{parametersLog} ex:{e.Message}");
            return null;
        }
    }
    
    private Dictionary<string, string> GetAuthData(string path, string parameters,EdayConfig siteConfig)
    {
        var md5 = Base64Md5(parameters);
        var contentType = "application/json";
        var signString = $"POST\n{md5}\n{contentType}\n{path}";
        var signature = GetSign(signString, siteConfig.ApiKey);
        return new Dictionary<string, string>
        {
            { "Accept", "application/json" },
            { "Authorization", $"ED {siteConfig.Operator}:{signature}" },
            { "Content-MD5", md5 }
        };
    }
    
    private string GetSign(string encryptText, string encryptKey)
    {
        using var hmacsha1 = new HMACSHA1(Encoding.UTF8.GetBytes(encryptKey));
        var hash = hmacsha1.ComputeHash(Encoding.UTF8.GetBytes(encryptText));
            
        return Convert.ToBase64String(hash);
    }
    
    private string Base64Md5(string data)
    {
        return Convert.ToBase64String(Md5(Encoding.ASCII.GetBytes(data)));
    }
    
    private byte[] Md5(byte[] data)
    {
        using var md5Crp = new MD5CryptoServiceProvider();
        return md5Crp.ComputeHash(data);
    }
}   