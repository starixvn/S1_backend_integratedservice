using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
            if (environment.EnvironmentName == "Development")
            {
                return "D";
            }
            else if (environment.EnvironmentName == "Staging")
            {
                return "S";
            }
            else if (environment.EnvironmentName == "Production")
            {
                return "P";
            }
            else if (environment.EnvironmentName == "QA")
            {
                return "Q";
            }
            return "X";
        }
    }
}
