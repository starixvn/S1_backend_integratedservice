using System;
using Microsoft.Extensions.DependencyInjection;
using ThirdPartySeamlessWallet.Service.BLL.ThirdParty.Interface;
using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class SabaBLL: IThirdPartyBLL
    {
        private readonly AuthBLL _authBLL;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly TpCustInfoService _tpCustInfoService;
        public Tp ThirdParty { get; set; }
        public GameType GameType { get; set; }
        
        public SabaBLL(IServiceProvider provider)
        {
            ThirdParty = Tp.Saba;
            GameType = GameType.Saba_Sports;
            _authBLL = provider.GetRequiredService<AuthBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _tpCustInfoService = provider.GetRequiredService<TpCustInfoService>();
        }

        public int UpdateOnline(int custId, bool isOnline, string token, string ip)
        {
            return _tpCustInfoService.UpdateOnline(custId, isOnline, token, ip);
        }
        
        public TpCustInfo GetTpInfo(string tpUserName)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache((int)ThirdParty, tpUserName);
            if (tpCustInfo == null)
            {
                tpCustInfo = new TpCustInfo();
            }
            return tpCustInfo;
        }
    }
}