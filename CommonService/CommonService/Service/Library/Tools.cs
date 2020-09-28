using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace CommonService.Service.Library
{
    public class Tools
    {
        public Tools()
        {
        }
        public string ToMD5(string encodeStr)
        {
            using (var cryptoMD5 = MD5.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(encodeStr);
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
    }
}
