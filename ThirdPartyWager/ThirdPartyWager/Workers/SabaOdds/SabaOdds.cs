using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class SabaOdds : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<SabaOdds> logger;
        private readonly Log2File log2File;
        private readonly HttpHelper httpHelper;
        private readonly SabaOddsBLL sabaOddsBLL;
        private Timer timerSabaOdds;

        public SabaOdds(IHostEnvironment environment, ILogger<SabaOdds> logger, Log2File log2File, HttpHelper httpHelper, SabaOddsBLL sabaOddsBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.log2File = log2File;
            this.httpHelper = httpHelper;
            this.sabaOddsBLL = sabaOddsBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerSabaOdds = new Timer(DoSabaOddsAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoSabaOddsAsync(object state)
        {
            bool isError = false;
            try
            {
                string url = "http://portal.1100011.com/feeds/Nova88/odds.xml?sport=1,3&&league=UEFA%20EURO%202020&bettype=1";

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
                var result = await httpHelper.GetClient().SendAsync(data);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var Content = result.Content.ReadAsStringAsync().Result;
                    Content = Content.Replace("<bookie ", "<bookie xmlns:json=\"http://james.newtonking.com/projects/json\" ");
                    Content = Content.Replace("<odd ", "<odd json:Array=\"true\" ");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(Content);
                    string sabaOddsJson = JsonConvert.SerializeXmlNode(xmlDoc, Newtonsoft.Json.Formatting.None, true);
                    sabaOddsJson = sabaOddsJson.Replace("@", "");
                    //GetSabaOddsJson response = JsonConvert.DeserializeObject<GetSabaOddsJson>(json);
                    //var b = "";
                    sabaOddsBLL.SetSabaOdds(sabaOddsJson);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                logger.LogError(ex, GetCurrentAsyncMethod());
                log2File.Error(ex.ToString(), GetCurrentAsyncMethod());
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(1);
                if (environment.IsDevelopment())
                {
                    //dueTimeSpan = TimeSpan.FromMinutes(1);
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timerSabaOdds.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerSabaOdds?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
