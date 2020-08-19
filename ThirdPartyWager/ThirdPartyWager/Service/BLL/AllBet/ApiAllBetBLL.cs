using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Ocsp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiAllBetBLL
    {
        private static byte[] NULL_IV = Convert.FromBase64String("AAAAAAAAAAA=");
        private readonly IHttpClientFactory _clientFactory;
        private readonly HttpClient client;
        public ApiAllBetBLL(IHttpClientFactory clientFactory, IConfiguration configuration)
        {
            _clientFactory = clientFactory;

            if (configuration.GetValue<string>("ProxyHost") != null)
            {
                client = _clientFactory.CreateClient("with_proxy");
            }
            else
            {
                client = _clientFactory.CreateClient();
            }
        }

        public async Task<WagerAllBetModel> GetBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex)
        {
            // param
            string uri = "/betlog_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = encrypt(parameter, desKey, null);
            string sign = base64edMd5($"{data}{md5Key}");
            string param = $"data={System.Web.HttpUtility.UrlEncode(data)}&sign={ System.Web.HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);
            
            return result;
        }

        public async Task<WagerAllBetModel> GetModifiedBetListByTime(string apiUrl, string desKey, string md5Key, string propertyId, string startTime, string endTime, int pageIndex)
        {
            // param
            string uri = "/betlog_modified_histories";
            string url = $"{apiUrl}{uri}";
            int pageSize = 1000;
            string parameter = $"random={random()}&startTime={startTime}&endTime={endTime}&pageIndex={pageIndex}&pageSize={pageSize}";

            // encrype param
            string data = encrypt(parameter, desKey, null);
            string sign = base64edMd5($"{data}{md5Key}");
            string param = $"data={System.Web.HttpUtility.UrlEncode(data)}&sign={ System.Web.HttpUtility.UrlEncode(sign)}&propertyId={propertyId}";

            // request
            var r = await DoHttpClientAsync(url, param);
            var result = JsonConvert.DeserializeObject<WagerAllBetModel>(r);

            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, string param)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content.ReadAsStringAsync().Result;
            }
            else
            {
                return null;
            }
        }

        private string random()
        {
            using RNGCryptoServiceProvider csp = new RNGCryptoServiceProvider();
            byte[] byteCsp = new byte[5];
            csp.GetBytes(byteCsp);
            return BitConverter.ToString(byteCsp);
        }

        public static string encrypt(string data, string base64Key, string base64edIv)
        {
            TripleDESCryptoServiceProvider des = new TripleDESCryptoServiceProvider();
            des.Mode = CipherMode.CBC;
            des.BlockSize = 64;
            des.Padding = PaddingMode.PKCS7;
            des.Key = Convert.FromBase64String(base64Key);
            des.IV = base64edIv == null ? NULL_IV : Convert.FromBase64String(base64edIv);

            byte[] dataBytes = Encoding.UTF8.GetBytes(data);

            using MemoryStream stream = new MemoryStream();
            using CryptoStream encStream = new CryptoStream(stream, des.CreateEncryptor(), CryptoStreamMode.Write);
            encStream.Write(dataBytes, 0, dataBytes.Length);
            encStream.FlushFinalBlock();

            string result = Convert.ToBase64String(stream.ToArray());
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
            using MD5CryptoServiceProvider md5Crp = new MD5CryptoServiceProvider();
            return md5Crp.ComputeHash(data);
        }

    }
}
