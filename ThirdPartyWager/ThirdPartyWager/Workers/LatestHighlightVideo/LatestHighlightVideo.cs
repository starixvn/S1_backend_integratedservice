using System;
using System.Collections.Generic;
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
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;
using static ThirdPartyWager.Extensions.MethodExtension;

namespace ThirdPartyWager.Workers
{
    public class LatestHighlightVideo : BackgroundService
    {
        private Timer timerLatestHighlightVideo;
        private readonly IHostEnvironment environment;
        private readonly ILogger<LatestHighlightVideo> logger;
        private readonly Log2File log2File;
        private readonly HttpHelper httpHelper;
        private readonly CommonBLL commonBLL;
        private readonly LatestHighlightVideoBLL latestHighlightVideoBLL;
        private readonly IConfiguration _configuration;



        public LatestHighlightVideo(IServiceProvider provider)
        {
            environment = provider.GetRequiredService<IHostEnvironment>();
            logger = provider.GetRequiredService<ILogger<LatestHighlightVideo>>();
            log2File = provider.GetRequiredService<Log2File>();
            httpHelper = provider.GetRequiredService<HttpHelper>();
            commonBLL = provider.GetRequiredService<CommonBLL>();
            latestHighlightVideoBLL = provider.GetRequiredService<LatestHighlightVideoBLL>();
            _configuration = provider.GetRequiredService<IConfiguration>();
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            timerLatestHighlightVideo = new Timer(DoLatestHighlightVideoAsync, null, TimeSpan.Zero, TimeSpan.Zero);

            return Task.CompletedTask;
        }
        private async void DoLatestHighlightVideoAsync(object state)
        {
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
                using var client = httpHelper.GetClientWithHandler(CreateHandler());
                var result = await client.SendAsync(data);
                
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    IEnumerable<ServerInfo> siteList = commonBLL.GetSiteList().Where(x => x.SiteId == SiteId);
                    var LatestHighlightVideo = JsonSerializer.Deserialize<List<LatestHighlightVideoJson>>(result.Content.ReadAsStringAsync().Result).LastOrDefault();

                    if (siteList.Any() && LatestHighlightVideo != null)
                    {
                        foreach (var siteListInfo in siteList)
                        {

                            var check = latestHighlightVideoBLL.GetLatestHighlightVideo(siteListInfo.SiteId, LatestHighlightVideo.date, LatestHighlightVideo.title.en);

                            if (check == 0)
                            {
                                var LatestHighlightVideoData = new List<LatestHighlightVideoData>();

                                foreach (var siteListInfoDetail in JsonSerializer.Deserialize<IEnumerable<string>>(siteListInfo.LangList))
                                {
                                    if (!string.IsNullOrEmpty(LatestHighlightVideo.m3u8))
                                    {
                                        string title = siteListInfoDetail == "cs" ? LatestHighlightVideo.title.cs : LatestHighlightVideo.title.en;
                                        LatestHighlightVideoData.Add(new LatestHighlightVideoData()
                                        {
                                            SiteId = SiteId,
                                            LangId = siteListInfoDetail,
                                            Date = LatestHighlightVideo.date,
                                            Title = title,
                                            Link = $"/api/Player?url=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}&v={timestamp}",
                                            //Link = $"https://www.88novasoursop.com/api/Player?url=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}&v={timestamp}",
                                            //Link = $"https://89ksj.nova88.com/Streaming/HighlightsRecap?SportType=1&Title={title}&ShareUrl={LatestHighlightVideo.share}&ClipUrl=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}?v={timestamp}",
                                            Operator = "Service Auto Create"
                                        }); ;
                                    }
                                }

                                latestHighlightVideoBLL.SetLatestHighlightVideo(JsonSerializer.Serialize(LatestHighlightVideoData));
                            }
                            else
                            {
                                log2File.Error($"{siteListInfo.SiteId}, {LatestHighlightVideo.date}, {LatestHighlightVideo.title.en} {check}", "LatestHighlightVideo");
                            }
                        }
                    }
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
                TimeSpan dueTimeSpan = TimeSpan.FromMinutes(10);
                if (environment.IsDevelopment())
                {
                    //dueTimeSpan = TimeSpan.FromMinutes(1);
                    dueTimeSpan = TimeSpan.FromSeconds(30);
                }

                if (isError)
                {
                    dueTimeSpan = TimeSpan.FromSeconds(5);
                }

                timerLatestHighlightVideo.Change(dueTimeSpan, TimeSpan.Zero);
            }

        }
        public override Task StopAsync(CancellationToken cancellationToken)
        {
            timerLatestHighlightVideo?.Change(Timeout.Infinite, 0);
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
