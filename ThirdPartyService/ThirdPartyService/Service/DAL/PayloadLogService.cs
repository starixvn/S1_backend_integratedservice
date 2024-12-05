using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartyCore;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyService.Service.Library;

namespace ThirdPartyService.Service.DAL;

internal class DetailedRequestBody
{
    public string Seq { get; set; }

    public DateTime TimeStamp { get; set; }

    public object Payload { get; set; }
}

internal class DetailedRequestBodyPayload
{
    public string GameType { get; set; }
}

public class PayloadLogService : IPayloadLogger
{
    private const string InsertDetailedPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins_v2";

    private const string UpdateDetailedResponseSpName = "Service_BettransPK_MaxGame_Payload_Set_v2";

    private readonly MySqlClient _mainDbClient;

    private readonly string _environmentName;

    private readonly PayloadLogConfig _config = default;

    public PayloadLogService(IServiceProvider serviceProvider)
    {
        var commonService = serviceProvider.GetRequiredService<Common>();
        _environmentName = commonService.GetEnvironmentName() ?? "Unspecified";
        _mainDbClient = serviceProvider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        _config = serviceProvider.GetService<IOptions<PayloadLogConfig>>().Value;
    }

    public void LogDetailedPayload(string content, PayloadLogMeta meta)
    {
        if (!_config.Enabled) return;

        var action = meta?.Action
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";
        if (_config.ExcludedActions.Contains(action)) return;

        try
        {
            var gameType = meta?.GameType
                ?? "Unknown";
            var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(content);
            var payload = getReq?.Payload?.ToString() ?? "";
            var environment = _environmentName;

            var logParams = new DynamicParameters(new
            {
                getReq.Seq,
                getReq.TimeStamp,
                Payload = payload,
                Action = action,
                GameType = gameType,
                Environment = environment,
            });

            ExecuteWithPartition(
                getReq.TimeStamp,
                InsertDetailedPayloadSpName,
                logParams
            );
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
        }
    }

    public void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent, PayloadLogMeta meta)
    {
        if (!_config.Enabled) return;

        var action = meta?.Action
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";

        if (_config.ExcludedActions.Contains(action)) return;

        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(requestContent);

        if (responseContent is not null && responseContent is string)
        {
            responseContent = JsonConvert.DeserializeObject((string)responseContent);
        }

        var response = JsonConvert.SerializeObject(responseContent, Formatting.Indented);
        var logParams = new DynamicParameters(new
        {
            getReq.Seq,
            Response = response
        });

        try
        {
            ExecuteWithPartition(
                getReq.TimeStamp,
                UpdateDetailedResponseSpName,
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