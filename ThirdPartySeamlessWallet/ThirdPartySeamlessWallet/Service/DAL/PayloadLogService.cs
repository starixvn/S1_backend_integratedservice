using System;
using Dapper;
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

public class PayloadLogService
{
    const string InsertDetailedPayloadSpName = "Service_BettransPK_MaxGame_Payload_Ins_v2"; 

    const string UpdateDetailedResponseSpName = "Service_BettransPK_MaxGame_Payload_Set_v2";

    private readonly PartitionService _partitionService;
    private readonly string _environmentName = "";

    public PayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<IWebHostEnvironment>()?.EnvironmentName ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
    }

    public virtual void LogDetailedPayload(string content, string action) {
        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(content);
        var payload = getReq.Payload.ToString();
        var gameType = JsonConvert.DeserializeObject<DetailedRequestBodyPayload>(payload)?.GameType ?? "Unknown";
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

    public virtual void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent) {
        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(requestContent);
        
        if (responseContent is not null && responseContent is string) {
            responseContent = JsonConvert.DeserializeObject(requestContent);
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
