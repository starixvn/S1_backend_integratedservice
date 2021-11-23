using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class LatestHighlightVideoBLL
    {
        private readonly LatestHighlightVideoService _latestHighlightVideoService;

        public LatestHighlightVideoBLL(IServiceProvider provider)
        {
            _latestHighlightVideoService = provider.GetRequiredService<LatestHighlightVideoService>();
        }
        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            return _latestHighlightVideoService.SetLatestHighlightVideo(data);
        }
        public int GetLatestHighlightVideo(int SiteId, string Date, string Title)
        {
            return _latestHighlightVideoService.GetLatestHighlightVideo(SiteId, Date, Title);
        }
    }
}
