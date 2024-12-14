using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;

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
        _environmentName = provider.GetRequiredService<ConfigBLL>()?.GetEnvironmentName() ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _config = provider.GetService<IOptions<PayloadLogConfig>>().Value;
        _logger = provider.GetRequiredService<ILogger<SBSWPayloadLogService>>();
    }

    public virtual void LogDetailedPayload(string seq, string content, PayloadLogMeta meta = null)
    {
        if (!_config.Enabled) return;

        var action = meta?.Action
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";
        if (_config.ExcludedActions.Contains(action)) return;

        try
        {
            var environment = _environmentName;

            var logParams = new DynamicParameters(new
            {
                Seq = seq,
                Payload = content,
                TimeStamp = DateTime.UtcNow,
                Action = action,
                Environment = environment,
                GameType = "Unknown"
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

    public virtual void UpdateDetailedPayLoadWithResponse(string seq, object responseContent, double elapsed, PayloadLogMeta meta = null)
    {
        if (!_config.Enabled)
        {
            return;
        }

        var action = meta?.Action
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";
        if (_config.ExcludedActions.Contains(action))
        {
            return;
        }

        var response = JsonConvert.SerializeObject(responseContent, Formatting.Indented);
        var logParams = new DynamicParameters(new
        {
            Seq = seq,
            Response = response,
            Elapsed = elapsed
        });

        try
        {
            _partitionService.Execute(
                DateTime.UtcNow,
                UpdateDetailedResponseSpName,
                logParams
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
    }
}