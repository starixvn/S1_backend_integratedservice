using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;

namespace ThirdPartyCore
{
    public delegate void InsertLogEventHandler(string url, string input, string output);
    public interface IThirdPartyHelper
    {
        ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs);
        ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID);
        ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID);
        TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        event InsertLogEventHandler InsertLog;
        bool isHeathly { set; get; }
    }

    public interface IThirdPartyParticular
    {
        ApiResult Logout(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        //ApiResult<TransferResult> BonusTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal bonus, string transID, string thirdPartyTransID);
    }

    public interface IThirdPartyGetBalanceParticular
    {
        /// <summary>
        /// 取得指定第三方多筆客戶餘額
        /// </summary>
        /// <param name="inputs">第三方相關參數</param>
        /// <param name="tpUserNameString">第三方使用者名稱參數清單</param>
        /// <returns></returns>
        ApiResult<List<ReceiveTpUserBalanceModel>> GetBalance(Dictionary<string, string> inputs, string tpUserNameString);
    }

    public abstract class ThirdPartyHelperBase : IThirdPartyHelper
    {
        public static string proxyHost;
        public static int proxyPort;
        private bool _isHeathly;
        public event InsertLogEventHandler InsertLog;
        public static IServerInfoProvider ServerInfoProvider { set; get; }
        public static IAuthProvider AuthProvider { set; get; }
        public static string EnvironmentName { set; get; }
        public static bool IsDevelopment { set; get; }
        public static string ContentRootPath { set; get; }
        public bool isHeathly
        {
            set
            {
                if (!value)
                {
                    CheckTime = CheckTime = DateTime.Now;
                }
                _isHeathly = value;
            }

            get
            {
                return _isHeathly;
            }
        }

        public bool isUnderMaintenance { set; get; }
        public DateTime CheckTime { set; get; }
        public ThirdPartyHelperBase()
        {
            isHeathly = true;
        }
        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) { return true; }
        protected string HttpPost(string url, string parameters, string contentType, Dictionary<string, string> headers = null, int timeOutTime = 0)
        {

            InsertLog?.Invoke(url, parameters, "1");

            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }

            WebRequest request = WebRequest.Create(url);
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            if (timeOutTime > 0)
            {
                request.Timeout = timeOutTime * 1000;
            }

            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }
            using (Stream RequestStream = request.GetRequestStream())
            {
                RequestStream.Write(byteArray, 0, byteArray.Length);
            }

            StringBuilder sb = new StringBuilder();
            using (WebResponse response = request.GetResponse())
            using (Stream ResponseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(ResponseStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    sb.Append(line);
                }
            }

            InsertLog?.Invoke(url, parameters, sb.ToString());
            return sb.ToString();
        }

        protected string HttpPostWithX509(string url, string parameters, string contentType, Dictionary<string, string> headers = null, Dictionary<string, string> certificate = null)
        {

            InsertLog?.Invoke(url, parameters, "1");

            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }

            request.Method = "POST";
            byte[] byteArray = Encoding.UTF8.GetBytes(parameters);
            request.ContentType = contentType;
            request.ContentLength = byteArray.Length;
            request.ClientCertificates.Add(new X509Certificate2(certificate["path"], certificate["password"], X509KeyStorageFlags.MachineKeySet));
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }
            using (Stream RequestStream = request.GetRequestStream())
            {
                RequestStream.Write(byteArray, 0, byteArray.Length);
            }

            StringBuilder sb = new StringBuilder();
            using (WebResponse response = request.GetResponse())
            using (Stream ResponseStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(ResponseStream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    sb.Append(line);
                }
            }

            InsertLog?.Invoke(url, parameters, sb.ToString());
            return sb.ToString();
        }
        private static bool CertificateValidationCallBack(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }
            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    foreach (X509ChainStatus status in
                   chain.ChainStatus)
                    {
                        if ((certificate.Subject == certificate.Issuer) &&
                        (status.Status ==
                       X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are valid.
                            continue;
                        }
                        else
                        {
                            if (status.Status !=
                           X509ChainStatusFlags.NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                // In all other cases, return false.
                return false;
            }
        }

        protected string HttpGet(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            InsertLog?.Invoke(url, parameters, "1");
            WebRequest request = WebRequest.Create($"{url}?{parameters}");
            if (!string.IsNullOrEmpty(proxyHost))
            {
                request.Proxy = new WebProxy(proxyHost, proxyPort);
            }
            request.Method = "GET";
            request.ContentType = contentType;
            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }

            StringBuilder sb = new StringBuilder();
            using (WebResponse response = request.GetResponse())
            using (Stream dataStream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(dataStream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    sb.Append(line);
                }
            }
            InsertLog?.Invoke(url, parameters, sb.ToString());
            return sb.ToString();
        }
        protected string ToMD5(string encodeStr)
        {
            using var cryptoMD5 = MD5.Create();
            var bytes = Encoding.UTF8.GetBytes(encodeStr);
            var hash = cryptoMD5.ComputeHash(bytes);
            var md5 = BitConverter.ToString(hash)
              .Replace("-", string.Empty);

            return md5;
        }
        protected string GetRandomString(int length)
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
        protected int GetTimestemp()
        {
            return Convert.ToInt32((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds);
        }

        protected long GetTimestemp1()
        {
            return Convert.ToInt64((DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds);
        }

        protected T FromJson<T>(string jsonString)
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
        protected string ToJson(object t)
        {
            return JsonConvert.SerializeObject(t, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });
        }

        protected bool IsJSON(string jsonString)
        {
            var value = jsonString.Trim();

            return (value.StartsWith("{") && value.EndsWith("}")) || (value.StartsWith("[") && value.EndsWith("]"));
        }
        protected bool IsXML(string xmlString)
        {
            var value = xmlString.Trim();

            return (value.StartsWith("<") && value.EndsWith(">"));
        }
        protected T XMLDeserialize<T>(string xmlString) where T : class
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            using (TextReader reader = new StringReader(xmlString))
            {
                var deserializationObj = (T)deserializer.Deserialize(reader);
                return deserializationObj;
            };
        }

        protected string DESEncryptSA(string inString, string secretkey)
        {
            var EncryptKey = Encoding.ASCII.GetBytes(secretkey);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, new
            DESCryptoServiceProvider().CreateEncryptor(EncryptKey, EncryptKey),
            CryptoStreamMode.Write);
            using var sw = new StreamWriter(cs);
            sw.Write(inString);
            sw.Flush();
            cs.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        protected string DESEncrypt(string pToEncrypt, string DES_Key)
        {
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = Encoding.GetEncoding("UTF-8").GetBytes(pToEncrypt);

            des.Key = Encoding.ASCII.GetBytes(DES_Key);
            des.IV = Encoding.ASCII.GetBytes(DES_Key);

            StringBuilder ret = new StringBuilder();
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(), CryptoStreamMode.Write))
            {
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                foreach (byte b in ms.ToArray())
                {
                    ret.AppendFormat("{0:X2}", b);
                }
            }

            return ret.ToString();
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

            return isEncodeUrl ? System.Web.HttpUtility.UrlEncode(base64Str) : base64Str;
        }
        public string AesDecrypt(string str, string key, bool isDecodeUrl = true)
        {
            if (string.IsNullOrEmpty(str)) return null;
            if (isDecodeUrl)
                str = System.Web.HttpUtility.UrlDecode(str);

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
        public string SHA1(string dataToSign)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
            byte[] source = Encoding.Default.GetBytes(dataToSign);//將字串轉為Byte[]
            byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
            string result = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
            return result;
        }


        public void Info(string InfoContent, string PrefixName)
        {
            var LogFolder = $"{Directory.GetCurrentDirectory()}\\Logs";

            var filePath = $@"{LogFolder}\{PrefixName}_Info_{DateTime.Now.ToString("yyyyMMdd")}.log";
            Task.Run(() =>
            {
                Directory.CreateDirectory(LogFolder);
                using StreamWriter writer = new StreamWriter(filePath, append: true);
                writer.WriteLine($"{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss")}\r\n  {InfoContent}");
            });
        }
        public string ApplyToken<T>(T t)
        {
            if (AuthProvider != null)
            {
                Guid g = Guid.NewGuid();
                string GuidString = Convert.ToBase64String(g.ToByteArray());
                GuidString = GuidString.Replace("=", "");
                GuidString = GuidString.Replace("+", "");
                GuidString = GuidString.Replace("/", "");
                GuidString = GuidString.Replace("_", "");
                GuidString = GuidString.Replace("@", "");

                if (AuthProvider.ApplyToken(GuidString, ToJson(t), 1))
                {
                    return GuidString;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public bool Authenticate<T>(string sToken, out T t)
        {
            string sPermissionData = "";
            t = default;
            sPermissionData = AuthProvider.Authenticate(sToken);

            if (!string.IsNullOrEmpty(sPermissionData))
            {
                t = FromJson<T>(sPermissionData);
                return true;
            }

            return false;
        }
        public abstract ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        public abstract ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs);
        public abstract ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        public abstract ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID);
        public abstract ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID);
        public abstract TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
    }

}
