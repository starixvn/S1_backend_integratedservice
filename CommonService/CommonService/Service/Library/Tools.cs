using System.Security.Cryptography;
using System.Text;

namespace CommonService.Service.Library
{
    public class Tools
    {
        public string ToMD5(string encodeStr)
        {
            using (var cryptoMD5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(encodeStr);
                var hash = cryptoMD5.ComputeHash(bytes);
                var md5 = BitConverter.ToString(hash)
                .Replace("-", String.Empty)
                .ToLower();

                return md5;
            }
        }

        public string GetRandomNumString(int length)
        {
            // var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
            var str = "0123456789";
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }
        
        public string SHA256(string input)
        {
            var inputBytes = Encoding.UTF8.GetBytes(input);
            var outputBytes = new SHA256CryptoServiceProvider().ComputeHash(inputBytes);
            return BitConverter.ToString(outputBytes).Replace("-", "").ToLower();
        }
    }
}
