using System.Text.Json.Serialization;

namespace SabaSimulator.API.Models.SabaPlus;

public class GameProviderResponse<TPayload>
{
	public string? TimeStamp => DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.fffz");

	public string? Seq => Guid.NewGuid().ToString();

	public int? ErrorCode { get; set; } = 0;


	public string? ErrorMessage;

	public TPayload? Payload { get; set; }
}