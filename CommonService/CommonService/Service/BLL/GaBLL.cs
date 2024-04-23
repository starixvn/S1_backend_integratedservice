using CommonService.Service.Library;
using CommonService.Service.Model;
using CommonService.Service.Model.enums;
using Newtonsoft.Json;
using System.Net;

namespace CommonService.Service.BLL
{
    public class GaBLL
    {
        private readonly ILogger<GaBLL> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly Dictionary<int, GAConfig> _gaConfig;
        private readonly bool _IsProduction;
        private const string EventName = "purchase";

        public GaBLL(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<GaBLL>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            var env = provider.GetRequiredService<IWebHostEnvironment>();
            _IsProduction = env.IsProduction();
            _gaConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<GAConfig>("GAConfig");
        }

        public async Task<ApiResult> GaEventSendAsync(PurchaseRequest request)
        {
            var apiResult = new ApiResult();
            if (!_IsProduction)
            {
                apiResult.ErrorCode = ErrorCode.Success;
                apiResult.Message = $"not production environment ignore purchaseEventSend always return success";
            }

            try
            {
                var config = GetGaConfig(request.SiteId);

                // 沒有設定的Site，直接回傳成功結果
                if (config == null)
                {
                    apiResult.ErrorCode = ErrorCode.Success;
                    apiResult.Message = $"siteId:{request.SiteId} not GAConfig";
                    return apiResult;
                }

                // Get PostData
                var purchasePostData = GetPurchaseEventPostData(request);
                var customerPostData = GetCustomizedEventPostData(request);
                var url = $"{config?.ApiUrl}?measurement_id={config?.MeasurementId}&api_secret={config?.SecretKey}";

                var purchaseTask = _httpHelper.HttpPost1Async(url, null, JsonConvert.SerializeObject(purchasePostData), "application/json");
                var customerTask = _httpHelper.HttpPost1Async(url, null, JsonConvert.SerializeObject(customerPostData), "application/json");
                await Task.WhenAll(customerTask);

                var results = new[] { purchaseTask.Result, customerTask.Result };
                apiResult.ErrorCode =
                    results.All(result => result.StatusCode == HttpStatusCode.NoContent)
                        ? ErrorCode.Success
                        : ErrorCode.Fail;

                return apiResult;
            }
            catch (Exception ex)
            {
                _logger.LogError($"GaEventSendAsync Error CustId:{request.CustId}，purchasePostData:{JsonConvert.SerializeObject(GetPurchaseEventPostData(request))} " +
                                 $"customerPostData:{JsonConvert.SerializeObject(GetCustomizedEventPostData(request))}  ExceptionMessage: {ex.Message}");
            }

            return apiResult;
        }

        #region private method

        /// <summary>
        /// GA 購買事件資料
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private PurchaseEventDto GetPurchaseEventPostData(PurchaseRequest request)
        {
            var postData = new PurchaseEventDto()
            {
                ClientId = request.CustId.ToString(),
                Events = new List<Event>()
                {
                    new()
                    {
                        Name = EventName,
                        Params = new Params()
                        {
                            TransactionId = request.TransId,
                            Currency = ConvertCurrencyName(request.CurrencyName),
                            Value = request.DepositAmount,
                            Items = new List<Item>()
                            {
                                new()
                                {
                                    ItemId = request.ItemId,
                                    ItemName = request.ItemName
                                }
                            }
                        }
                    }
                }
            };

            return postData;
        }

        /// <summary>
        /// GA 自訂事件資料
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// 
        private CustomizedEventDto GetCustomizedEventPostData(PurchaseRequest request)
        {
            var postData = new CustomizedEventDto()
            {
                ClientId = request.CustId.ToString(),
                Events = new List<CustomizedEvent>()
                {
                    new()
                    {
                        Name = request.EventName,
                        EventParams = request.ToDictionary()
                    }
                }
            };

            return postData;
        }

        private GAConfig GetGaConfig(int siteId)
        {
            return _gaConfig.TryGetValue(siteId, out var value)
                ? value
                : null;
        }

        /// <summary>
        /// 幣別轉匯，GA 購買事件幣別只支援 正規 ISO Code
        /// </summary>
        /// <param name="currencyName"></param>
        /// <returns></returns>
        private static string ConvertCurrencyName(string currencyName)
        {
            return currencyName == "KHUSD"
                ? "USD"
                : currencyName;
        }

        #endregion

    }
}
