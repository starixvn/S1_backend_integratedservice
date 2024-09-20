using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ThirdPartySeamlessWallet.Service.BLL;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Controllers
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class MigrationController : ControllerBase
    {
        private readonly MigrationBLL _migrationBll;
        private readonly Log2File _log2File;
        private readonly Dictionary<int, MigrationConfig> _siteConfigDic;
        private const string LogName = "MigrationController";
        private readonly Dictionary<int, int> _agentIdList;

        public MigrationController(IServiceProvider provider)
        {
            _migrationBll = provider.GetRequiredService<MigrationBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MigrationConfig>("Migration");
            _agentIdList = new Dictionary<int, int>
            {
                { 2, 1 },
                { 3, 2 },
                { 4, 3 },
                { 13, 4 },
                { 15, 5 },
                { 45, 6 },
                { 51, 7 },
                { 61, 8 },
                { 72, 9 },
                { 74, 10 }
            };
        }

        [HttpPost]
        public async Task<MigrationCustomerResponse> MigrationCustomerAsync(MigrationCustomerRequest request)
        {
            var resp = new MigrationCustomerResponse
            {
                CustId = 0,
                Message = "Unknown Error",
                ErrorCode = 9000
            };

            _siteConfigDic.TryGetValue(request.SiteId , out var config);
            if (config is null)
            {
                resp.Message = "SiteId Not Supported. ";
                return resp;
            }
            
            Request.Headers.TryGetValue("Authorization", out var authorization);
            var sAuthorization = authorization.ToString();
            
            if (config.Key != sAuthorization)
            {
                resp.Message = "Authorization Error";
                return resp;
            }
            
            var agentResult = _agentIdList.TryGetValue(request.CurrencyId , out var agent);
            if (!agentResult)
            {
                resp.Message = "Currency Agent Not Supported.";
                return resp;
            }

            var allAgentId = JsonConvert.SerializeObject(new List<int> {agent});
            try 
            {
                  var (custId, resultCode) = await _migrationBll.MigrationCustomerIns(request , agent ,allAgentId);
                  
                  if (custId == -1)
                  {
                      resp.Message = "Duplicate SiteId and Username ";
                      resp.ErrorCode = -1;
                      return resp;
                  }
                  
                  if (custId > 0)
                  {
                      resp.CustId = custId;
                      resp.Message = "Successful ";
                      resp.ErrorCode = 0;
                  }
                  else
                  {
                      resp.ErrorCode = resultCode;
                      resp.Message = string.Empty;
                      var checkString = resultCode.ToString("D7");
                      for (var i = 0; i < checkString.Length; i++)
                      {
                          if (checkString[i] == '0')
                          {
                              switch (i)
                              {
                                  case 0:
                                      resp.Message += "Error at table CustWithdrawalLimit; ";
                                      break;
                                  case 1:
                                      resp.Message += "Error at table WithdrawalLimitLog; ";
                                      break;
                                  case 2:
                                      resp.Message += "Error at table CustWallet; ";
                                      break;
                                  case 3:
                                      resp.Message += "Error at table CustBankInfo; ";
                                      break;
                                  case 4:
                                      resp.Message += "Error at table CustRegInfo; ";
                                      break;
                                  case 5:
                                      resp.Message += "Error at table CustProfile; ";
                                      break;
                                  case 6:
                                      resp.Message += "Error at table CustBackupPassword; ";
                                      break;
                              }
                          }
                      }
                      
                  }
            }
            catch (Exception ex) 
            { 
                 _log2File.Error($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", LogName, ex);
            }
            
            _log2File.Info($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}", LogName);
            return resp;
        }
    }
}