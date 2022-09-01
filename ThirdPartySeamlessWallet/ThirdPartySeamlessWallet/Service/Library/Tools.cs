using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
        public long ConvertLocTime(DateTime time, int OffsetsHours) =>
            new DateTimeOffset(time, new TimeSpan(OffsetsHours, 0, 0)).ToUnixTimeSeconds();
        public T XMLDeserialize<T>(string xmlString) where T : class
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (StringReader reader = new StringReader(xmlString))
            {
                var deserializationObj = (T)deserializer.Deserialize(reader);
                return deserializationObj;
            };
        }

        public string TransformJsonToXml<T>(string json)
        {
            // Remove root property
            var jsonObjectAttribute = typeof(T).GetCustomAttribute(typeof(JsonObjectAttribute)) as JsonObjectAttribute;
            var root = JObject.Parse(json);
            var data = root[jsonObjectAttribute?.Title ?? throw new InvalidOperationException()];

            var obj = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(data));

            var serializer = new XmlSerializer(typeof(T));
            using var memoryStream = new MemoryStream();
            using var xmlWriter = XmlWriter.Create(memoryStream, new XmlWriterSettings() { Indent = true, Encoding = Encoding.UTF8 });
            xmlWriter.WriteStartDocument();
            var xmlSerializerNamespaces = new XmlSerializerNamespaces();
            xmlSerializerNamespaces.Add("", "");
            serializer.Serialize(xmlWriter, obj, xmlSerializerNamespaces);
            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }

        public string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);

            return md5;
        }

        public string AesDecrypt(string str, string key, bool isDecodeUrl = true)
        {
            if (string.IsNullOrEmpty(str)) return null;
            if (isDecodeUrl)
                str = HttpUtility.UrlDecode(str);

            byte[] toEncryptArray = Convert.FromBase64String(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            return Encoding.UTF8.GetString(resultArray);
        }

        public string AesEncrypt(string str, string key, bool isEncodeUrl = true)
        {
            if (string.IsNullOrEmpty(str)) return null;
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(str);

            RijndaelManaged rm = new RijndaelManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };

            ICryptoTransform cTransform = rm.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);

            var base64Str = Convert.ToBase64String(resultArray, 0, resultArray.Length);

            return isEncodeUrl ? HttpUtility.UrlEncode(base64Str) : base64Str;
        }

        public T FromJson<T>(string jsonString)
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
        public string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        public long GetUnixTimestampByTicks(long ticks)
        {
            long unixTimestamp = ticks - new DateTime(1970, 1, 1).Ticks;
            return unixTimestamp / TimeSpan.TicksPerSecond * 1000;
        }

        #region GetClientIP
        /// <summary>
        /// 获取客户端IP
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public string GetClientIP(HttpContext httpContext)
        {
            string[] HeaderNames = new string[] { "X-Forwarded-For", "HTTP_X_FORWARDED_FOR", "HTTP_INCAP_CLIENT_IP", "HTTP_TRUE_CLIENT_IP", "HTTP_X_REAL_IP", "REMOTE_ADDR" };

            foreach (string Header in HeaderNames)
            {

                if (httpContext.Request.Headers.ContainsKey(Header))
                {
                    string sServerVariables = httpContext.Request.Headers[Header];
                    string ip = GetIPAddress(sServerVariables.Split(','));
                    if (!string.IsNullOrEmpty(ip))
                    {
                        return ip;
                    }
                }
            }

            return httpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        private string GetIPAddress(string[] IPArry)
        {
            foreach (string ip in IPArry)
            {
                string IPAddress = ip.Trim();
                if (IsIPAddress(IPAddress)
                    && !isInnerIP(IPAddress)) return IPAddress.Trim();
            }

            return null;
        }
        private bool isInnerIP(string ipAddress)
        {
            long ipNum = getIpNum(ipAddress);

            long aBegin = 167772160;// getIpNum("10.0.0.0");
            long aEnd = 184549375;// getIpNum("10.255.255.255");
            long bBegin = 2886729728; //getIpNum("172.16.0.0");
            long bEnd = 2887778303; //getIpNum("172.31.255.255");
            long cBegin = 3232235520; //getIpNum("192.168.0.0");
            long cEnd = 3232301055; //getIpNum("192.168.255.255");

            // Logger.Log("aBegin:" + aBegin + ",aEnd:" + aEnd + ",bBegin:" + bBegin + ",bEnd:" + bEnd + ",cBegin:" + cBegin + ",cEnd:" + cEnd);

            bool isInnerIp = isInner(ipNum, aBegin, aEnd) || isInner(ipNum, bBegin, bEnd) || isInner(ipNum, cBegin, cEnd) || ipAddress == "127.0.0.1";
            return isInnerIp;
        }
        private long getIpNum(string ipAddress)
        {
            string[] ip = ipAddress.Split('.');
            long a = Convert.ToInt32(ip[0]);
            long b = Convert.ToInt32(ip[1]);
            long c = Convert.ToInt32(ip[2]);
            long d = Convert.ToInt32(ip[3]);

            long ipNum = a * 256 * 256 * 256 + b * 256 * 256 + c * 256 + d;
            return ipNum;
        }
        private bool IsIPAddress(string str1)
        {
            if (string.IsNullOrEmpty(str1) || str1.Length < 7 || str1.Length > 15) return false;
            const string regformat = @"^\d{1,3}[\.]\d{1,3}[\.]\d{1,3}[\.]\d{1,3}$";

            Regex regex = new Regex(regformat, RegexOptions.IgnoreCase);
            return regex.IsMatch(str1);

        }
        private bool isInner(long userIp, long begin, long end)
        {
            return (userIp >= begin) && (userIp <= end);
        }
        #endregion
    }
}
