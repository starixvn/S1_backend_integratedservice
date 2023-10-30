using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

using static ThirdPartySeamlessWallet.Extensions.MethodExtension;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SabaController : ControllerBase
    {
        private readonly ILogger<SabaController> _logger;
        private readonly Tools _tools;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly string _sabaSecretKey;
        public Tp ThirdParty { get; set; }

        public SabaController(IServiceProvider provider)
        {
            _sabaSecretKey = provider.GetRequiredService<ConfigBLL>().GetConfigValue("SabaSecretKey");
            _logger = provider.GetRequiredService<ILogger<SabaController>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tools = provider.GetRequiredService<Tools>();
            ThirdParty = Tp.SBSW;
        }

        [HttpPost]
        public ContentResult CheckOnlineMember([FromForm] CheckOnlineMemberInput input)
        {

            try
            {
                if (input.Secret_Key != _sabaSecretKey)
                {
                    return ReturnXML(GetOutput(SabaOnlineMemberStatus.InvalidSecretKeyOrIpAddressNotAllow));
                }

                var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, input.Vendor_Member_Id);
                if (tpCustInfo == null || tpCustInfo.CustId == 0)
                {
                    return ReturnXML(GetOutput(SabaOnlineMemberStatus.VendorMemberIdNotFound));
                }

                var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)ThirdParty, input.Vendor_Member_Id);
                if (tpCheck == null)
                {
                    return ReturnXML(GetOutput(SabaOnlineMemberStatus.MemberIsOffline));
                }

                _tpCustInfoBLL.UpdateOnline(tpCustInfo.CustId, true, tpCheck.Token, tpCheck.IP);
                return ReturnXML(GetOutput(SabaOnlineMemberStatus.MemberIsOnline));
            }
            catch (Exception e)
            {
                _logger.LogError(e, GetCurrentMethod());
                var error = GetOutput(SabaOnlineMemberStatus.InvalidSecretKeyOrIpAddressNotAllow, $"System Error:{e.Message}");
                return ReturnXML(error, 403);
            }
        }

        private static CheckOnlineMemberOutput GetOutput(SabaOnlineMemberStatus status, string message = null)
        {
            return new CheckOnlineMemberOutput
            {
                CheckOnlineMember = new CheckOnlineMember
                {
                    Version = "2.0",
                    StatusCode = (int)status,
                    Message = message ?? status.GetEnumDescription()
                }
            };
        }

        private ContentResult ReturnXML(CheckOnlineMemberOutput output, int statusCode = 200)
        {
            var json = JsonConvert.SerializeObject(output);
            return new ContentResult()
            {
                Content = _tools.TransformJsonToXml<CheckOnlineMember>(json),
                ContentType = "application/xml",
                StatusCode = statusCode
            };
        }
    }
}