using Newtonsoft.Json;

namespace ThirdPartyCore;

public class TemporaryLogSentryService
{
    public void WriteLogLaunchGame(string thirdPartyName, string apiUrl, Dictionary<string, string> header, string postData, string result)
    {
        SentrySdk.CaptureMessage($"{thirdPartyName} at {DateTime.Now}" +
                                 $"{Environment.NewLine}--ApiUrl: {JsonConvert.SerializeObject(apiUrl)}" +
                                 $"{Environment.NewLine}--header: {JsonConvert.SerializeObject(header)}" +
                                 $"{Environment.NewLine}--postData: {postData}" +
                                 $"{Environment.NewLine}---------------------" +
                                 $"{Environment.NewLine}--result: {result}");
    }
}