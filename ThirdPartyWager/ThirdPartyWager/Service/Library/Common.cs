using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json;

namespace ThirdPartyWager.Service.Library
{
    public class Common
    {
        public string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);
            return md5.ToLower();
        }
        public string random()
        {
            using var csp = new RNGCryptoServiceProvider();
            var byteCsp = new byte[5];
            csp.GetBytes(byteCsp);
            return BitConverter.ToString(byteCsp);
        }
        public string encrypt(string data, string base64Key, string base64edIv, byte[] NULL_IV)
        {
            using var des = new TripleDESCryptoServiceProvider();
            des.Mode = CipherMode.CBC;
            des.BlockSize = 64;
            des.Padding = PaddingMode.PKCS7;
            des.Key = Convert.FromBase64String(base64Key);
            des.IV = base64edIv == null ? NULL_IV : Convert.FromBase64String(base64edIv);

            var dataBytes = Encoding.UTF8.GetBytes(data);

            using var stream = new MemoryStream();
            using var encStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);
            encStream.Write(dataBytes, 0, dataBytes.Length);
            encStream.FlushFinalBlock();

            var result = Convert.ToBase64String(stream.ToArray());
            encStream.Close();
            stream.Close();
            return result;
        }
        public string base64edMd5(string data)
        {
            return Convert.ToBase64String(md5(Encoding.UTF8.GetBytes(data)));
        }

        public byte[] md5(byte[] data)
        {
            using var md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }
        public string GetRandomString(int length)
        {
            var str = "0123456789abcdefghijklmnopqrstuvwxyz";//ABCDEFGHIJKLMNOPQRSTUVWXYZ
            var next = new Random();
            var builder = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                builder.Append(str[next.Next(0, str.Length)]);
            }
            return builder.ToString();
        }
        public string encrypt(string data, string base64Key)
        {
            var btIv = Encoding.UTF8.GetBytes(base64Key);
            var btKey = Encoding.UTF8.GetBytes(base64Key);
            using var des = new DESCryptoServiceProvider();
            using var ms = new MemoryStream();
            var inData = Encoding.UTF8.GetBytes(data);

            using var cs = new CryptoStream(ms, des.CreateEncryptor(btKey, btIv), CryptoStreamMode.Write);
            cs.Write(inData, 0, inData.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }
        public T FromJson<T>(string jsonString)
        {
            try
            {
                if (!string.IsNullOrEmpty(jsonString))
                {
                    return JsonConvert.DeserializeObject<T>(jsonString);
                }
                else
                {
                    return default(T);
                }
            }
            catch (Exception)
            {
                return default(T);
            }
        }
        public string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }
        public long GetTimestemp1()
        {
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds);
        }

        public string HMACSHA256(string message, string key)
        {
            var encoding = new System.Text.UTF8Encoding();
            var keyByte = encoding.GetBytes(key);
            var messageBytes = encoding.GetBytes(message);
            using (var hmacSHA256 = new HMACSHA256(keyByte))
            {
                var hashMessage = hmacSHA256.ComputeHash(messageBytes);
                return BitConverter.ToString(hashMessage).Replace("-", "").ToLower();
            }
        }

        public string AesEncrypt(string str, string key, bool isEncodeUrl = true)
        {
            if (string.IsNullOrEmpty(str)) return null;
            var toEncryptArray = Encoding.UTF8.GetBytes(str);

            var rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            var cTransform = rm.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            var base64Str = Convert.ToBase64String(resultArray, 0, resultArray.Length);

            return isEncodeUrl ? System.Web.HttpUtility.UrlEncode(base64Str) : base64Str;
        }

        public string Sign(string EncryptText, string EncryptKey)
        {
            using var myHMACSHA1 = new HMACSHA1(Convert.FromBase64String(EncryptKey));
            var RstRes = myHMACSHA1.ComputeHash(Encoding.UTF8.GetBytes(EncryptText));

            return Convert.ToBase64String(RstRes);
        }

        public string RSA(string data, string privateKey)
        {
            RSACryptoServiceProvider RSAProvider = new RSACryptoServiceProvider();
            byte[] plaintext = Encoding.UTF8.GetBytes(data);
            RSAProvider.FromXmlString(privateKey);
            RSAParameters priKey = RSAProvider.ExportParameters(true);
            RSAProvider.ImportParameters(priKey);
            byte[] sign = RSAProvider.SignData(plaintext, CryptoConfig.MapNameToOID("MD5"));
            return Convert.ToBase64String(sign);
        }

        public string HMACSHA512(string input, string key)
        {
            var salt = Encoding.UTF8.GetBytes(key);
            var HASH_SIZE = 64;
            var ITERATIONS = 1000;

            // Generate the hash
            var pbkdf2 = new Rfc2898DeriveBytes(input, salt, ITERATIONS, HashAlgorithmName.SHA512);
            var data = pbkdf2.GetBytes(HASH_SIZE);
            return Convert.ToBase64String(data);
        }
    }
}
