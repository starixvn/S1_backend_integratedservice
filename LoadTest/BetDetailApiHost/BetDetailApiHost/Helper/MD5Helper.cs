using System.Security.Cryptography;
using System.Text;

namespace SabaSimulator.API.Helper;

public static class MD5Helper
{
	public static string ToMD5(string encodeStr)
	{
		using var cryptoMD5 = MD5.Create();
		var bytes = Encoding.UTF8.GetBytes(encodeStr);
		var hash = cryptoMD5.ComputeHash(bytes);
		var md5 = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

		return md5.ToLower();
	}
}
