using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;

namespace ThirdPartySeamlessWallet.Service.DAL;

internal class DetailedRequestBody
{
    public string Seq { get; set; }
    public DateTime TimeStamp { get; set; }

    public PayloadBody Payload { get; set; }
}

internal class PayloadBody
{
    public string OperatorCustId { get; set; }
    public string WalletSideToken { get; set; }
    public string GameType { get; set; }
    public string GameId { get; set; }
    public object Additional { get; set; }
}

public class PayloadLogConfig
{
    public bool Enabled { get; set; } = false;
    public HashSet<string> ExcludedActions { get; set; } = new HashSet<string>();
}

public class PayloadLogMeta
{
    public string Action { get; set; }
    public string GameType { get; set; }
}

public class PayloadLogService
{
    private const string InsertDetailedPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins_v2";

    private readonly PartitionService _partitionService;
    private readonly string _environmentName;

    private readonly PayloadLogConfig _config;

    public PayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<ConfigBLL>().GetEnvironmentName() ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _config = provider.GetService<IOptions<PayloadLogConfig>>()?.Value;
    }

    public virtual void LogDetailedPayload(string actionName, string siteName, string requestContent, object responseContent, double elapsed)
    {
        if (!_config.Enabled) return;

        if (_config.ExcludedActions.Contains(actionName)) return;

        try
        {
            var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(requestContent) ?? new DetailedRequestBody();
            var gameType = getReq.Payload?.GameType ?? "Unknown";
            var responseText = responseContent as string ?? JsonConvert.SerializeObject(responseContent, Formatting.Indented);

            var logParams = new DynamicParameters(new
            {
                getReq.Seq,
                getReq.TimeStamp,
                Payload = requestContent,
                Action = actionName,
                GameType = gameType,
                Environment = $"{_environmentName}_{siteName}",
                Response = responseText,
                Elapsed = elapsed,
            });

            _partitionService.Execute(getReq.TimeStamp, InsertDetailedPayloadSpName, logParams);
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }
}