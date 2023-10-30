using System.Data;

using Dapper;

using ThirdPartySeamlessWallet.Service.Library;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.DAL
{
    public class TpCustInfoService
    {
        private readonly MySqlClient _mainDbClient;

        public TpCustInfoService(IServiceProvider provider)
        {
            _mainDbClient = provider.GetServices<MySqlClient>().First(p => p.DataBase == DataBase.Main);
        }
        public TpCustInfo GetTpCustInfoByTpUserName(int TpId, string TpUserName)
        {
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpUserName_Get", new { TpId = TpId, TpUserName = TpUserName });
        }

        public TpCustInfo GetTpCustInfoByTpCustId(int TpId, string tpCustId)
        {
            return _mainDbClient.QueryFirst<TpCustInfo>("Common_TpCustInfoByTpCustId_Get", new { TpId = TpId, TpCustId = tpCustId });
        }

        public int SetCustGotoGame(int custId, int tpId, int gameType)
        {
            return _mainDbClient.Execute("Service_CustGotoGame_Set", commandType: CommandType.StoredProcedure, new { CustId = custId, TpId = tpId, GameType = gameType });
        }
        public int GetCustGotoGameType(int custId, int tpId)
        {
            var p = new DynamicParameters(new { CustId = custId, TpId = tpId });
            p.Add("GameType", dbType: DbType.Int32, direction: ParameterDirection.Output);

            var result = _mainDbClient.Execute("Service_CustGotoGameType_Get", commandType: CommandType.StoredProcedure, p);
            return p.Get<int>("GameType");
        }
        public int SetTpPwd(int tpId, string tpUserName, string tpPwd)
        {
            return _mainDbClient.Execute("Service_TpPwd_Set", commandType: CommandType.StoredProcedure, new { TpId = tpId, TpUserName = tpUserName, TpPwd = tpPwd });
        }
        public void AddLoginRecord(int custId, int siteId, string ip, out string token)
        {
            token = GetToken();

            using var conn = _mainDbClient.GetMySqlConnection();
            conn.Open();
            using var trans = conn.BeginTransaction();

            _mainDbClient.Execute("LoginRecord_Set", trans, new { CustId = custId, SiteId = siteId, IP = ip, Token = token });
            _mainDbClient.CommitAndDisposeConnection(conn, trans);
        }
        public void AddUserLoginLog(int custId, string userName, int siteId, string loginIp, string fromUrl, string userAgent)
        {
            Task.Run(() =>
            {
                _mainDbClient.Execute("LoginLog_Ins", commandType: CommandType.StoredProcedure, new { CustId = custId, UserName = userName, SiteId = siteId, LoginIp = loginIp, FromUrl = fromUrl, UserAgent = userAgent });
            });
        }
        public int UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            var parameters = new DynamicParameters(new { IsOnline = isOnline, CustId = custId, IP = ip, Token = token });
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
