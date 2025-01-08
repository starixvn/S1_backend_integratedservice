using SabaSimulator.API.Models.LaunchGame;

namespace SabaSimulator.API.Models.SabaPlus;

public class GameProviderRequest<TPayload>
{
	public string? TimeStamp { get; set; }

	public string? Seq { get; set; }

	public string? OperatorKey { get; set; }

	public TPayload? Payload { get; set; }
}