using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Serialization;

using Microsoft.Extensions.Logging;

using Newtonsoft.Json;

using ThirdPartyCore.Model;
using ThirdPartyCore.Model.enums;
using ThirdPartyCore.Model.Exceptions;
using ThirdPartyCore.Model.TpBetRecordExtend;
using ThirdPartyCore.Model.TpMerchantBalance.Response;

namespace ThirdPartyCore
{
    public delegate void InsertLogEventHandler(string url, string input, string output);
    public abstract class ThirdPartyHelperBase : IThirdPartyHelper, IThirdPartyTpBetRecordDetail, IThirdPartyTpGetMerchantBalance, IThirdPartyEventParticular
    {
        public static string proxyHost;

        public static int proxyPort;

        private bool _isHeathly;

        public event InsertLogEventHandler InsertLog;

        public static IServerInfoProvider ServerInfoProvider { set; get; }

        public static IAuthProvider AuthProvider { set; get; }

        public static ITpBetRecordProvider TpBetRecordProvider { set; get; }

        public static ITpCustInfoProvider TpCustInfoProvider { set; get; }

        public static IBetLimitProvider BetLimitProvider { set; get; }

        public static string SeamlessServiceHost { get; set; }

        public static string EnvironmentName { set; get; }

        public static bool IsDevelopment { set; get; }

        public static string ContentRootPath { set; get; }

        public static ILogger<IThirdPartyHelper> logger { set; get; }

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

        public DateTime CheckTime { set; get; }

        public ThirdPartyHelperBase()
        {
            isHeathly = true;
        }

        public bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) { return true; }

