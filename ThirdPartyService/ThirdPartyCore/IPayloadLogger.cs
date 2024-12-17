using ThirdPartyCore.Model;

namespace ThirdPartyCore;

public interface IPayloadLogger
{
    void LogDetailedPayload(PayloadLogMeta payloadLogMeta);
}