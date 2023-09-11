using ThirdPartySeamlessWallet.Service.DAL;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL.ThirdParty
{
    public class V8BLL
    {
        private readonly int _thisTp;
        private readonly ThirdPartyBLL _thirdPartyBLL;
        private readonly TpCustInfoBLL _tpCustInfoBLL;
        private readonly BettingService _bettingService;
        private readonly SettledService _settledService;

        public V8BLL(IServiceProvider provider)
        {
            _thisTp = (int)Tp.V8;
            _tpCustInfoBLL = provider.GetRequiredService<TpCustInfoBLL>();
            _thirdPartyBLL = provider.GetRequiredService<ThirdPartyBLL>();
            _bettingService = provider.GetRequiredService<BettingService>();
            _settledService = provider.GetRequiredService<SettledService>();
        }

        public int PlaceBetPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _bettingService.PlaceBetPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.PayoutPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int CancelBetPK_V8(BettingDataPK_V8 bettingDataPK_V8, out decimal afterAmount)
        {
            var tpCustInfo = _tpCustInfoBLL.GetTpCustInfoWithCache(_thisTp, bettingDataPK_V8.account);
            var tpCurrency = _thirdPartyBLL.GetTpCurrency(_thisTp, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate != 0)
            {
                bettingDataPK_V8.money *= tpCurrency.ExchangeRate;
            }

            var result = _settledService.CancelBetPK_V8(tpCustInfo, bettingDataPK_V8, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate != 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<BettransDataPK_V8> GetRecordPK_V8(OrderDataPK_V8 orderDataPK_V8)
        {
            return _settledService.GetRecordPK_V8(orderDataPK_V8);
        }
    }
}
