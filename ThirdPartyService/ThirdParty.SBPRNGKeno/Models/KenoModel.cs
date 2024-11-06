namespace ThirdParty.SBPKeno.Models;

public class LoginResp
{
    public string Seq { get; set; }
    public DateTime TimeStamp { get; set; }
    public int ErrorCode { get; set; }
    public string ErrorMessage { get; set; }
    public Payload_Login Payload { get; set; }
}

public class Payload_Login
{
    public string ReturnUrl { get; set; }
}

public class GetBetHistoryResp : LoginResp
{
}