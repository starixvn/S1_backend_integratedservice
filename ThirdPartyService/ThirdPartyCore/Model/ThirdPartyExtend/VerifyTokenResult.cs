namespace ThirdPartyCore.Model;

public class VerifyTokenResult
{
    public string TpUserName { get; set; } = string.Empty;
    public VerifyTokenResult()
    {

    }
    public VerifyTokenResult(string tpUserName)
    {
        TpUserName = tpUserName.Trim();
    }
}
