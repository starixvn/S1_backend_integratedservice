using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class SabaOdds : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<SabaOdds> _logger;
        private readonly HttpHelper _httpHelper;
        private readonly SabaOddsBLL _sabaOddsBll;
        private Timer _timerSabaOdds;

        public SabaOdds(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<SabaOdds>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _sabaOddsBll = provider.GetRequiredService<SabaOddsBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _timerSabaOdds = new Timer(DoSabaOddsAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoSabaOddsAsync(object state)
        {
            bool isError = false;
            try
            {
                //string url = "http://portal.1100011.com/feeds/Nova88/odds.xml?sport=1,3&&league=UEFA%20EURO%202020&bettype=1";
                string url = "http://portal.1100011.com/feeds/Nova88/odds.xml?sport=1&bettype=1";

                UriBuilder builder = new UriBuilder(url)
                {
                    Port = -1
                };
                var query = HttpUtility.ParseQueryString(builder.Query);
                //query["action"] = "getmatch";
                //query["apiuser"] = "nvc";
                //query["key"] = "Gs6Am3Cn0C";
                //query["format"] = "JSON";
                builder.Query = query.ToString();
                string newurl = builder.ToString();

                var data = new HttpRequestMessage(HttpMethod.Get, newurl);
                var result = await _httpHelper.GetClient().SendAsync(data);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var Content = result.Content.ReadAsStringAsync().Result;
                    Content = Content.Replace("<bookie ", "<bookie xmlns:json=\"http://james.newtonking.com/projects/json\" ");
                    Content = Content.Replace("<odd ", "<odd json:Array=\"true\" ");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(Content);
                    string sabaOddsJson = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.None, true);
                    sabaOddsJson = sabaOddsJson.Replace("@", "");
                    var response = JsonConvert.DeserializeObject<GetSabaOddsJson>(sabaOddsJson);
                    
                    var updateList = response.Matches.match.Select(match => new UpdateOdd()
                    {
                        id = match.id, 
                        h = match.bookie.odd.FirstOrDefault()?.h,
                        o = match.bookie.odd.FirstOrDefault()?.o, 
                        u = match.bookie.odd.FirstOrDefault()?.u
                    });
                    var listMatchId = _sabaOddsBll.GetSabaOddsMatchId();
                    
                    var updateOdds = updateList.Where(x => listMatchId.Contains(x.id));
                    await _sabaOddsBll.SetSabaOdds(updateOdds);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, GetCurrentAsyncMethod()); 
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (_environment.IsDevelopment())
                {
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timerSabaOdds.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerSabaOdds?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
