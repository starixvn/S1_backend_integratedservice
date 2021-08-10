using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class BettingBLL
    {
        #region OneGaming

        public int MoneyOutPK_OneGaming(BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, bettingDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.MoneyOutPK_OneGaming(tpCustInfo, bettingDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region AMB

        public int PlaceBetPK_AMB(BettingDataPK_AMB bettingDataPK_AMB, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, bettingDataPK_AMB.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AMB, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataPK_AMB.amount *= tpCurrency.ExchangeRate;
                bettingDataPK_AMB.outStanding *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.PlaceBetPK_AMB(tpCustInfo, bettingDataPK_AMB, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                bettingDataPK_AMB.amount /= tpCurrency.ExchangeRate;
                bettingDataPK_AMB.outStanding /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion

        #region MP

        public int MoneyOutPK_MP(TransferData_MP transferData_MP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.MP, transferData_MP.player);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.MP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                transferData_MP.requestAmount *= tpCurrency.ExchangeRate;
            }

            var result = bettingService.MoneyOutPK_MP(tpCustInfo, transferData_MP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        #endregion
    }
}
