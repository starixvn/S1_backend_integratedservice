using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]

    public class EVOController : ControllerBase
    {
        private readonly ILogger<EVOController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingBLL _bettingBLL;
        private readonly SettledBLL _settledBLL;
        private readonly Log2File _log2File;
        private readonly Tools _tools;
        private readonly Tp thisTp;

        public EVOController(
            IServiceProvider provider,
            ILogger<EVOController> logger,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _log2File = provider.GetRequiredService<Log2File>();
            _configuration = configuration;
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _bettingBLL = provider.GetRequiredService<BettingBLL>();
            _settledBLL = provider.GetRequiredService<SettledBLL>();
            _tools = provider.GetRequiredService<Tools>();
            thisTp = Tp.EVO;
        }

        #region HttpStatusCode

        public enum Error
        {
            OK = 200,
            INVALID_DATA = 400,
            UAUTHORIZED = 401,
            NotFound = 404,
            ERROR = 500,
        }

        #endregion

        #region Request



        #endregion

        #region Response



        #endregion

        [HttpPost]
        [HttpGet]
        [Route("[controller]/{path1?}")]
        public async Task<dynamic> NotFoundAsync()
        {
            string logName = $"{thisTp}_NotFound";

            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var Content = await reader.ReadToEndAsync();
            _log2File.Error(Content, logName);
            Response.StatusCode = (int)Error.NotFound;
            return 404;
        }
    }
}
