using ThirdPartyService.Service.Model;

namespace ThirdPartyService.Service.Library
{
    public class Common
    {
        private readonly IConfiguration _configuration;

        public Common(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GetEnv()
        {
            var envName = GetEnvironmentName();
            switch (envName)
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

        public string GetEnvironmentName()
        {
            return _configuration.GetValue<string>("EnvironmentName");
        }

        public bool IsDevelopmentEnvironment()
        {
            return GetEnvironmentName().Equals("Development");
        }

        public bool IsProdEnvironment()
        {
            return GetEnvironmentName().Equals("PROD");
        }

        public ServiceHostConfig GetServiceHostConfig()
        {
            var serviceHostConfig = _configuration.GetSection("serviceHosts").Get<ServiceHostConfig>();
            return serviceHostConfig;
        }
    }
}