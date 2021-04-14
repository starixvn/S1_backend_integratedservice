using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model;

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



        public LatestHighlightVideo(IHostEnvironment environment, ILogger<LatestHighlightVideo> logger, Log2File log2File, HttpHelper httpHelper, CommonBLL commonBLL, LatestHighlightVideoBLL latestHighlightVideoBLL)
        {
            this.environment = environment;
            this.logger = logger;
            this.log2File = log2File;
            this.httpHelper = httpHelper;
            this.commonBLL = commonBLL;
            this.latestHighlightVideoBLL = latestHighlightVideoBLL;
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
                var result = await httpHelper.GetClient().SendAsync(data);
                var LatestHighlightVideoData = new List<LatestHighlightVideoData>();
                if (result.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    IEnumerable<ServerInfo> siteList = commonBLL.GetSiteList().Where(x => x.SiteId == SiteId);
                    var LatestHighlightVideo = JsonSerializer.Deserialize<List<LatestHighlightVideoJson>>(result.Content.ReadAsStringAsync().Result).LastOrDefault();

                    if (siteList.Any() && siteList != null && LatestHighlightVideo != null)
                    {
                        foreach (var siteListInfo in siteList)
                        {

                            var check = latestHighlightVideoBLL.GetLatestHighlightVideo(siteListInfo.SiteId, LatestHighlightVideo.date, LatestHighlightVideo.title.en);

                            if (check == 0)
                            {
                                foreach (var siteListInfoDetail in JsonSerializer.Deserialize<IEnumerable<string>>(siteListInfo.LangList))
                                {
                                    string title = siteListInfoDetail == "cs" ? LatestHighlightVideo.title.cs : LatestHighlightVideo.title.en;
                                    LatestHighlightVideoData.Add(new LatestHighlightVideoData()
                                    {
                                        SiteId = SiteId,
                                        LangId = siteListInfoDetail,
                                        Date = LatestHighlightVideo.date,
                                        Title = title,
                                        Link = $"https://89ksj.nova88.com/Streaming/HighlightsRecap?SportType=1&Title={title}&ShareUrl={LatestHighlightVideo.share}&ClipUrl=https://glombi.accordde.com/video/{LatestHighlightVideo.m3u8}?v={timestamp}",
                                        Operator = "Service Auto Create"
                                    }); ;
                                }
                            }
                        }
                        latestHighlightVideoBLL.SetLatestHighlightVideo(JsonSerializer.Serialize(LatestHighlightVideoData));
                    }
                }
            }
            catch (Exception ex)
            {
                isError = true;
                logger.LogError(ex.Message);
                log2File.Error(ex.ToString(), "LatestHighlightVideo");
            }
            finally
            {
                TimeSpan dueTimeSpan = TimeSpan.FromHours(1);
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
    }
}
