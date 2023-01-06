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
        var awsKeyConfig = provider.GetRequiredService<IOptions<AWSKeyConfig>>();
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

        _client = new AmazonSimpleNotificationServiceClient(awsKeyConfig.Value.AccessKey, awsKeyConfig.Value.SecretKey, config);
        var attributes = new SetSMSAttributesRequest();
        attributes.Attributes.Add("DefaultSMSType", "Transactional");
        _client.SetSMSAttributesAsync(attributes);
    }

    public async Task<PublishResponse> PublishAsync(PublishRequest request)
    {
        if (request.PhoneNumber.StartsWith("+66"))
        {
            var senderId = new MessageAttributeValue
            {
                StringValue = "NWBusiness",
                DataType = "String"
            };
            request.MessageAttributes.Add("AWS.SNS.SMS.SenderID", senderId);
        }
        
        return await _client.PublishAsync(request);
    }
}