using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace CommonService.Service.Library
{
    public class HttpHelper
    {
        public static string proxyHost;
        public static int proxyPort;
        public HttpHelper()
        {

        }

        public async Task<string> HttpPostAsync(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
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
            var data = new StringContent(parameters, Encoding.UTF8, contentType);
            var response = await client.PostAsync(url, data);

            string result = response.Content.ReadAsStringAsync().Result;

            return result;
        }

        public async Task<HttpResponseMessage> HttpPost1Async(string url, Dictionary<string, string> headerParams, string parameters, string contentType)
        {
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
            var data = new StringContent(parameters, Encoding.UTF8, contentType);
            var response = await client.PostAsync(url, data);

            return response;
        }

        public async Task<HttpResponseMessage> HttpFromPostAsync(string url, Dictionary<string, string> headerParams, Dictionary<string, string> parameters, string contentType)
        {
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
            var data = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(parameters) };
            
            var response = await client.SendAsync(data);

            return response;
        }
    }
}
