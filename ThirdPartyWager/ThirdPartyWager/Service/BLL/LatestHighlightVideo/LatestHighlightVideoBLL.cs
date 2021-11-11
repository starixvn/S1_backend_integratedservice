using System.Collections.Generic;
using ThirdPartyWager.Service.DAL;
using ThirdPartyWager.Service.Model;

namespace ThirdPartyWager.Service.BLL
{
    public class LatestHighlightVideoBLL
    {
        private readonly LatestHighlightVideoService latestHighlightVideoService;

        public LatestHighlightVideoBLL(LatestHighlightVideoService latestHighlightVideoService)
        {
            this.latestHighlightVideoService = latestHighlightVideoService;
        }
        public int SetLatestHighlightVideo(List<LatestHighlightVideoData> data)
        {
            return latestHighlightVideoService.SetLatestHighlightVideo(data);
        }
        public int GetLatestHighlightVideo(int SiteId, string Date, string Title)
        {
            return latestHighlightVideoService.GetLatestHighlightVideo(SiteId, Date, Title);
        }
    }
}
