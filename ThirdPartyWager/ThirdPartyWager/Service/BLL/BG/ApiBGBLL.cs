using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

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

    public async Task<WagerBG_ApiResponseModel<object>> OrderQuery(string apiUrl, string sn, string secretCode, string agnetLoginId, string startTime, string endTime, int pageIndex, int pageSize)
    {
        // param
        var guid = Guid.NewGuid().ToString();

        var parameters = JsonConvert.SerializeObject(new
        {
            id = guid,
            method = "open.order.agent.query",
            @params = new
            {
                random = guid,
                digest = _common.ToMD5(guid + sn + SHA1(secretCode)),
                sn = sn,
                agentLoginId = agnetLoginId,
                startTime = startTime,
                endTime = endTime,
                pageIndex = pageIndex,
                pageSize = pageSize
            },
            jsonrpc = "2.0"
        });

        // request
        var response = await DoHttpClientAsync(apiUrl, parameters, "application/json");
        var result = JsonConvert.DeserializeObject<WagerBG_ApiResponseModel<object>>(response);
        return result;
    }

    public async Task<WagerBG_ApiResponseModel<object>> OrderSum(string apiUrl,
        string sn, string secrectKey, string agentLoginId, string startTime, string endTime)
    {
        // param
        var guid = Guid.NewGuid().ToString();

        var parameters = JsonConvert.SerializeObject(new
        {
            id = guid,
            method = "open.user.order.sum",
            @params = new
            {
                random = guid,
                sign = _common.ToMD5(guid + sn + secrectKey),
                sn = sn,
                agentLoginId = agentLoginId,
                startTime = startTime,
                endTime = endTime
            },
            jsonrpc = "2.0"
        });

        // request
        var response = await DoHttpClientAsync(apiUrl, parameters, "application/json");
        var result = JsonConvert.DeserializeObject<WagerBG_ApiResponseModel<object>>(response);
        return result;
    }
    private string SHA1(string dataToSign)
    {
        SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
        byte[] source = Encoding.Default.GetBytes(dataToSign);//將字串轉為Byte[]
        byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
        string result = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
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
