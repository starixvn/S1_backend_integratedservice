namespace ThirdPartyService.Service.Model;

public class CreateMemberRule
{
    public int TpId { get; set; }
    public CreateMemberRuleEnum Rule { get; set; }
    /// <summary>
    /// 0=disable, 1=enable
    /// </summary>
    public int UseEnvPrefix { get; set; } = 1;
}
