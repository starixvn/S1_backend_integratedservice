namespace CommonService.Service.Model;


public class TelegramConfig
{
    public TelegramCommon TelegramCommon { get; set; }
    public List<TelegramNotification> TelegramNotification { get; set; }
}

public class TelegramCommon
{
    public string Url { get; set; }
}

public class TelegramNotification
{
    public string GroupName { get; set; }
    public string Token { get; set; }
    public long ChatId { get; set; }
}