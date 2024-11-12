using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;
using System.Xml.Serialization;
using Newtonsoft.Json;

using Serilog.Context;

namespace CommonService.Service.Library
{
    public class HttpHelper
    {
        public static string proxyHost;
        public static int proxyPort;
        private readonly ILogger<HttpHelper> _logger;
        public HttpHelper(IServiceProvider provider)
        {
            _logger = provider.GetRequiredService<ILogger<HttpHelper>>();
        }

        public async Task<string> HttpPostAsync(string url, object parameter, string contentType, Dictionary<string, string> headers = null, X509Certificate2 certificate = null)
        {
            using var handler = new HttpClientHandler();
            if (certificate != null)
            {
                handler.ClientCertificates.Add(new X509Certificate2(certificate));
            }

            handler.ServerCertificateCustomValidationCallback = delegate { return true; };

            using var client = new HttpClient(handler);
            HttpContent content;

            if (parameter is Dictionary<string, string> paramDict)
            {
                content = new FormUrlEncodedContent(paramDict);
            }
            else if (parameter is string xmlString)
            {
                content = new StringContent(xmlString, Encoding.UTF8, contentType);
            }
            else
            {
                throw new ArgumentException("Unsupported parameters type. Use Dictionary<string, string> or string.");
            }

            content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);
            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    request.Headers.Add(item.Key, item.Value);
                }
            }

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation($"HttpHelper.Request.Response: {responseContent}");
            return responseContent;
        }

        public async Task<HttpResponseMessage> HttpPost1Async(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }
            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            _logger.LogInformation($"HttpHelper.Request.BODY:{parameters}");
            var response = await client.PostAsync(url, data);
            _logger.LogInformation($"HttpHelper.Request.Response:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }

        public async Task<HttpResponseMessage> HttpFromPostAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }
            _logger.LogInformation($"HttpHelper.Request.BODY:{JsonConvert.SerializeObject(parameters)}");
            using var data = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(parameters) };

            var response = await client.SendAsync(data);
            _logger.LogInformation($"HttpHelper.Request.Response:{response.Content.ReadAsStringAsync().Result}");
            return response;

        }

        public async Task<HttpResponseMessage> HttpGetAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType, int? port)
        {
            HttpClientHandler handler;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler
                {
                    Proxy = new WebProxy(proxyHost, proxyPort),
                    UseProxy = true,
                };
            }
            else
            {
                handler = new HttpClientHandler();
            }

            using var client = new HttpClient(handler);
            if (contentType != null)
            {
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(contentType));
            }

            if (headerParams != null)
            {
                foreach (var param in headerParams)
                {
                    client.DefaultRequestHeaders.Add(param.Key, param.Value);
                }
            }

            var builder = new UriBuilder(url)
            {
                Port = port ?? -1
            };

            var query = HttpUtility.ParseQueryString(builder.Query);
            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    query[param.Key] = param.Value;
                }
            }
            builder.Query = query.ToString();
            var newurl = builder.ToString();
            LogContext.PushProperty("HttpHelper.Request.URL", newurl);
            using var data = new HttpRequestMessage(HttpMethod.Get, newurl);
            var response = await client.SendAsync(data);
            _logger.LogInformation($"HttpHelper.Request.Response:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }

        public async Task<HttpResponseMessage> HttpPostWithX509(string url, string parameters, string contentType, Dictionary<string, string> headers = null, X509Certificate2 certificate = null)
        {
            using var handler = new HttpClientHandler();
            if (certificate != null)
            {
                handler.ClientCertificates.Add(new X509Certificate2(certificate));
            }

            handler.ServerCertificateCustomValidationCallback = delegate { return true; };

            using var client = new HttpClient(handler);
            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Content = new StringContent(parameters);
            httpRequestMessage.Method = HttpMethod.Post;
            httpRequestMessage.RequestUri = new Uri(url);
            httpRequestMessage.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue(contentType);

            if (headers != null)
            {
                foreach (var item in headers)
                {
                    client.DefaultRequestHeaders.Add(item.Key, item.Value ?? string.Empty);
                }
            }

            var response = await client.SendAsync(httpRequestMessage);

            _logger.LogInformation($"HttpHelper.Request.Response:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }

    }
}
