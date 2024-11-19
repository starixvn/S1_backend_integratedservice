namespace ThirdPartyService.Service.Library
{
    public class Common
    {
        private readonly IWebHostEnvironment environment;

        public Common(IWebHostEnvironment environment)
        {
            this.environment = environment;
        }

        public string GetEnv()
        {
            switch (environment.EnvironmentName)
            {
                case "Development":
                case "LOCAL":
                    return "D";

                case "SIT":
                    return "S";

                case "UAT":
                    return "U";

                case "PROD":
                    return "P";

                default:
                    return "D";
            }
        }
    }
}