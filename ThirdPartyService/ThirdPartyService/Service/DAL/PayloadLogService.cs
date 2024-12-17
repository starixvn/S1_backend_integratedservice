using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL;

public class PayloadLogService : IPayloadLogger
{
    private const string InsertDetailedPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins_v2";

    private readonly MySqlClient _mainDbClient;

    private readonly string _environmentName;

    private readonly PayloadLogConfig _config;

    public PayloadLogService(IServiceProvider serviceProvider)
    {
        var commonService = serviceProvider.GetRequiredService<Common>();
        _environmentName = commonService.GetEnvironmentName() ?? "Unspecified";
        _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _config = serviceProvider.GetService<IOptions<PayloadLogConfig>>()?.Value;
    }

    public void LogDetailedPayload(PayloadLogMeta payloadLogMeta)
    {
        if (!_config.Enabled) return;

        if (_config.ExcludedActions.Contains(payloadLogMeta.ActionName)) return;

        try
        {
            var responseText = payloadLogMeta.ResponseContent as string ?? JsonConvert.SerializeObject(payloadLogMeta.ResponseContent, Formatting.Indented);

            var logParams = new DynamicParameters(new
            {
                payloadLogMeta.Seq,
                payloadLogMeta.TimeStamp,
                Payload = payloadLogMeta.RequestContent,
                Action = payloadLogMeta.ActionName,
                GameType = payloadLogMeta.GameType,
                Environment = $"{_environmentName}_{payloadLogMeta.SiteName}",
                Response = responseText,
                Elapsed = payloadLogMeta.Elapsed,
            });

            ExecuteWithPartition(
                payloadLogMeta.TimeStamp,
                InsertDetailedPayloadSpName,
                logParams
            );
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    private int ExecuteWithPartition(DateTime parttitionTime, string spName, object parameters)
    {
        var p = new DynamicParameters(parameters);
        p.Add("YYYYMM", GetPartitionKey(parttitionTime));
        return _mainDbClient.QueryFirst<int>(spName, p);
    }

    private static string GetPartitionKey(DateTime partitionTime)
    {
        return partitionTime.ToString("yyyyMM");
    }
}