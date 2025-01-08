namespace SabaSimulator.API.Models.LaunchGame;

public class LaunchGameResult
{
	public string Seq { get; set; }= Guid.NewGuid().ToString("N");
	public string TimeStamp { get; set; }= DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff");
	public int ErrorCode { get; set; } = 0; 
	public string ErrorMessage { get; set; } = "Success";
	public string Message { get; set;  }= "Success";
	public LaunchGamePayload Payload { get; set; } = new LaunchGamePayload();
}
public class LaunchGamePayload
{
	public string ReturnUrl { get; set; } 

	public string Token { get; set; } = Guid.NewGuid().ToString("N");
}