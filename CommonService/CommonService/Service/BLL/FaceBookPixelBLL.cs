using System.Net;
using CommonService.Service.DAL;
using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;

namespace CommonService.Service.BLL
{
    public class FaceBookPixelBLL
    {
        private readonly ILogger<FaceBookPixelBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly FaceBookPixelConfig _faceBookPixelConfig;
        private readonly CurrencyService _currencyService;
        private readonly Tools _tools;
        
        
        public FaceBookPixelBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FaceBookPixelBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _faceBookPixelConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<FaceBookPixelConfig>("FaceBookPixelConfig");
            _currencyService = provider.GetRequiredService<CurrencyService>();
            _tools = provider.GetRequiredService<Tools>();
        }

        public async Task<ApiResult> SendAsync(PixelRequest request)
        {
            var apiResult = new ApiResult();
            if (_faceBookPixelConfig.Status == 0)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = $"not production environment ignore FaceBookPixelSend , always return success";
                return apiResult;
            }
            
            try
            {
                var url = string.Format(_faceBookPixelConfig.ApiUrl, _faceBookPixelConfig.PixelId, _faceBookPixelConfig.Token);

                var eventData = new EventData
                {
                    data = new List<PixelEvent>()
                };

                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var custSha = _tools.SHA256(request.CustId.ToString());
                var isoCode = _currencyService.GetCurrency().FirstOrDefault(x => x.CurrencyId == request.CurrencyId)?.IsoCode;
                
                foreach (var pixelEvent in request.PixelEvent)
                {
                    var data = new PixelEvent
                    {
                        event_name =  pixelEvent.ToString(),
                        event_time = now,
                        user_data = new UserData
                        {
                            external_id = new List<string> {custSha}
                        },
                        custom_data = new CustomData
                        {
                            currency = isoCode,
                            value = request.Value
                        },
                        event_source_url = request.Url,
                        action_source =  "website"
                    };
                    eventData.data.Add(data);
                }
                
                var parameters = new Dictionary<string, string>
                {
                    { "data", JsonConvert.SerializeObject(eventData.data) },
                    {"access_token",_faceBookPixelConfig.Token }
                }; 
                
                var response = await _httpHelper.HttpFromPostAsync(url, null, parameters, "application/x-www-form-urlencoded");
                apiResult.ErrorCode = response.StatusCode == HttpStatusCode.OK ? ErrorCode.Success : ErrorCode.Fail;
                
                return apiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Send FaceBookPixel Error , Message :{ex.Message}");
            }

            return apiResult;
        }
    }
}
