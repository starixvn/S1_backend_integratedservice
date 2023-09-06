using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThirdPartySeamlessWallet.Service.Model;
using ThirdPartySeamlessWallet.Service.Model.enums;

namespace ThirdPartySeamlessWallet.Service.BLL
{
    public partial class SettledBLL
    {
        #region OneGaming

        public int MoneyInPK_OneGaming(BettingDataPK_OneGaming bettingDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, bettingDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                bettingDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.MoneyInPK_OneGaming(tpCustInfo, bettingDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int RollbackPK_OneGaming(RollbackDataPK_OneGaming rollbackDataPK_OneGaming, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.OneGaming, rollbackDataPK_OneGaming.player_id);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.OneGaming, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                rollbackDataPK_OneGaming.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.RollbackPK_OneGaming(tpCustInfo, rollbackDataPK_OneGaming, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleDataPK_OneGaming> GetUnsettledListPK_OneGaming()
        {
            return settledService.GetUnsettledListPK_OneGaming();
        }

        public int SetSettleFlag_TpWagerPK_OneGaming(string ticket)
        {
            return settledService.SetSettleFlag_TpWagerPK_OneGaming(ticket);
        }

        public int Settled_OneGaming(TpCustInfo tpCustInfo, BetRecordInfo betRecordInfo)
        {
            return settledService.Settled_OneGaming(tpCustInfo, betRecordInfo);
        }

        #endregion

        #region V8

        public IEnumerable<SettleDataPK_V8> GetUnsettledListPK_V8()
        {
            return settledService.GetUnsettledListPK_V8();
        }

        public int SetSettleFlag_TpWagerPK_V8(string gameId)
        {
            return settledService.SetSettleFlag_TpWagerPK_V8(gameId);
        }

        #endregion

        #region AMB

        public int CancelBetPK_AMB(CancelDataPK_AMB cancelDataPK_AMB, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, cancelDataPK_AMB.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AMB, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                cancelDataPK_AMB.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelBetPK_AMB(tpCustInfo, cancelDataPK_AMB, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                cancelDataPK_AMB.amount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public int PayoutPK_AMB(PayoutDataPK_AMB payoutDataPK_AMB, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, payoutDataPK_AMB.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AMB, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                payoutDataPK_AMB.amount *= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.commission *= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.winlose *= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.betAmount *= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.outStanding *= tpCurrency.ExchangeRate;
            }

            var result = settledService.PayoutPK_AMB(tpCustInfo, payoutDataPK_AMB, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.amount /= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.commission /= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.winlose /= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.betAmount /= tpCurrency.ExchangeRate;
                payoutDataPK_AMB.outStanding /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int ResettlePK_AMB(VoidDataPK_AMB voidDataPK_AMB, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.AMB, voidDataPK_AMB.username);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.AMB, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                voidDataPK_AMB.amount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.ResettlePK_AMB(tpCustInfo, voidDataPK_AMB, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
                voidDataPK_AMB.amount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public IEnumerable<SettleData_AMB> GetUnsettledListPK_AMB()
        {
            return settledService.GetUnsettledListPK_AMB();
        }

        public int SetSettleFlag_BettransPK_AMB(string TransId)
        {
            return settledService.SetSettleFlag_BettransPK_AMB(TransId);
        }

        #endregion

        #region MP

        public int MoneyInPK_MP(TransferData_MP transferData_MP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.MP, transferData_MP.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.MP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                transferData_MP.requestAmount *= tpCurrency.ExchangeRate;
                transferData_MP.totalBet = transferData_MP.totalBet / 100 * tpCurrency.ExchangeRate;
                transferData_MP.validBet = transferData_MP.validBet / 100 * tpCurrency.ExchangeRate;
                transferData_MP.totalWithdraw = transferData_MP.totalWithdraw / 100 * tpCurrency.ExchangeRate;
                transferData_MP.revenue = transferData_MP.revenue / 100 * tpCurrency.ExchangeRate;

            }

            var result = settledService.MoneyInPK_MP(tpCustInfo, transferData_MP, out afterAmount);

            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }

            return result;
        }

        public int CancelMoneyInPK_MP(TransferData_MP transferData_MP, out decimal afterAmount)
        {
            var tpCustInfo = tpCustInfoBLL.GetTpCustInfoWithCache((int)Tp.MP, transferData_MP.playerId);
            var tpCurrency = thirdPartyBLL.GetTpCurrency((int)Tp.MP, tpCustInfo.CurrencyId);

            if (tpCurrency.ExchangeRate > 0)
            {
                transferData_MP.requestAmount *= tpCurrency.ExchangeRate;
            }

            var result = settledService.CancelMoneyInPK_MP(tpCustInfo, transferData_MP, out afterAmount);
            if (result > 0 && afterAmount > 0 && tpCurrency.ExchangeRate > 0)
            {
                afterAmount /= tpCurrency.ExchangeRate;
            }
            return result;
        }

        public IEnumerable<SettleDataPK_MP> GetUnsettledListPK_MP()
        {
            return settledService.GetUnsettledListPK_MP();
        }

        public int SetSettleFlag_TpWagerPK_MP(string TransId)
        {
            return settledService.SetSettleFlag_TpWagerPK_MP(TransId);
        }
        #endregion
    }
}
