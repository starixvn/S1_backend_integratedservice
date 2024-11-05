using System;
using System.Diagnostics;
using Dapper;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Service.DAL;

internal class DetailedRequestBody {
    public string Seq { get; set; }
    public DateTime TimeStamp { get; set; }

    public object Payload { get; set; }
}

internal class DetailedRequestBodyPayload {
    public string OperatorCustId { get; set; }
    public string WalletSideToken { get; set; }
    public string GameType { get; set; }

    public string GameId { get; set; }
    public object Additional { get; set; }  
}

public class PayloadLogConfig {
    public bool Enabled { get; set; } = false;
    public HashSet<string> ExcludedActions  { get; set; } = new HashSet<string>();
}

public class PayloadLogMeta {
    public string Action { get; set; }
    public string GameType { get; set; }
}

public class PayloadLogService
{
    const string InsertDetailedPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins_v2"; 

    const string UpdateDetailedResponseSpName = "Service_BettransPK_MaxGame_Payload_Set_v2";

    private readonly PartitionService _partitionService;
    private readonly string _environmentName = "";

    private readonly PayloadLogConfig _config = default;

    public PayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<IWebHostEnvironment>()?.EnvironmentName ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _config = provider.GetService<IOptions<PayloadLogConfig>>().Value;
    }

    public virtual void LogDetailedPayload(string content, PayloadLogMeta meta = null) {
        if (!_config.Enabled) return;
        
        var action = meta?.Action 
            ?? (new StackTrace().GetFrame(1).GetMethod().Name)
            ?? "Unknown";

        if (_config.ExcludedActions.Contains(action)) return;

        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(content);
        var payload = getReq.Payload.ToString();
        
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

        try {

            _partitionService.Execute(
                getReq.TimeStamp,
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
        
        if (responseContent is not null && responseContent is string) {
            responseContent = JsonConvert.DeserializeObject(responseContent.ToString());
        }
        var response = JsonConvert.SerializeObject(responseContent,Formatting.Indented);
        var logParams = new DynamicParameters(new {
            getReq.Seq,
            Response = response
        });

        try {
            _partitionService.Execute(
                getReq.TimeStamp,
                UpdateDetailedResponseSpName,
                logParams
            );

        } catch (Exception ex) {
            SentrySdk.CaptureException(ex);
        }
    }
}
