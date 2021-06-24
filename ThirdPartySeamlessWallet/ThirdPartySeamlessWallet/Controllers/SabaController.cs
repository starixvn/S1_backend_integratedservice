using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class SabaController : ControllerBase
    {
        private readonly ILogger<SabaController> _logger;
        private static SabaBLL _sabaBLL { get; set; }
        private readonly Tools _tools;
        private readonly ThirdPartyBLL _thirdPartyBll;
        private readonly string _sabaSecretKey;
        private const string Version = "2.0";

        public SabaController(IServiceProvider provider)
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            _sabaSecretKey = configuration.GetValue<string>("SabaSecretKey");
            _logger = provider.GetRequiredService<ILogger<SabaController>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _sabaBLL = new SabaBLL(provider);
            _tools = provider.GetRequiredService<Tools>();
        }

        [HttpPost]
        public async Task<ContentResult> CheckOnlineMember(CheckOnlineMemberInput input)
        {
            string json;
            if (input.Secret_Key != _sabaSecretKey)
            {
                json = JsonConvert.SerializeObject(new CheckOnlineMemberOutput
                {
                    CheckOnlineMember = new CheckOnlineMember{
                        Version = Version,
                        StatusCode = (int)SabaOnlineMemberStatus.InvalidSecretKeyOrIpAddressNotAllow,
                        Message = SabaOnlineMemberStatus.InvalidSecretKeyOrIpAddressNotAllow.ToString()
                    }
                });
            }
            else
            {
                var tpCustInfo = _sabaBLL.GetTpInfo(input.Vendor_Member_Id);
                if (tpCustInfo.CustId == 0)
                {
                    json = JsonConvert.SerializeObject(new CheckOnlineMemberOutput
                    {
                        CheckOnlineMember = new CheckOnlineMember{
                            Version = Version,
                            StatusCode = (int)SabaOnlineMemberStatus.VendorMemberIdNotFound,
                            Message = SabaOnlineMemberStatus.VendorMemberIdNotFound.ToString()
                        }
                    });
                }
                else
                {
                    var tpCheck = _thirdPartyBll.ThirdPartyCheckWithCache((int)Tp.Saba, input.Vendor_Member_Id);
                    if (tpCheck == null)
                    {
                        json = JsonConvert.SerializeObject(new CheckOnlineMemberOutput
                        {
                            CheckOnlineMember = new CheckOnlineMember{
                                Version = Version,
                                StatusCode = (int)SabaOnlineMemberStatus.MemberIsOffline,
                                Message = SabaOnlineMemberStatus.MemberIsOffline.ToString()
                            }
                        });
                    }
                    else
                    {
                        _sabaBLL.UpdateOnline(tpCustInfo.CustId, true, tpCheck.Token, tpCheck.IP);
                        json = JsonConvert.SerializeObject(new CheckOnlineMemberOutput
                        {
                            CheckOnlineMember = new CheckOnlineMember
                            {
                                Version = Version,
                                StatusCode = (int) SabaOnlineMemberStatus.MemberIsOnline,
                                Message = SabaOnlineMemberStatus.MemberIsOnline.ToString()
                            }
                        });
                    }
                }
            }

            var result = _tools.TransformJsonToXml<CheckOnlineMember>(json); 
            return new ContentResult() { Content = result, ContentType = "application/xml", StatusCode = 200 };
        }
    }
}