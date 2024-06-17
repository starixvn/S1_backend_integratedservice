using CommonService.Service.Model.enums;

namespace CommonService.Service.Model;

public class SMSSupportConfig
{
    public Dictionary<string, List<OTPProvider>> SMSSupport { get; set; }
}
 