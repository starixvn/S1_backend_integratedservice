using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;

namespace ThirdPartySeamlessWallet.Service.DAL;

public class SBSWPayloadLogService
{
    private const string InsertDetailedPayloadSpName = "Service_Bettrans_SBSW_Payload_Ins_v2";

    private const string UpdateDetailedResponseSpName = "Service_Bettrans_SBSW_Payload_Set_v2";

    private readonly PartitionService _partitionService;
    private readonly string _environmentName;

    private readonly PayloadLogConfig _config;

    private readonly ILogger<SBSWPayloadLogService> _logger;

    public SBSWPayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<IWebHostEnvironment>()?.EnvironmentName ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _config = provider.GetService<IOptions<PayloadLogConfig>>().Value;
        _logger = provider.GetRequiredService<ILogger<SBSWPayloadLogService>>();
    }

    public virtual void LogDetailedPayload(string content, PayloadLogMeta meta = null) {
        if (!_config.Enabled) return;
        
        var action = meta?.Action 
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";
        if (_config.ExcludedActions.Contains(action)) return;

        try {
            var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(content);
            var payload = getReq?.Payload?.ToString() ?? "{}";
            
            var gameType = meta?.GameType
                ?? JsonConvert.DeserializeObject<DetailedRequestBodyPayload>(payload)?.GameType
                ?? "Unknown"; 

            var environment = _environmentName;

            var logParams = new DynamicParameters(new {
                getReq.Seq,
                getReq.TimeStamp,
                Payload = payload,
                Action = action,
                GameType = gameType,
                Environment = environment,
            });

            _partitionService.Execute(
                DateTime.Now,
                InsertDetailedPayloadSpName,
                logParams
            );

        } catch (Exception ex) {
            SentrySdk.CaptureException(ex);
        }

    }

    public virtual void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent, PayloadLogMeta meta = null) {
        if (!_config.Enabled) return;

        var action = meta?.Action 
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";
        if (_config.ExcludedActions.Contains(action)) return;
        
        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(requestContent);
        
        if (responseContent is string) {
            responseContent = JsonConvert.DeserializeObject(responseContent.ToString() ?? string.Empty);
        }
        var response = JsonConvert.SerializeObject(responseContent,Formatting.Indented);
        var logParams = new DynamicParameters(new {
            getReq.Seq,
            Response = response
        });

        try {
            _partitionService.Execute(
                DateTime.Now, 
                UpdateDetailedResponseSpName,
                logParams
            );

        } catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}
