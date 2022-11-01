using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using CommonService.Service.Model;
using Microsoft.Extensions.Options;

namespace CommonService.Service.Library;

public class AWSSNSHelper
{
    private readonly IAmazonSimpleNotificationService _client;

    public AWSSNSHelper(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var awsKeyConfig = provider.GetRequiredService<IOptions<Dictionary<string, AWSKeyConfig>>>().Value["ailab-sns"];
        var proxyHost = configuration.GetValue<string>("ProxyHost");
        var proxyPort = configuration.GetValue<int>("ProxyPort");

        var config = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = RegionEndpoint.APSoutheast1
        };

        if (!string.IsNullOrEmpty(proxyHost))
        {
            config.ProxyHost = proxyHost;
            config.ProxyPort = proxyPort;
        }

        _client = new AmazonSimpleNotificationServiceClient(awsKeyConfig.AccessKey, awsKeyConfig.SecretKey, config);
        var attributes = new SetSMSAttributesRequest();
        attributes.Attributes.Add("DefaultSMSType", "Transactional");
        _client.SetSMSAttributesAsync(attributes);
    }

    public async Task<PublishResponse> PublishAsync(PublishRequest request)
    {
        return await _client.PublishAsync(request);
    }
}