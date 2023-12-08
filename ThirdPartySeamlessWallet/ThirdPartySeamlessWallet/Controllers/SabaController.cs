using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Newtonsoft.Json;

using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.DAL;
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
        private readonly PartitionService _partitionService;
        public Tp ThirdParty { get; set; }

        public SabaController(IServiceProvider provider)
        {
            _sabaSecretKey = provider.GetRequiredService<ConfigBLL>().GetConfigValue("SabaSecretKey");
            _logger = provider.GetRequiredService<ILogger<SabaController>>();
            _thirdPartyBll = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tools = provider.GetRequiredService<Tools>();
            ThirdParty = Tp.SBSW;
            _partitionService = provider.GetRequiredService<PartitionService>();
        }

        Dictionary<string, int> siteIdDic = new Dictionary<string, int>() {
            {"N88",1 },
            {"N8G",2 },
            {"N8B",3 },
            {"N8I",5 }
        };

        Dictionary<int, string> counrtyDic = new Dictionary<int, string>() {
            {2,"Malaysia" },
            {3,"United States of America" },
            {4,"Thailand" },
            {13,"China" },
            {15,"Indonesia" },
            {51,"Viet Nam" },
            {61,"India" },
            {72,"Cambodia" },
            {74,"Bangladesh" },
        };

        #region CheckOnlineMember

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

        #endregion

        #region GetUserData

        [HttpPost]
        public IEnumerable<FinancialLog> GetFinancialLog(GetDataInput input)
        {
            var result = Enumerable.Empty<FinancialLog>();
            try
            {
                int siteId = 0;
                siteIdDic.TryGetValue(input.SiteId, out siteId);

                var custProfile = _tpCustInfoBLL.GetCustProfileWithCacheByUserName(siteId, input.UserName);

                //Search range table
                var parameters = new GetFinancialLogSpInput(custProfile.CustId);
                result = _partitionService
                    .MonthlyQuery<FinancialLog>(parameters.StartDate, parameters.EndDate, "Service_FinancialLog_Get", parameters)
                    .Where(log => log.FinancialType == FinancialType.Deposit || log.FinancialType == FinancialType.Withdrawal);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"GetFinancialLog Exception:{e.Message}");
            }

            return result;
        }

        #endregion

        #region GetUserData
        [HttpPost]
        public CustInfo GetCustInfo(GetDataInput input)
        {
            var result = new CustInfo();
            try
            {
                int siteId = 0;
                siteIdDic.TryGetValue(input.SiteId, out siteId);
                var custProfile = _tpCustInfoBLL.GetCustProfileWithCacheByUserName(siteId, input.UserName);

                var nationality = string.Empty;
                counrtyDic.TryGetValue(custProfile.CurrencyId, out nationality);

                var dateOfBirth = string.Empty;
                if (custProfile.Birth_y != null && custProfile.Birth_m != null && custProfile.Birth_d != null)
                {
                    dateOfBirth = $"{custProfile.Birth_y}/{custProfile.Birth_m}/{custProfile.Birth_d}";
                }
                result.RegisteredName = custProfile.UserName;
                result.Email = custProfile.Email;
                result.Mobile = $"({custProfile.AreaCode}){custProfile.PhoneNo}";
                result.Active = custProfile.Status == CustStatus.Running;
                result.Locked = custProfile.Status == CustStatus.Lock;
                result.DateOfRegistration = custProfile.RegTime;
                result.RegistrationIP = custProfile.SignupIp;
                result.CurrentAddress = custProfile.CurrentAddress;
                result.DateOfBirth = dateOfBirth;
                result.Nationality = nationality;
                result.Occupation = GetOccupation(custProfile.Occupation);

            }
            catch (Exception e)
            {
                _logger.LogError(e, $"GetCustProfile Exception:{e.Message}");
            }

            return result;
        }
        #endregion

        private string GetOccupation(Occupation occupation)
        {
            return occupation switch
            {
                Occupation.SelfEmployed => "Self Employed",
                Occupation.Employed => "Employed",
                Occupation.Retired => "Retired",
                Occupation.Others => "Others",
                _ => string.Empty
            };
        }

    }
}