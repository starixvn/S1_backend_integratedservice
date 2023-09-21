namespace CommonService.Service.Model;

public class LetsTalkConfig
{
    public LetsTalkCommon LetsTalkCommon { get; set; }
    public List<LetsTalkNotification> LetsTalkNotification { get; set; }
}

public class LetsTalkCommon
{
    public string SignInURL { get; set; }
    public string SandMessageURL { get; set; }
}

public class LetsTalkNotification
{
    public string GroupName { get; set; }
    public int SignInId { get; set; }
    public string SignInToken { get; set; }
    public string SandMessageRoomID { get; set; }
}