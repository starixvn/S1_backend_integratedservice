using System.Text;
using Newtonsoft.Json;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL.IM;

public class ApiIMBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly ILogger<ApiIMBLL> _logger;
    private readonly Log2File _log2File;
    private readonly string _tpName = Tp.IM.ToString();
    public ApiIMBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _logger = provider.GetRequiredService<ILogger<ApiIMBLL>>();
        _log2File = provider.GetRequiredService<Log2File>();
    }
    
    public async Task<IMWagerModel> GetBetListByTime(GetBetListByTimeParam param)
    {
        var wagerUrl = param.Setting["WagerUrl"];
        var merchantCode = param.Setting["Key"];
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
       
        var resp = await DoHttpClientAsync($"{wagerUrl}{uri}", req);
        var result = JsonConvert.DeserializeObject<IMWagerModel>(resp);
        
        return result;
    }

    #region Private

    private async Task<string> DoHttpClientAsync(string url, object param)
    {
        var strReq = JsonConvert.SerializeObject(param);
        var result = "-";
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(strReq, Encoding.UTF8, "application/json");

            var response = await _httpHelper.GetClient().SendAsync(request);
        
            using var sr = new StreamReader(await response.Content.ReadAsStreamAsync(), Encoding.UTF8);
            result = await sr.ReadToEndAsync();

            if (!response.IsSuccessStatusCode)
            {
                _log2File.InsertHttpErrorLog(_tpName, response.StatusCode, response.ReasonPhrase);
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
            _log2File.InsertRequestLog(_tpName, url, strReq, result);
        }
    }

    #endregion
}