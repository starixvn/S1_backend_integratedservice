using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class LiveStreaming : BackgroundService
    {
        private readonly IHostEnvironment _environment;
        private readonly ILogger<LiveStreaming> _logger; 
        private readonly HttpHelper _httpHelper;
        private readonly LiveStreamingBLL _liveStreamingBll;
        private Timer _timerLiveStreaming;
        private readonly string _tpName = "LiveStreaming";

        public LiveStreaming(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<LiveStreaming>>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _liveStreamingBll = provider.GetRequiredService<LiveStreamingBLL>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _timerLiveStreaming = new Timer(DoLiveStreamingAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoLiveStreamingAsync(object state)
        {
            LogContext.PushProperty("RequestId", Guid.NewGuid().ToString());
            LogContext.PushProperty("Tp", _tpName);
            var isError = false;
            try
            {
                var url = "https://www.glivestreaming.com/api.php";

                var builder = new UriBuilder(url)
                {
                    Port = -1
                };
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["action"] = "getmatch";
                query["apiuser"] = "nvc";
                query["key"] = "lnQzpXfTvn";
                query["format"] = "JSON";
                builder.Query = query.ToString();
                var newurl = builder.ToString();

                var data = new HttpRequestMessage(HttpMethod.Get, newurl);
                var result = await _httpHelper.GetClient().SendAsync(data);

                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    _liveStreamingBll.TruncateLiveStreaming();
                    var resultModel = JsonSerializer.Deserialize<GetLiveStreaming>(result.Content.ReadAsStringAsync().Result);
                        await _liveStreamingBll.SetLiveStreaming(resultModel.Match);
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, GetCurrentAsyncMethod());
            }
            finally
            {
                var dueTimeSpan = TimeSpan.FromMinutes(1);
                if (_environment.IsDevelopment())
                {
                    //dueTimeSpan = TimeSpan.FromMinutes(1);
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timerLiveStreaming.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLiveStreaming?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }
    }
}
