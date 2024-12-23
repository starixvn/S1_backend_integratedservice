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

        public MigrationController(IServiceProvider provider)
        {
            _migrationBll = provider.GetRequiredService<MigrationBLL>();
            _log2File = provider.GetRequiredService<Log2File>();
            _siteConfigDic = provider.GetRequiredService<ConfigBLL>().GetConfigValuesBySiteId<MigrationConfig>("Migration");
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
            
            try 
            {
                  var (custId, resultCode) = await _migrationBll.MigrationInsFlow(request);
                  
                  if (custId > 0 && resultCode == 0)
                  {
                      resp.CustId = custId;
                      resp.Message = "Successful ";
                      resp.ErrorCode = 0;
                  }
                  else
                  {
                      resp.ErrorCode = resultCode;
                      resp.Message = string.Empty;

                      switch (resultCode)
                      {
                          case -1:
                              resp.Message = "Error at table Customer; ";
                              break;
                          case -2:
                              resp.Message = "Error at table CustBackupPassword; ";
                              break;
                          case -3:
                              resp.Message = "Error at table CustRegInfo; ";
                              break;
                          case -4:
                              resp.Message = "Error at table CustBankInfo; ";
                              break;
                          case -5:
                              resp.Message = "Error at table TpCustInfo; ";
                              break;
                          case -6:
                              resp.Message = "Error at table CustVip; ";
                              break;
                          case -7:
                              resp.Message = "Error at table CustProfile; ";
                              break;
                          case -8:
                              resp.Message = "Error at table CustBankDetail; ";
                              break;
                          case -9:
                              resp.Message = "Error at table CustEWalletDetail; ";
                              break;
                          case -10:
                              resp.Message = "Error at table CustCryptoWalletDetail; ";
                              break;
                          case -11:
                              resp.Message = "Error at table CustTransSummary; ";
                              break;
                          case -12:
                              resp.Message = "Error at table CustRewardPoint; ";
                              break; 
                          case -13:
                              resp.Message = "Error at table CustActiveMonthsLevel; ";
                              break;  
                    }
                  }
            }
            catch (Exception ex) 
            { 
                 _log2File.Error($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "MigrationCustomer", ex);
            }
            
            _log2File.Info($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}", "MigrationCustomer");
            return resp;
        }
        
        [HttpPost]
        public MigrationCustomerBalanceResponse MigrationCustomerBalanceAsync(MigrationBalanceRequest request)
        {
            var resp = new MigrationCustomerBalanceResponse
            {
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
            
            try 
            {
                  var resultCode = _migrationBll.MigrationCustomerBalanceIns(request);
                  
                  if (resultCode == 0)
                  {
                      resp.Message = "Successful ";
                      resp.ErrorCode = 0;
                  }
                  else
                  {
                      resp.ErrorCode = resultCode;
                      resp.Message = string.Empty;

                      switch (resultCode)
                      {
                          case -1:
                              resp.Message = "Error at table CustWallet ";
                              break;
                          case -2:
                              resp.Message = "Error at table CustWithdrawalLimit ";
                              break;
                          case -99:
                              resp.Message = "CustId not exist ";
                              break; 
                    }
                  }
            }
            catch (Exception ex) 
            { 
                 _log2File.Error($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "MigrationCustomerBalance", ex);
            }
            
            _log2File.Info($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}", "MigrationCustomerBalance");
            return resp;
        }
        
        [HttpPost]
        public async Task<MigrationAffiliateResponse> MigrationAffiliateAsync(MigrationAffiliateRequest request)
        {
            var resp = new MigrationAffiliateResponse
            {
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
            
            try 
            {
                  var result = await _migrationBll.MigrationAffiliateIns(request);
                  
                  if (result)
                  {
                      resp.Message = "Successful";
                      resp.ErrorCode = 0;
                  }
            }
            catch (Exception ex) 
            { 
                 _log2File.Error($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}\r\n{ex}", "MigrationAffiliate", ex);
            }
            
            _log2File.Info($"{JsonConvert.SerializeObject(request)}\r\n{JsonConvert.SerializeObject(resp)}", "MigrationAffiliate");
            return resp;
        }
    }
}