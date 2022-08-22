namespace ThirdPartyWager.Service.DAL
{
    public class WagerSkywindService
    {
        private readonly IConfiguration configuration;

        public WagerSkywindService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }
    }
}
