using ThirdPartyWager.Service.DAL;

namespace ThirdPartyWager.Service.BLL
{
    public class LatestHighlightVideoBLL
    {
        private readonly LatestHighlightVideoService latestHighlightVideoService;

        public LatestHighlightVideoBLL(LatestHighlightVideoService latestHighlightVideoService)
        {
            this.latestHighlightVideoService = latestHighlightVideoService;
        }
        public int SetLatestHighlightVideo(string latestHighlightVideoJson)
        {
            return latestHighlightVideoService.SetLatestHighlightVideo(latestHighlightVideoJson);
        }
        public int GetLatestHighlightVideo(int SiteId, string Date, string Title)
        {
            return latestHighlightVideoService.GetLatestHighlightVideo(SiteId, Date, Title);
        }
    }
}
