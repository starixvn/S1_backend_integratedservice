using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class ApiYeebetBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly ILogger<ApiYeebetBLL> _logger;
    private readonly string _tpName = Tp.Yeebet.ToString();

    public ApiYeebetBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _common = provider.GetRequiredService<Common>();
        _log2File = provider.GetRequiredService<Log2File>();
        _logger = provider.GetRequiredService<ILogger<ApiYeebetBLL>>();
    }

    public async Task<WagerYeebet_GetbetsResponseModel> Getbets(string apiUrl, string appid, int size, string apiKey)
    {
        // param
        var url = $"{apiUrl}api/data/getbets/";
        var sign = _common.ToMD5($"appid={appid}&size={size}&key={apiKey}").ToLower();

        // request
        var r = await HttpGet(url, "getbets", $"appid={appid}&size={size}&sign={sign}");
        var result = JsonConvert.DeserializeObject<WagerYeebet_GetbetsResponseModel>(r);
        return result;
    }

    public async Task<WagerYeebet_RemovebetsResponseModel> Removebets(string apiUrl, string appid, string ids, string apiKey)
    {
        // param
        var url = $"{apiUrl}api/data/removebets/";
        var sign = _common.ToMD5($"appid={appid}&ids={ids}&key={apiKey}").ToLower();

        // request
        var r = await HttpGet(url, "removebets", $"appid={appid}&ids={ids}&sign={sign}");
        var result = JsonConvert.DeserializeObject<WagerYeebet_RemovebetsResponseModel>(r);
        return result;
    }

    protected async Task<string> HttpGet(string url,
       string recordEvent,
       string parameters,
       Dictionary<string, string> headers = null, string contentType = null)
    {
        try
        {
            _log2File.InsertRequestLog(_tpName, url, parameters, "1");
            var request = new HttpRequestMessage(HttpMethod.Get, $"{url}?{parameters}");

            if (!string.IsNullOrWhiteSpace(contentType))
                request.Content = new StringContent(parameters, Encoding.UTF8, contentType);
            if (headers != null)
            {
                foreach (var item in headers)
                    request.Headers.Add(item.Key, item.Value);
            }

            var client = _httpHelper.GetClient();
            var response = await client.SendAsync(request);

            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"{url}?{parameters} {recordEvent} error:{content}");
                return null;
            }

            return content;
        }
        catch (Exception e)
        {
            _logger.LogError(e, $"{url}?{parameters} {recordEvent} failed:{e.Message}");
            return null;
        }
    }
}
