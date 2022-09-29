using Dapper;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly IConfiguration configuration;
        private readonly MySqlClient _mainDbClient;

        public TpCustInfoService(IServiceProvider provider)
        {
            configuration = provider.GetRequiredService<IConfiguration>();
            var mySqlLogger = provider.GetRequiredService<ILogger<MySqlClient>>();
            _mainDbClient = new MySqlClient(configuration.GetConnectionString("ConnectionString"), mySqlLogger);
        }
        public TpCustInfo GetTpCustInfo(int TpId, string TpUserName)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.QueryFirstOrDefault<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName }, commandType: CommandType.StoredProcedure);
        }

        public int SetCustGotoGame(int custId, int tpId, int gameType)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_CustGotoGame_Set", new { CustId = custId, TpId = tpId, GameType = gameType }, commandType: CommandType.StoredProcedure);
        }
        public int GetCustGotoGameType(int custId, int tpId)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            var p = new DynamicParameters(new { CustId = custId, TpId = tpId });
            p.Add("GameType", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = conn.Execute("Service_CustGotoGameType_Get", p, commandType: CommandType.StoredProcedure);
            return p.Get<int>("GameType");
        }
        public int SetTpPwd(int tpId, string tpUserName, string tpPwd)
        {
            using var conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            return conn.Execute("Service_TpPwd_Set", new { TpId = tpId, TpUserName = tpUserName, TpPwd = tpPwd }, commandType: CommandType.StoredProcedure);
        }
        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            token = GetToken();
            using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
            conn.Open();
            using MySqlTransaction trans = conn.BeginTransaction();
            conn.Execute("LoginRecord_Set", new { CustId = custId, SiteId = siteId, IP = ip, Token = token }, trans, commandType: CommandType.StoredProcedure);
            trans.Commit();
        }
        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            Task.Run(() =>
            {
                using MySqlConnection conn = new MySqlConnection(configuration.GetConnectionString("ConnectionString"));
                conn.Execute("LoginLog_Ins", new { CustId = custId, UserName = userName, SiteId = siteId, LoginIp = loginIp, FromUrl = fromUrl, UserAgent = userAgent }, commandType: CommandType.StoredProcedure);
            });
        }
        public int UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            var parameters = new DynamicParameters(new {IsOnline = isOnline, CustId = custId, IP = ip, Token = token});
            return _mainDbClient.Execute("LoginCheckIn_Set", parameters: parameters);
        }
        private string GetToken()
        {
            Guid g = Guid.NewGuid();
            string GuidString = Convert.ToBase64String(g.ToByteArray());
            GuidString = GuidString.Replace("=", "");
            GuidString = GuidString.Replace("+", "");

            return GuidString;
        }

        public TpCountryCode GetTpCurrencyCodeByCurrencyId(int tpId, string tpCurrencyId)
        {
            var parameters = new DynamicParameters(new { TpId = tpId, TpCurrencyId = tpCurrencyId });
            return _mainDbClient.QueryFirst<TpCountryCode>("Service_TpCountryCodeByTpCurrencyId_Get", parameters: parameters);
        }

        public TpCountryCode GetTpCurrencyCode(int tpId, int currencyId)
        {
            var parameters = new DynamicParameters(new { TpId = tpId, CurrencyId = currencyId });
            return _mainDbClient.QueryFirst<TpCountryCode>("Service_TpCountryCode_Get", parameters: parameters);
        }
    }
}
