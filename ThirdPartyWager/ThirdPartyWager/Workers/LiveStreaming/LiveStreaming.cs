using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class LiveStreaming : BackgroundService
    {
        private readonly IHostEnvironment environment;
        private readonly ILogger<LiveStreaming> logger;
        private readonly Log2File log2File;
        private readonly HttpHelper httpHelper;
        private readonly LiveStreamingBLL liveStreamingBLL;
        private Timer timerLiveStreaming;

        public LiveStreaming(IHostEnvironment environment, ILogger<LiveStreaming> logger, Log2File log2File, HttpHelper httpHelper, LiveStreamingBLL liveStreamingBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.log2File = log2File;
            this.httpHelper = httpHelper;
            this.liveStreamingBLL = liveStreamingBLL;
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerLiveStreaming = new Timer(DoLiveStreamingAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoLiveStreamingAsync(object state)
        {
            bool isError = false;
            try
            {
                string url = "https://www.glivestreaming.com/api.php";

                UriBuilder builder = new UriBuilder(url)
                {
                    Port = -1
                };
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["action"] = "getmatch";
                query["apiuser"] = "nvc";
                query["key"] = "lnQzpXfTvn";
                query["format"] = "JSON";
                builder.Query = query.ToString();
                string newurl = builder.ToString();

                var data = new HttpRequestMessage(HttpMethod.Get, newurl);
                var result = await httpHelper.GetClient().SendAsync(data);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    liveStreamingBLL.SetLiveStreaming(JsonSerializer.Serialize(JsonSerializer.Deserialize<GetLiveStreaming>(result.Content.ReadAsStringAsync().Result)));
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

                timerLiveStreaming.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerLiveStreaming?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
