using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.Model.SBSW;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/{merchant}")]
    [Produces("application/json")]
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

                var bodyText = string.Empty;
                if (GetHeaderValueAs<string>("Content-Encoding")?.Contains("gzip") ?? false)
                {
                    using var ms = new MemoryStream(2048);
                    await Request.Body.CopyToAsync(ms);
                    bodyText = GZipDecompress(ms.ToArray());
                }
                else
                {
                    using var reader = new StreamReader(Request.Body);
                    bodyText = await reader.ReadToEndAsync();
                }

                return await _sbswBLL.SeamlessWallet(merchant, transType, bodyText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return new ReturnObj(ReturnCode.UnknownError);
            }
        }

        private string GZipDecompress(byte[] data)
        {
            using var msi = new MemoryStream(data);
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(msi, CompressionMode.Decompress))
            {
                gs.CopyTo(mso);
            }
            return Encoding.UTF8.GetString(mso.ToArray());
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
