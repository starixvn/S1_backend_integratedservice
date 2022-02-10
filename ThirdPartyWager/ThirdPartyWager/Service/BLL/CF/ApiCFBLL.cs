using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class ApiCFBLL
    {
        private readonly HttpHelper httpHelper;

        public ApiCFBLL(HttpHelper httpHelper)
        {
            this.httpHelper = httpHelper;
        }
        public async Task<WagerCFModel> GetBetListByTime(string apiUrl, string apiKey, string agentCode, string startTime, string endTime)
        {
            // param
            var uri = "/get_cockfight_processed_ticket_2.aspx";
            var url = $"{apiUrl}{uri}";
            var param = $"api_key={apiKey}&agent_code={agentCode}&start_datetime={startTime}&end_datetime={endTime}";

            // request
            var r = await DoHttpClientAsync(url, param, "data");
            var result = JsonConvert.DeserializeObject<WagerCFModel>(r);
            return result;
        }

        public async Task<WagerCFModel> GetBetSummaryByTime(string apiUrl, string apiKey, string agentCode, string checkDate)
        {
            // param
            var uri = "/get_cockfight_player_summary.aspx";
            var url = $"{apiUrl}{uri}";
            var param = $"api_key={apiKey}&agent_code={agentCode}&match_date={checkDate}";

            // request
            var r = await DoHttpClientAsync(url, param, "data");
            var result = JsonConvert.DeserializeObject<WagerCFModel>(r);
            return result;
        }


        private async Task<string> DoHttpClientAsync(string url, string param, string tagName)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Content = new StringContent(param, Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = await httpHelper.GetClient().SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = response.Content.ReadAsStringAsync().Result;

                // xml to json
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(result);

                // single data force to array
                var eleTag = xmlDoc.GetElementsByTagName(tagName);
                if (eleTag.Count == 1)
                {
                    var attribute = xmlDoc.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
                    attribute.InnerText = "true";
                    var node = eleTag.Item(0) as XmlElement;
                    node.Attributes.Append(attribute);
                }

                var json = JsonConvert.SerializeXmlNode(xmlDoc);
                return json;
            }
            else
            {
                return null;
            }
        }

    }
}