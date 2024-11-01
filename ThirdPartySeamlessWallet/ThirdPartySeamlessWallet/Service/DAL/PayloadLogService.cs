using System;
using Dapper;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.MaxGame;

namespace ThirdPartySeamlessWallet.Service.DAL;

internal class DetailedRequestBody {
    public string Seq { get; set; }
    public string TimeStamp { get; set; }

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

    private readonly MaxGameBLL _maxGameBLL;

    public PayloadLogService(IServiceProvider provider)
    {
        _environmentName = provider.GetRequiredService<IWebHostEnvironment>()?.EnvironmentName ?? "Unspecified";
        _partitionService = provider.GetRequiredService<PartitionService>();
        _maxGameBLL = _maxGameBLL = new MaxGameBLL(provider);
    }

    public virtual void LogDetailedPayload(string content, string action) {
        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(content);
        var payload = getReq.Payload.ToString();
        var gameName = JsonConvert.DeserializeObject<DetailedRequestBodyPayload>(payload)?.GameType;
        var gameType = (int)_maxGameBLL.GetGameType(gameName);
        var environment = _environmentName;

        var logParams = new DynamicParameters(new {
            getReq.Seq,
            getReq.TimeStamp,
            Payload = payload,
            Action = action,
            GameType = gameType,
            Environment = environment,
        });
        DateTime.TryParse(getReq.TimeStamp, out DateTime ts);

        try {

            _partitionService.Execute(
                ts,
                InsertDetailedPayloadSpName,
                logParams
            );

        } catch (Exception ex) {
            SentrySdk.CaptureException(ex);
        }

    }

    public virtual void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent) {
        var getReq = JsonConvert.DeserializeObject<DetailedRequestBody>(requestContent);
        _ = DateTime.TryParse(getReq.TimeStamp, out DateTime ts);
        
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
                ts,
                UpdateDetailedResponseSpName,
                logParams
            );

        } catch (Exception ex) {
            SentrySdk.CaptureException(ex);
        }
    }
}
