using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace ThirdPartySeamlessWallet.Extensions.ActionFilters
{
    public class GzipFilter : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            using var reader = new StreamReader(context.HttpContext.Request.Body);
            var rawText = reader.ReadToEnd();
            var compress = GZipCompress(rawText);
            var deompress = GZipDecompress(compress);
            context.HttpContext.Request.Body = new MemoryStream(deompress);
        }

        private byte[] GZipCompress(string data)
        {
            using var msi = new MemoryStream(Encoding.UTF8.GetBytes(data));
            using var mso = new MemoryStream();
            using (var gs = new GZipStream(mso, CompressionMode.Compress))
            {
                msi.CopyTo(gs);
            }
            return mso.ToArray();
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
    }
}