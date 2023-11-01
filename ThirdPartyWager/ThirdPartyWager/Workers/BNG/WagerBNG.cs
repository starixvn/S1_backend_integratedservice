using ThirdPartyWager.Service.BLL;
using ThirdPartyWager.Service.Library;
using ThirdPartyWager.Service.Model.enums;

namespace ThirdPartyWager.Workers.BNG
{
    public class WagerBNG : BackgroundService
    {
        private readonly int _tpId = (int)Tp.FaChai;
        private readonly string _tpName = Tp.FaChai.ToString();
        private Timer _timer;
        private readonly Dictionary<int, Dictionary<string, string>> _siteConfigDic;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly WagerFaChaiBLL _wagerFaChaiBLL;
        private readonly CommonBLL _commonBLL;
        private readonly DateTimeHelper _dateTimeHelper;
        private readonly ILogger<WagerFaChai> _logger;
        private readonly int timeZone = -4;
        private readonly int iRangMins = 15;
        private readonly string _timeFormat = "yyyy-MM-dd HH:mm:ss";
        public WagerBNG(IServiceProvider provider)
        {

        }
    }
}
