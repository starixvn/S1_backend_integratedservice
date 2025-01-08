namespace SabaSimulator.API.Models.LaunchGame;

public class LaunchGameRequest
{
	public string? TimeStamp { get; set; } 
	public string? Seq { get; set; } 
	public Payload? Payload { get; set; }
}

public class Payload
{
	public ProductInfo? ProductInfo { get; set; }
	public CustInfo? CustInfo { get; set; }
	public AgentInfo? AgentInfo { get; set; }
	public string? HomeUrl { get; set; } 
	public AdditionalInfo? Additional { get; set; } 
}

public class ProductInfo
{
	public string? GameType { get; set; } 
	public string? GameId { get; set; }
	public string? AppType { get; set; }
}

public class CustInfo
{
	public string? OperatorCustomerId { get; set; } 
	public string? OperatorCustomerName { get; set; } 
	public string? Language { get; set; } 
	public string? Platform { get; set; } 
	public string? CurrencyISO { get; set; } 
	public string? BrowserUserAgent { get; set; }
	public string? IpAddress { get; set; } 
	public string? WalletSideToken { get; set; } 
}

public class AgentInfo
{
	public string? AgentId { get; set; } 
	public string? AgentKey { get; set; }
	public string? AgentCode { get; set; } 
	public string? ExtraInfo { get; set; } 
}

public class AdditionalInfo
{
	public string? SpecialKey { get; set; }
}
