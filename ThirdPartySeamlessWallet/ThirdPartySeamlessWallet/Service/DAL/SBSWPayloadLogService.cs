using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;

namespace ThirdPartySeamlessWallet.Service.DAL;

public class SBSWPayloadLogService
{
    private const string InsertDetailedPayloadSpName = "Service_Bettrans_SBSW_Payload_Ins_v2";

    private readonly PartitionService _partitionService;
    private readonly string _environmentName;

    private readonly PayloadLogConfig _config;

    private readonly ILogger<SBSWPayloadLogService> _logger;

    public SBSWPayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<ConfigBLL>()?.GetEnvironmentName() ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _config = provider.GetService<IOptions<PayloadLogConfig>>()?.Value;
        _logger = provider.GetRequiredService<ILogger<SBSWPayloadLogService>>();
    }

    public virtual void LogDetailedPayload(string actionName, string requestContent, object responseContent, double elapsed)
    {
        if (!_config.Enabled) return;

        if (_config.ExcludedActions.Contains(actionName)) return;

        try
        {
            var response = JsonConvert.SerializeObject(responseContent, Formatting.Indented);
            var logParams = new DynamicParameters(new
            {
                Seq = Guid.NewGuid().ToString(),
                Payload = requestContent,
                TimeStamp = DateTime.UtcNow,
                Action = actionName,
                Environment = _environmentName,
                GameType = "SBSW",
                Response = response,
                Elapsed = elapsed
            });

            _partitionService.Execute(
                DateTime.UtcNow,
                InsertDetailedPayloadSpName,
                logParams
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}