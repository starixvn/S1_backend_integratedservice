namespace ThirdPartyCore.Model;

public class GetTokenResult
{
    public string Token { get; set; } = string.Empty;
    public GetTokenResult()
    {
        
    }
    public GetTokenResult(string token)
    {
        Token = token;
    }
}
