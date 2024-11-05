using ThirdPartyCore.Model;

namespace ThirdPartyCore;

public interface IPayloadLogger
{
    void LogDetailedPayload(string content, PayloadLogMeta meta = null);
    void UpdateDetailedPayLoadWithResponse(string requestContent, object responseContent, PayloadLogMeta meta = null);
}

