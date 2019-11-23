using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartyCore.Model;

namespace ThirdPartyService.Service.BLL
{
    public class TpCustInfoBLL
    {
        private readonly IConfiguration configuration;

        public TpCustInfoBLL(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

    }
}
