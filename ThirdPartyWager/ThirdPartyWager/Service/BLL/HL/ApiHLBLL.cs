using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;
using ThirdPartyWager.Service.Model.TpSiteConfig;

namespace ThirdPartyWager.Service.BLL.HL;

public class ApiHLBLL
{
    private readonly HttpHelper _httpHelper;
    private readonly Common _common;
    private readonly Log2File _log2File;
    private readonly ILogger<ApiHLBLL> _logger;
    private readonly string _tpName = Tp.HL.ToString();
    private readonly IConfiguration _configuration;
    private readonly HLConfig _siteHLConfig;
    
    public ApiHLBLL(IServiceProvider provider)
    {
        _httpHelper = provider.GetRequiredService<HttpHelper>();
        _log2File = provider.GetRequiredService<Log2File>();
        _logger = provider.GetRequiredService<ILogger<ApiHLBLL>>();
        _configuration = provider.GetRequiredService<IConfiguration>();
        _siteHLConfig = provider.GetRequiredService<ConfigBLL>().GetConfigValue<HLConfig>(Tp.HL.ToString());
    }
    
}