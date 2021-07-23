using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

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

        public async Task<string> HttpPostAsync(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
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
            foreach (var param in headerParams)
            {
                client.DefaultRequestHeaders.Add(param.Key, param.Value);
            }
            _logger.LogInformation($"HttpHelper.Request.BODY:{parameters}");
            using var data = new StringContent(parameters, Encoding.UTF8, contentType);
            using var response = await client.PostAsync(url, data);

            string result = response.Content.ReadAsStringAsync().Result;
            _logger.LogInformation($"HttpHelper.Request.Repsone:{result}");
            return result;
        }

        public async Task<HttpResponseMessage> HttpPost1Async(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
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
            _logger.LogInformation($"HttpHelper.Request.Repsone:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }

        public async Task<HttpResponseMessage> HttpFromPostAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
            LogContext.PushProperty("HttpHelper.Request.URL", url);
            HttpClientHandler handler = null;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
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
            _logger.LogInformation($"HttpHelper.Request.Repsone:{response.Content.ReadAsStringAsync().Result}");
            return response;

        }

        public async Task<HttpResponseMessage> HttpGetAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
            HttpClientHandler handler;
            if (!string.IsNullOrEmpty(proxyHost))
            {
                handler = new HttpClientHandler()
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

            UriBuilder builder = new UriBuilder(url)
            {
                Port = -1
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
            string newurl = builder.ToString();
            LogContext.PushProperty("HttpHelper.Request.URL", newurl);
            using var data = new HttpRequestMessage(HttpMethod.Get, newurl);
            var response = await client.SendAsync(data);
            _logger.LogInformation($"HttpHelper.Request.Repsone:{response.Content.ReadAsStringAsync().Result}");
            return response;
        }
    }
}
