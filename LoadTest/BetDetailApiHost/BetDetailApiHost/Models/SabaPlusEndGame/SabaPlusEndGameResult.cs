using SabaSimulator.API.Models.SabaPlus;
namespace SabaSimulator.API.Models.SabaPlusEndGame;

public class SabaPlusEndGameResult : GameProviderResponse<EndGameResponsePayload>;

public record EndGameResponsePayload(string OperatorCustId, decimal Balance);