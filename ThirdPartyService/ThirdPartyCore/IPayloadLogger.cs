namespace ThirdPartyCore;

public interface IPayloadLogger
{
    void LogDetailedPayload(string content, string action = "Unknown", string gameType = "Unknown");
    void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent);
}

