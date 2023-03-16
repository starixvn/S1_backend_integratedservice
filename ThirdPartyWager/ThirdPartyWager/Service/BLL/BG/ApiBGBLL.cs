using Newtonsoft.Json;
using System.Text;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL;

public class ApiBGBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly string _tpName = Tp.BG.ToString();

    public ApiBGBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _common = provider.GetRequiredService<Common>();
        _log2File = provider.GetRequiredService<Log2File>();
    }

    public async Task<WagerBG_OrderQueryResponseModel> OrderQuery(string apiUrl, string sn, string secrectKey, string startTime, string endTime, int pageIndex, int pageSize)
    {
        // param
        var guid = Guid.NewGuid().ToString();

        var parameters = JsonConvert.SerializeObject(new
        {
            id = guid,
            method = "open.order.query",
            @params = new
            {
                random = guid,
                sign = _common.ToMD5(guid + sn + secrectKey),
                sn = sn,
                startTime = startTime,
                endTime = endTime,
                pageIndex = pageIndex,
                pageSize = pageSize
            },
            jsonrpc = "2.0"
        });

        // request
        var response = await DoHttpClientAsync(apiUrl, parameters, "application/json");
        var result = JsonConvert.DeserializeObject<WagerBG_OrderQueryResponseModel>(response);
        return result;
    }

    private async Task<string> DoHttpClientAsync(string url, string param, string contentType)
    {
        _log2File.InsertRequestLog(_tpName, url, param, "1");
        var httpContent = new StringContent(param, Encoding.UTF8, contentType);
        var response = await _httpHelper.GetClient().PostAsync(url, httpContent);
        var result = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
            _log2File.InsertResponseLog(_tpName, url, param, result);
            return result;
        }

        return result;
    }
}
