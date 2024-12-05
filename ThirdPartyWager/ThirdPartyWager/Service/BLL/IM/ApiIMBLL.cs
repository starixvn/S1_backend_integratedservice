using System.Net;
using System.Text;

using Newtonsoft.Json;

using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL;

public class ApiIMBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly ILogger<ApiIMBLL> _logger;
    private readonly Log2File _log2File;
    private readonly IConfiguration _configuration;
    private readonly ProxyConfig? _tokyoProxy;

    public ApiIMBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _logger = provider.GetRequiredService<ILogger<ApiIMBLL>>();
        _log2File = provider.GetRequiredService<Log2File>();
        _configuration = provider.GetRequiredService<IConfiguration>();
        _tokyoProxy = provider.GetRequiredService<ConfigBLL>().GetProxyConfig();
    }

    public async Task<IMWagerModel<T>> GetBetListByTime<T>(GetBetListByTimeParam param, Tp tp)
    {
        var wagerUrl = param.Setting.WagerUrl;
        var merchantCode = param.Setting.Key;
        const string uri = "/Report/GetBetLog";
        const int pageSize = 1000; // MAX: 50000

        var req = new IMGetBetLogReq
        {
            MerchantCode = merchantCode,
            StartDate = param.StartTime,
            EndDate = param.EndTime,
            Page = param.Page,
            PageSize = pageSize,
            Currency = param.Currency,
            ProductWallet = (int)param.ProductWallet
        };

        var resp = await DoHttpClientAsync($"{wagerUrl}{uri}", req, tp);
        var result = JsonConvert.DeserializeObject<IMWagerModel<T>>(resp);

        return result;
    }

    /// <summary>
    /// For Sportsbook & ESports
    /// </summary>
    public async Task<IMWagerModel<T>> GetSportsBetListByTime<T>(GetBetListByTimeParam param, Tp tp)
    {
        var wagerUrl = param.Setting.WagerUrl;
        var merchantCode = param.Setting.Key;
        const string uri = "/Report/GetBetLog";

        var req = new IMGetSportsBetLogReq
        {
            MerchantCode = merchantCode,
            StartDate = param.StartTime,
            EndDate = param.EndTime,
            Page = param.Page,
            ProductWallet = (int)param.ProductWallet,
            DateFilterType = param.DateFilterType,
            Language = "EN",
            Product = param.Product
        };

        var resp = await DoHttpClientAsync($"{wagerUrl}{uri}", req, tp);
        var result = JsonConvert.DeserializeObject<IMWagerModel<T>>(resp);

        return result;
    }

    #region Private

    private async Task<string> DoHttpClientAsync(string url, object param, Tp tp)
    {
        var tpName = tp.ToString();
        var result = "-";
        try
        {
            using var client = _httpHelper.GetClientWithHandler(CreateHandler());
            var response = await client.PostAsJsonAsync(url, param);

            using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            result = await sr.ReadToEndAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(tpName, response.StatusCode, response.ReasonPhrase);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"POST {url} throw Exception");

            throw;
        }
        finally
        {
            _log2File.InsertRequestLog(tpName, url, JsonConvert.SerializeObject(param), result);
        }
    }

    private HttpClientHandler CreateHandler()
    {
        var handler = new HttpClientHandler();
        string proxyHost;
        int proxyPort;

        if (_tokyoProxy != null)
        {
            handler.UseProxy = true;
            proxyHost = _tokyoProxy.ProxyHost;
            proxyPort = _tokyoProxy.ProxyPort;
            handler.Proxy = new WebProxy(proxyHost, proxyPort);

            _logger.LogInformation($"use proxy, host:{proxyHost}, port:{proxyPort}");

            return handler;
        }

        proxyHost = _configuration.GetValue<string>("ProxyHost");
        proxyPort = _configuration.GetValue<int>("ProxyPort");

        if (!string.IsNullOrEmpty(proxyHost))
        {
            handler.UseProxy = true;
            handler.Proxy = new WebProxy(proxyHost, proxyPort);

            _logger.LogInformation($"use proxy, host:{proxyHost}, port:{proxyPort}");
        }

        return handler;
    }

    #endregion Private
}