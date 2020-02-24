using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ThirdPartySeamlessWallet.Service.Library
{
    public class Tools
    {
        /// <summary>
        /// 保留两位小数的金额
        /// </summary>
        /// <param name="d"></param>
        /// <param name="decimals"></param>
        /// <returns></returns>
        public decimal Floor(decimal d, int decimals)
        {
            long value = (long)Math.Pow(10.0, decimals);
            return Convert.ToDecimal(Math.Floor(d * (decimal)value) / (decimal)value);
        }

        public string DESDecrypt(string inString, string encryptKey)
        {
            byte[] EncryptKey = Encoding.ASCII.GetBytes(encryptKey);

            try
            {
                return new StreamReader(new CryptoStream(new MemoryStream(
                Convert.FromBase64String(inString)), new
                DESCryptoServiceProvider().CreateDecryptor(EncryptKey, EncryptKey),
                CryptoStreamMode.Read)).ReadToEnd();
            }
            catch
            {

            }
            return "";
        }
    }
}