        protected string HttpPost(string url, string parameters, string contentType, Dictionary<string, string> headers = null, int timeOutTime = 0)
        {
            try
            {
                var parametersLog = parameters;
                if (headers != null)
                {
                    parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
                }
                InsertLog?.Invoke(url, parametersLog, "1");

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
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }

        protected string HttpPostWithProxy(string url, string parameters, string contentType, WebProxy proxy, Dictionary<string, string> headers = null, int timeOutTime = 0)
        {
            try
            {
                var parametersLog = parameters;
                if (headers != null)
                {
                    parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
                }
                InsertLog?.Invoke(url, parametersLog, "1");

                if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
                    ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
                }

                var request = WebRequest.Create(url);
                if (proxy is not null)
                {
                    request.Proxy = proxy;
                }
                else
                {
                    if (!string.IsNullOrEmpty(proxyHost))
                    {
                        request.Proxy = new WebProxy(proxyHost, proxyPort);
                    }
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
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }
        
        protected string HttpPostWithoutParameters(string url, Dictionary<string, string> headers = null, int timeOutTime = 0)
        {
            try
            {
                var parametersLog = url;
                if (headers != null)
                {
                    parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
                }
                InsertLog?.Invoke(url, parametersLog, "1");

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

                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        request.Headers.Add(item.Key, item.Value);
                    }
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

                InsertLog?.Invoke(url, "", sb.ToString()); // parameters 現在為空，因為沒有body
                return sb.ToString();
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }

        protected string HttpPostClient(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            var parametersLog = parameters;
            if (headers != null)
            {
                parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
            }
            InsertLog?.Invoke(url, parametersLog, "1");

            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }

            using var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyHost, proxyPort);
            }
            using var client = new HttpClient(handler);
            try
            {
                var httpRequestMessage = new HttpRequestMessage();
                httpRequestMessage.Content = new StringContent(parameters);
                httpRequestMessage.Method = HttpMethod.Post;
                httpRequestMessage.RequestUri = new Uri(url);
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);
                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        if (string.Compare(key, "Accept", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(value));
                        }
                        else if (key.Contains("-"))
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(key, value);
                        }
                        else
                        {
                            httpRequestMessage.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                var response = client.SendAsync(httpRequestMessage).Result;
                var sb = response.Content.ReadAsStringAsync().Result;
                InsertLog?.Invoke(url, parametersLog, sb);
                return sb;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                InsertLog?.Invoke(url, parametersLog, e.Message);
                return e.Message;
            }
        }

        protected string HttpPostClientWithGzip(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            var parametersLog = parameters;
            if (headers != null)
            {
                parametersLog = $"{parametersLog}, Header={JsonConvert.SerializeObject(headers)}";
            }
            InsertLog?.Invoke(url, parametersLog, "1");

            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }

            using var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyHost, proxyPort);
            }
            using var client = new HttpClient(handler);
            try
            {
                var httpRequestMessage = new HttpRequestMessage();
                httpRequestMessage.Content = new StringContent(parameters);
                httpRequestMessage.Method = HttpMethod.Post;
                httpRequestMessage.RequestUri = new Uri(url);
                httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);
                if (headers != null)
                {
                    foreach (var (key, value) in headers)
                    {
                        if (string.Compare(key, "Accept", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            httpRequestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(value));
                        }
                        else if (key.Contains("-"))
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(key, value);
                        }
                        else
                        {
                            httpRequestMessage.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                string result;
                
                var response = client.SendAsync(httpRequestMessage).Result;
                
                if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                {
                    var raw = response.Content.ReadAsByteArrayAsync().Result;
                    
                    using var output = new MemoryStream();
                    using (var input = new MemoryStream(raw))
                    using (var gzip = new GZipStream(input, CompressionMode.Decompress))
                    {
                        gzip.CopyToAsync(output);
                    }
                    
                    output.Seek(0, SeekOrigin.Begin);
                    result = new StreamReader(output).ReadToEnd();
                }
                else
                {
                    result = response.Content.ReadAsStringAsync().Result;
                }
                
                InsertLog?.Invoke(url, parametersLog, result);
                return result;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                InsertLog?.Invoke(url, parametersLog, e.Message);
                return e.Message;
            }
        }

        
        protected string HttpPostWithX509(string url, Dictionary<string, string> parameters, string contentType, Dictionary<string, string> headers = null, X509Certificate2 certificate = null)
        {
            using var handler = new HttpClientHandler();
            if (certificate != null)
            {
                handler.ClientCertificates.Add(certificate);
            }

            //ignore Certificate
            handler.ServerCertificateCustomValidationCallback = delegate { return true; };

            using var client = new HttpClient(handler);
            try
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, item.Value ?? string.Empty);
                    }
                }
                var formData = new FormUrlEncodedContent(parameters);
                var response = client.PostAsync(url, formData).Result;
                var sb = response.Content.ReadAsStringAsync().Result;
                logger.LogInformation($"response: {sb}");
                return sb;
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    logger.LogError(ex, ex.Message);
                }
                return ae.Message;
            }
            catch (HttpRequestException ex)
            {
                if (ex.InnerException != null)
                {
                    logger.LogError(ex, ex.InnerException.Message);
                }
                return ex.Message;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }

        protected string HttpClientPost(string url, string parameters, string contentType, Dictionary<string, string> headers = null, int timeOutTime = 0)
        {
            try
            {
                //會http status code回傳非200，
                //但同時又會回傳error message的response，可使用。
                using (var client = new HttpClient())
                {
                    if (timeOutTime > 0)
                    {
                        client.Timeout = TimeSpan.FromSeconds(timeOutTime);
                    }
                    
                    if (headers != null)
                    {
                        foreach (var item in headers)
                        {
                            client.DefaultRequestHeaders.Add(item.Key, item.Value);
                        }
                    }
                    
                    using (var content = new StringContent(parameters, Encoding.UTF8, contentType))
                    {
                        using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                        {
                            request.Content = content;
                            using (var response = client.Send(request))
                            {
                                var responseContent = response.Content.ReadAsStringAsync().Result;
                                InsertLog?.Invoke(url, parameters, responseContent);
                                return responseContent;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }

        protected string HttpGet(string url, string parameters, string contentType, Dictionary<string, string> headers = null)
        {
            try
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
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                return e.Message;
            }
        }

        protected string HttpGet(string url, string parameters, Dictionary<string, string> headers = null)
        {
            InsertLog?.Invoke(url, parameters, "1");
            using var handler = new HttpClientHandler();
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(proxyHost, proxyPort);
            }
            using var client = new HttpClient(handler);
            try
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }
                var stream = client.GetStreamAsync($"{url}?{parameters}").Result;
                using StreamReader myStreamReader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"));
                var response = myStreamReader.ReadToEnd();
                InsertLog?.Invoke(url, JsonConvert.SerializeObject(parameters), response);
                return response;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                InsertLog?.Invoke(url, JsonConvert.SerializeObject(parameters), e.Message);
                return e.Message;
            }
        }

        protected string HttpGetWithProxy(string url, string parameters, WebProxy proxy = null, Dictionary<string, string> headers = null)
        {
            InsertLog?.Invoke(url, parameters, "1");

            using var handler = new HttpClientHandler();
            if (proxy is not null)
            {
                handler.UseProxy = true;
                handler.Proxy = proxy;
            }

            using var client = new HttpClient(handler);
            try
            {
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        client.DefaultRequestHeaders.Add(item.Key, item.Value);
                    }
                }

                var stream = client.GetStreamAsync($"{url}?{parameters}").Result;
                using StreamReader myStreamReader = new StreamReader(stream, Encoding.GetEncoding("UTF-8"));
                var response = myStreamReader.ReadToEnd();
                InsertLog?.Invoke(url, JsonConvert.SerializeObject(parameters), response);
                return response;
            }
            catch (Exception e)
            {
                logger.LogError(e, e.Message);
                InsertLog?.Invoke(url, JsonConvert.SerializeObject(parameters), e.Message);
                return e.Message;
            }
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

        protected string DESDecrypt(string pToDecrypt, string DES_Key)
        {
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {

                byte[] inputByteArray = new byte[pToDecrypt.Length / 2];
                //反轉
                for (int x = 0; x < pToDecrypt.Length / 2; x++)
                {
                    int i = (Convert.ToInt32(pToDecrypt.Substring(x * 2, 2), 16));
                    inputByteArray[x] = (byte)i;
                }
                //設定加密金鑰(轉為Byte)
                des.Key = Encoding.ASCII.GetBytes(DES_Key);
                //設定初始化向量(轉為Byte)
                des.IV = Encoding.ASCII.GetBytes(DES_Key);
                using (MemoryStream ms = new MemoryStream())
                {
                    using (CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write))
                    {
                        //例外處理
                        try
                        {
                            cs.Write(inputByteArray, 0, inputByteArray.Length);
                            cs.FlushFinalBlock();
                            //輸出資料
                            return System.Text.Encoding.Default.GetString(ms.ToArray());
                        }
                        catch (CryptographicException)
                        {
                            //若金鑰或向量錯誤，傳回N/A
                            return "N/A";
                        }
                    }
                }
            }
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

        public string AesEncryptV2(string jsonObject, string secretKey)
        {
            byte[] toEncryptArray = Encoding.UTF8.GetBytes(jsonObject);
            byte[] keyBytes = new byte[16];
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
            Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
            RijndaelManaged rm = new RijndaelManaged
            {
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                KeySize = 128,
                BlockSize = 128,
                Key = keyBytes,
                IV = keyBytes

            };
            byte[] resultArray = rm.CreateEncryptor().TransformFinalBlock(toEncryptArray, 0, toEncryptArray.Length);
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public string SHA1(string dataToSign)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();//建立一個SHA1
            byte[] source = Encoding.Default.GetBytes(dataToSign);//將字串轉為Byte[]
            byte[] crypto = sha1.ComputeHash(source);//進行SHA1加密
            string result = Convert.ToBase64String(crypto);//把加密後的字串從Byte[]轉為字串
            return result;
        }

        public static string RSA(string data, string privateKey)
        {
            RSACryptoServiceProvider RSAProvider = new RSACryptoServiceProvider();
            byte[] plaintext = Encoding.UTF8.GetBytes(data);
            RSAProvider.FromXmlString(privateKey);
            RSAParameters priKey = RSAProvider.ExportParameters(true);
            RSAProvider.ImportParameters(priKey);
            byte[] sign = RSAProvider.SignData(plaintext, CryptoConfig.MapNameToOID("MD5"));
            return Convert.ToBase64String(sign);
        }

        public string ApplyToken<T>(T t,int Expiry = 1)
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

                if (AuthProvider.ApplyToken(GuidString, ToJson(t), Expiry))
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
        public bool UpdateTpToken(string token, string tpToken)
        {
            return AuthProvider.UpdateTpToken(token, tpToken);
        }
        public abstract ApiResult CreateMember(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        public abstract ApiResult Login(TpCustInfo tpCustInfo, AppType appType, string TpLangId, Dictionary<string, string> inputs);
        public abstract ApiResult<UserBalance> GetBalance(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);
        public abstract ApiResult<TransferResult> Transfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction direction, string TransID, string ThirdPartyTransID);
        public abstract ApiResult<TransferResult> CheckTransfer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs, decimal Amount, Direction Direction, string TransID, string ThirdPartyTransID);
        public abstract TransIdModel GetThirdId(TpCustInfo tpCustInfo, Dictionary<string, string> inputs);

        /// <summary>
        /// 回傳後台STATEMENT的單號明細（優先使用第三方API提供的明細連結，放置在DetailUrl欄位）
        /// </summary>
        public abstract BaseDetail GetBetDetail(GetBetDetail getBetDetail, TpCustInfo tpCustInfo, Dictionary<string, string> inputs);

        /// <summary>
        /// 呼叫第三方API查詢商號餘額
        /// </summary>
        public virtual ApiResult<MerchantBalanceResponse> GetMerchantBalance(TpMerchantInfo tpMerchantInfo, Dictionary<string, string> inputs)
        {
            throw new GetMerchantBalanceNotImplementedException($"TpId:{tpMerchantInfo.TpId} GetMerchantBalance NotImplemented.");
        }

        /// <summary>
        /// 回傳後台BetList所需要的注單明細
        /// </summary>
        public abstract TpBetRecordExtend GetTpBetRecordExtend(BetRecordInfo betRecordInfo, TpGameTypeOption tpGameType, TpCustInfo tpCustInfo, Dictionary<string, string> inputs);

        /// <summary>
        /// 呼叫第三方API發放獎勵
        /// </summary>
        public virtual ApiResult AddPrizeToPlayer(TpCustInfo tpCustInfo, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException($"TpId:{tpCustInfo.TpId} AddPrizeToPlayer NotImplemented.");
        }
        /// <summary>
        /// 呼叫第三方API創建BonusCode
        /// </summary>
        /// <param name="inputs"></param>
        /// <returns></returns>
        public virtual ApiResult CreateBonusCode(int siteId, Dictionary<string, string> inputs)
        {
            throw new NotImplementedException($"CreateBonusCode NotImplemented.");
        }
    }
}
