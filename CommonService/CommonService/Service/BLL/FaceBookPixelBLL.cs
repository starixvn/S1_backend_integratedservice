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
        private readonly Dictionary<int, FaceBookPixelConfig> _faceBookPixelConfig;
        private readonly Tools _tools;
        private readonly FaceBookPixelService _faceBookPixelService;
        
       
        public FaceBookPixelBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<FaceBookPixelBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _faceBookPixelConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<FaceBookPixelConfig>("FaceBookPixelConfig");
            _tools = provider.GetRequiredService<Tools>();
            _faceBookPixelService = provider.GetRequiredService<FaceBookPixelService>();
        }

        public async Task<ApiResult> SendAsync(PixelRequest request)
        {
            var apiResult = new ApiResult();
          
            try
            {
                var config = _faceBookPixelConfig[request.SiteId];

                foreach (var keyData in config.Key)
                {
                    if (keyData.Status == 0)
                    {
                        continue;
                    }
                    
                    var url = string.Format(config.ApiUrl, keyData.PixelId, keyData.Token);

                    var eventData = new EventData
                    {
                        data = new List<PixelEvent>()
                    };
                    
                    var custFaceBookData = _faceBookPixelService.GetCustFaceBookData(request.CustId , request.TransId);
                    if (custFaceBookData is null)
                    {
                        apiResult.ErrorCode =  ErrorCode.Fail;
                        apiResult.Message = "Can't get cust facebook data";
                        return apiResult;
                    }
                    
                    #region Deal Parameter
                    
                    var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var email = custFaceBookData.Email is null ? "" : _tools.SHA256(custFaceBookData.Email);
                    var custSha = _tools.SHA256(request.CustId.ToString());

                    // 將 IPv4 地址轉換為 IPv6 映射地址
                    var ipv4 = IPAddress.Parse(custFaceBookData.Ip);
                    var ipv6 = ipv4.MapToIPv6().ToString();
                    
                    #endregion
                    
                    foreach (var pixelEvent in request.PixelEvent)
                    {
                        var data = new PixelEvent
                        {
                            event_name =  pixelEvent.ToString(),
                            event_time = now,
                            user_data = new UserData
                            {
                                fbp = custFaceBookData.Fbp,
                                fbc = custFaceBookData.Fbc,
                                em = email,
                                client_ip_address = ipv6,
                                client_user_agent = custFaceBookData.UserAgent,
                                external_id = new List<string> {custSha}
                            },
                            event_source_url = request.Url,
                            action_source =  "website"
                        };
                        
                        switch (pixelEvent)
                        {
                            case FaceBookPixel.registerSubmit:
                            case FaceBookPixel.CompleteRegistration:
                                data.event_id = custFaceBookData.UserName;
                                break;
                            case FaceBookPixel.depositSubmit:
                            case FaceBookPixel.firstDeposit:
                            case FaceBookPixel.withdraw:
                            case FaceBookPixel.firstDepositArrival:
                            case FaceBookPixel.Purchase:
                            case FaceBookPixel.redeposit:
                            case FaceBookPixel.StartTrial:
                                data.custom_data = new CustomData
                                {
                                    currency = custFaceBookData.IsoCode,
                                    value = request.Value
                                };
                                data.event_id = request.TransId;
                                break;
                        }
                        
                        eventData.data.Add(data);
                    }
                    
                    var settings = new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore // 忽略空值的屬性
                    };

                    var jsonPayload = JsonConvert.SerializeObject(eventData, settings);
                    var responseMessage = await _httpHelper.HttpPost1Async(url, null, jsonPayload, "application/json");
                    
                    if (responseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        _faceBookPixelService.DelCustFaceBookData(request.CustId , request.TransId);
                    }
                }
                
                apiResult.ErrorCode =  ErrorCode.Success;
                
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
