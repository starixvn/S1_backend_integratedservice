using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Extensions.Configuration;
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
    public class LatestHighlightVideo : BackgroundService
    {
        private Timer _timerLatestHighlightVideo;
        private readonly IHostEnvironment _environment;
        private readonly ILogger<LatestHighlightVideo> _logger;
        private readonly Log2File _log2File;
        private readonly HttpHelper _httpHelper;
        private readonly CommonBLL _commonBll;
        private readonly LatestHighlightVideoBLL _latestHighlightVideoBll;
        private readonly IConfiguration _configuration;
        private readonly string _tpName = "LatestHighlightVideo";

        public LatestHighlightVideo(IServiceProvider provider)
        {
            _environment = provider.GetRequiredService<IHostEnvironment>();
            _logger = provider.GetRequiredService<ILogger<LatestHighlightVideo>>();
            _log2File = provider.GetRequiredService<Log2File>();
            _httpHelper = provider.GetRequiredService<HttpHelper>();
            _commonBll = provider.GetRequiredService<CommonBLL>();
            _latestHighlightVideoBll = provider.GetRequiredService<LatestHighlightVideoBLL>();
            _configuration = provider.GetRequiredService<IConfiguration>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            _timerLatestHighlightVideo = new Timer(DoLatestHighlightVideoAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoLatestHighlightVideoAsync(object state)
        {
            LogContext.PushProperty("Tp", _tpName);
            bool isError = false;
            try
            {
                string url = "https://glombi.accordde.com/video/soccerList.json";
                string timestamp = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
                int SiteId = 1;
                UriBuilder builder = new UriBuilder(url)
                {
                    Port = -1
                };
                var query = HttpUtility.ParseQueryString(builder.Query);
                query["v"] = timestamp;
                builder.Query = query.ToString();
                string newurl = builder.ToString();
                var data = new HttpRequestMessage(HttpMethod.Get, newurl);
                using var client = _httpHelper.GetClientWithHandler(CreateHandler());
                var result = await client.SendAsync(data);
                
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    IEnumerable<ServerInfo> siteList = _commonBll.GetSiteList().Where(x => x.SiteId == SiteId);
                    var latestHighlightVideo = JsonSerializer.Deserialize<List<LatestHighlightVideoJson>>(result.Content.ReadAsStringAsync().Result).LastOrDefault();

                    if (siteList.Any() && latestHighlightVideo != null)
                    {
                        foreach (var siteListInfo in siteList)
                        {
                            var check = _latestHighlightVideoBll.GetLatestHighlightVideo(siteListInfo.SiteId, latestHighlightVideo.date, latestHighlightVideo.title.en);

                            if (check == 0)
                            {
                                var sw = new Stopwatch();
                                sw.Start();
                                var LatestHighlightVideoData = new List<LatestHighlightVideoData>();

                                foreach (var siteListInfoDetail in JsonSerializer.Deserialize<IEnumerable<string>>(siteListInfo.LangList))
                                {
                                    if (!string.IsNullOrEmpty(latestHighlightVideo.m3u8))
                                    {
                                        string title = siteListInfoDetail == "cs" ? latestHighlightVideo.title.cs : latestHighlightVideo.title.en;
                                        LatestHighlightVideoData.Add(new LatestHighlightVideoData()
                                        {
                                            SiteId = SiteId,
                                            LangId = siteListInfoDetail,
                                            Date = latestHighlightVideo.date,
                                            Title = title,
                                            Link = $"/api/Player?url=https://glombi.accordde.com/video/{latestHighlightVideo.m3u8}&v={timestamp}",
                                            //Link = $"https://www.88novasoursop.com/api/Player?url=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}&v={timestamp}",
                                            //Link = $"https://89ksj.nova88.com/Streaming/HighlightsRecap?SportType=1&Title={title}&ShareUrl={LatestHighlightVideo.share}&ClipUrl=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}?v={timestamp}",
                                            Operator = "Service Auto Create"
                                        }); ;
                                    }
                                }

                                _latestHighlightVideoBll.SetLatestHighlightVideo(LatestHighlightVideoData);
                                
                                sw.Stop();
                                _logger.LogInformation($"End to SetLatestHighlightVideo ,CostTime :{sw.Elapsed:c}", GetCurrentAsyncMethod());
                            }
                            else
                            {
                                _log2File.Error($"{siteListInfo.SiteId}, {latestHighlightVideo.date}, {latestHighlightVideo.title.en} {check}", "LatestHighlightVideo");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                _logger.LogError(ex, GetCurrentAsyncMethod());
                _log2File.Error(ex.ToString(), GetCurrentAsyncMethod());
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(10);
                if (_environment.IsDevelopment())
                {
                    //dueTimeSpan = TimeSpan.FromMinutes(1);
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                _timerLatestHighlightVideo.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _timerLatestHighlightVideo?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        private HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
            if (_configuration.GetValue<string>("ProxyHost") != null)
            {
                handler.UseProxy = true;
                handler.Proxy = new WebProxy(_configuration.GetValue<string>("ProxyHost"),
                    _configuration.GetValue<int>("ProxyPort"));
                handler.ServerCertificateCustomValidationCallback = delegate { return true; };
            }

            return handler;
        }
    }
}
