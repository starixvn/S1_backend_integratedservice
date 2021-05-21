using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Serilog.Context;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiN2LiveBLL
    {
        private readonly HttpHelper httpHelper;
        private readonly Log2File log2File;
        private readonly ILogger<ApiN2LiveBLL> logger;

        public ApiN2LiveBLL(
            HttpHelper httpHelper,
            Log2File log2File,
            ILogger<ApiN2LiveBLL> logger
        )
        {
            this.httpHelper = httpHelper;
            this.log2File = log2File;
            this.logger = logger;
        }

        public async Task<WagerN2LiveModel> GetBetListByTime(string apiUrl, string vendorId, string merchantPassCode, string startTime, string endTime, string guid)
        {
            XmlDocument doc = new XmlDocument();

            // request
            XmlElement request = doc.CreateElement("request");
            request.SetAttribute("action", "gameinfo");

            // element
            XmlElement element = doc.CreateElement("element");

            // properties
            element.InnerXml = "" +
                "<properties name='vendorid'>" + vendorId + "</properties>" +
                "<properties name='merchantpasscode'>" + merchantPassCode + "</properties>" +
                "<properties name='startdate'>" + startTime + "</properties>" +
                "<properties name='enddate'>" + endTime + "</properties>" +
                "<properties name='timezone'>0</properties>" +
                "";

            // append
            doc.AppendChild(request);
            request.AppendChild(element);

            // request
            log2File.Info($"Guid:\r\n{guid}\r\nUrl:\r\n{apiUrl}\r\nRequest:\r\n{JsonConvert.SerializeObject(doc.InnerXml)}", Tp.N2Live.ToString());
            string r = await DoHttpClientAsync(apiUrl, doc.InnerXml);
            log2File.Info($"Guid:\r\n{guid}\r\nUrl:\r\n{apiUrl}\r\nResponse:\r\n{r}", Tp.N2Live.ToString());
            var result = JsonConvert.DeserializeObject<WagerN2LiveModel>(r);
            return result;
        }

        private async Task<string> DoHttpClientAsync(string url, object param)
        {
           

            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param.ToString(), Encoding.UTF8, "application/xml");
            request.Headers.Add("Accept-Encoding", "gzip");

            LogContext.PushProperty("TpId", Tp.N2Live.ToString());
            logger.LogInformation($"URL {url}");
            logger.LogInformation($"REQUEST: {param.ToString()}");
            logger.LogInformation($"HEADER: {request.Headers}");

            HttpResponseMessage response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync();
                using var decompressed = new GZipStream(responseStream, CompressionMode.Decompress);
                using var sr = new StreamReader(decompressed);
                var output = await sr.ReadToEndAsync();

                // single data force to array
                output = output.Replace("<deal ", "<deal xmlns:json='http://james.newtonking.com/projects/json' json:Array='true' ");
                output = output.Replace("<betdetail ", "<betdetail xmlns:json='http://james.newtonking.com/projects/json' json:Array='true' ");
                output = output.Replace("<clientbet ", "<clientbet xmlns:json='http://james.newtonking.com/projects/json' json:Array='true' ");

                // xml to json
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(output);

                // single data force to array
                var eleTag = xmlDoc.GetElementsByTagName("game");
                if (eleTag.Count == 1)
                {
                    var attribute = xmlDoc.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
                    attribute.InnerText = "true";
                    var node = eleTag.Item(0) as XmlElement;
                    node.Attributes.Append(attribute);
                }

                string json = JsonConvert.SerializeXmlNode(xmlDoc);
                logger.LogInformation($"RESPONSE: {xmlDoc.InnerXml}");
                return json;
            }
            else
            {
                logger.LogInformation($"RESPONSE: null");
                return null;
            }
        }
    }
}
