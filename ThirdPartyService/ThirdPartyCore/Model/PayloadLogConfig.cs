namespace ThirdPartyCore.Model;

public class PayloadLogConfig
{
    public bool Enabled { get; set; } = false;
    public HashSet<string> ExcludedActions { get; set; } = new HashSet<string>();
}

public class PayloadLogMeta
{
    public string Seq { get; set; }
    public DateTime TimeStamp { get; set; }
    public string ActionName { get; set; }
    public string GameType { get; set; }
    public string SiteName { get; set; }
    public string RequestContent { get; set; }
    public object ResponseContent { get; set; }
    public double Elapsed { get; set; }
}