using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/{merchant}")]
    public class SBSWController : ControllerBase
    {
        private readonly ILogger<SBSWController> _logger;

        private static ISBSWBLL _sbswBLL { get; set; }

        public SBSWController(IServiceProvider provider, ILogger<SBSWController> logger)
        {
            _logger = logger;
            _sbswBLL = new SBSWBLL(provider);
        }

       
        [HttpPost]
        [Route("{method}")]
        public async Task<dynamic> SeamlessWallet([FromRoute] string merchant, [FromRoute] string method)
        {
            try
            {
                if (!Enum.TryParse(method, ignoreCase: true, out SBSWTransType transType))
                {
                    return new ReturnObj(ReturnCode.InvalidParameters);
                }
                using var reader = new StreamReader(Request.Body);
                var bodyText = await reader.ReadToEndAsync();

                _logger.LogInformation($"bodyText1:{bodyText}");
                var acceptEncoding = GetHeaderValueAs<string>("Accept-Encoding");
                _logger.LogInformation($"AcceptEncoding:{acceptEncoding}");

                if (acceptEncoding.ToLower().Split(",").Contains("gzip"))
                {
                    var deompress = GZipDecompress(Encoding.UTF8.GetBytes(bodyText));
                    bodyText = Encoding.UTF8.GetString(deompress);
                    _logger.LogInformation($"deompress:{bodyText}");
                }                        
                
                return await _sbswBLL.SeamlessWallet(transType, merchant, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError);
            }
        }

        private byte[] GZipDecompress(byte[] data)
        {
            using var msi = new MemoryStream(data);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return mso.ToArray();
        }

        private T GetHeaderValueAs<T>(string headerName)
        {
            if (Request?.Headers?.TryGetValue(headerName, out StringValues values) ?? false)
            {
                string rawValues = values.ToString();

                if (!string.IsNullOrWhiteSpace(rawValues))
                {
                    return (T)Convert.ChangeType(values.ToString(), typeof(T));
                }
            }
            return default;
        }

    }
}
