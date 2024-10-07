using System.Security.Policy;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL;

public class ApiPG2BLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly ILogger<ApiPG2BLL> _logger;
    private readonly string _tpName = Tp.PG2.ToString();
    private readonly WagerPG2Service _wagerPG2Service;

    public ApiPG2BLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _common = provider.GetRequiredService<Common>();
        _log2File = provider.GetRequiredService<Log2File>();
        _logger = provider.GetRequiredService<ILogger<ApiPG2BLL>>();
        _wagerPG2Service = provider.GetRequiredService<WagerPG2Service>();
    }

    public async Task<CurrencyStats_PG2> GetBetSummaryByDate(dynamic setting, string currency, int gameType, string reportTimeStartTime,
        string reportTimeEndTime, int perPage, int page)
    {
        var apiUrl = setting.ApiUrl;
        var key = setting.CurrencyData[currency].ApiKey;
        var agent = setting.CurrencyData[currency].Agent;

        var url = $"{apiUrl}/seamless/report/date";
        var userList = _wagerPG2Service.GetUserListByDate(reportTimeStartTime);

        var param = new
        {
            agent = agent,
            startDate = reportTimeStartTime,
            endDate = reportTimeEndTime,
            userList = userList,
            page = page,
            limit = perPage
        };

        var sParam = JsonConvert.SerializeObject(param);

        // request
        var r = await DoHttpClientAsync(url, sParam, key, "x-amb-signature", HttpMethod.Post);
        var result = JsonConvert.DeserializeObject<CurrencyStats_PG2>(r);
        return result;
    }

    private async Task<string> DoHttpClientAsync(string url, string param, string secretKey, string headerKey, HttpMethod method)
    {
        HttpRequestMessage request;
        request = new HttpRequestMessage(method, url);
        request.Content = new StringContent(param, Encoding.UTF8, "application/json");
        request.Headers.Add(headerKey, _common.HMACSHA512(param, secretKey));
        _log2File.InsertRequestLog(_tpName, url, $"{param}\r\n{JsonConvert.SerializeObject(request.Headers)}", "1");

        var response = await _httpHelper.GetClient().SendAsync(request);
        var result = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
            _log2File.InsertResponseLog(_tpName, url, param, result);
        }
        else
        {
            var dict = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(result);
            if (dict.ContainsKey("status") && dict["status"].ToString() == "429")
            {
                _log2File.InsertResponseLog(_tpName, url, param, result, false);
                var temp = new CurrencyStats_PG2();
                temp.status = new Status_PG2
                {
                    code = 429,
                    message = dict["message"]
                };
                _log2File.InsertResponseLog(_tpName, url, param, JsonConvert.SerializeObject(temp), false);
                return JsonConvert.SerializeObject(temp);
            }
        }

        return result;
    }
}