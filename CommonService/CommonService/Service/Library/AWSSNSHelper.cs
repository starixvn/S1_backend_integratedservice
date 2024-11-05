using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

using CommonService.Service.BLL;
using CommonService.Service.Model;

namespace CommonService.Service.Library;

public class AWSSNSHelper
{
    private readonly IAmazonSimpleNotificationService _client;
    private readonly AWSKeyConfig _awsKeyConfig;

    public AWSSNSHelper(IServiceProvider provider)
    {
        var configuration = provider.GetRequiredService<IConfiguration>();
        var configBLL = provider.GetRequiredService<ConfigBLL>();
        _awsKeyConfig = configBLL.GetConfigValue<AWSKeyConfig>("AWSKey");
        var proxyHost = configuration.GetValue<string>("ProxyHost");
        var proxyPort = configuration.GetValue<int>("ProxyPort");

        var region = configBLL.GetConfigValue("AWSRegion");

        var config = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };

        if (!string.IsNullOrEmpty(proxyHost))
        {
            config.ProxyHost = proxyHost;
            config.ProxyPort = proxyPort;
        }

        //AWSKey也只有一組，直接拿就好
        _client = new AmazonSimpleNotificationServiceClient(_awsKeyConfig.AccessKey, _awsKeyConfig.SecretKey, config);
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