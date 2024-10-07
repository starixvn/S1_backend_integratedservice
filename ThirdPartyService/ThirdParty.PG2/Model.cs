using Newtonsoft.Json;
using ThirdPartyCore.Model;
namespace ThirdParty.PG2;


public class CommonResponse
{
    public Status status { get; set; }
    public Data? data { get; set; }
}

public class Status
{
    public int code { get; set; }
    public string message { get; set; }
}

public class Data
{
    public string url { get; set; }
}